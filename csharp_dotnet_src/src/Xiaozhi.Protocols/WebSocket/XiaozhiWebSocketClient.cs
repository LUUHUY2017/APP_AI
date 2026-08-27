using System;
using System.Buffers.Binary;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xiaozhi.Core.Interfaces;
using Xiaozhi.Core.Models;

namespace Xiaozhi.Protocols.WebSocket;

/// <summary>
/// ============================================================================
/// LILY AI - WEBSOCKET CLIENT GIAO TIẾP VỚI MÁY CHỦ TENCLASS / XIAOZHI
/// ============================================================================
/// Mục đích & Luồng nghiệp vụ:
/// 1. Thiết lập kết nối WebSocket với các Headers định danh: Device-Id, Client-Id, Authorization.
/// 2. Bắt tay phiên làm việc (Handshake) qua gói tin JSON: 'hello'.
/// 3. Xử lý gói tin nhị phân (Binary): Bóc tách chuẩn xác 16-byte Header của Tenclass
///    để lấy dữ liệu nén Opus nguyên bản và chuyển cho tầng Audio phát ra loa.
/// 4. Quản lý trạng thái: lắng nghe (listen), ngắt lời (abort), gửi câu hỏi văn bản (detect).
/// 5. Bắn các sự kiện: OnSttReceived, OnLlmResponse, OnTtsStateChanged, OnIncomingAudio.
/// </summary>
public class XiaozhiWebSocketClient : IProtocol
{
    private ClientWebSocket? _ws;
    private readonly string _serverUrl;
    private readonly string _token;
    private readonly string _deviceId;
    private readonly string _clientId;
    private string? _sessionId;
    private CancellationTokenSource? _cts;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private bool _isDisposed;

    // Đường dẫn ghi log traffic mạng để debug
    private static readonly string LogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "XiaozhiLily", "ws_traffic.log");

    public static void Log(string msg)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogFile)!);
            File.AppendAllText(LogFile, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n");
        }
        catch { }
    }

    public string? SessionId => _sessionId;

    // Các sự kiện chuẩn theo interface IProtocol
    public event Func<byte[], Task>? OnIncomingAudio;              // Dữ liệu âm thanh Opus sau khi bóc tách header 16 byte
    public event Func<string, Task>? OnIncomingText;               // Toàn bộ JSON thô từ server
    public event Func<Task>? OnConnected;                          // Báo kết nối thành công
    public event Func<string, Task>? OnDisconnected;               // Báo ngắt kết nối kèm lý do
    public event Func<Exception, Task>? OnError;                   // Báo lỗi ngoại lệ mạng

    // Các sự kiện nghiệp vụ chuyên biệt cho WPF & MAUI
    public event Func<string, Task>? OnSttReceived;                 // Khi server nhận dạng giọng nói thành văn bản
    public event Func<string, string?, Task>? OnLlmResponse;        // Khi AI trả lời nội dung text
    public event Func<string, Task>? OnTtsStateChanged;            // Trạng thái phát âm thanh: start, stop, sentence_start/end
    public event Action<string>? OnStatusChanged;                  // Chuỗi trạng thái hiển thị UI

    public bool IsConnected => _ws?.State == WebSocketState.Open;

    public XiaozhiWebSocketClient(string serverUrl, string token, string deviceId, string clientId)
    {
        _serverUrl = serverUrl;
        _token = token;
        _deviceId = deviceId;
        _clientId = clientId;
    }

    /// <summary>
    /// Khởi tạo kết nối WebSocket với các Headers xác thực chuẩn của thiết bị
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected) return;

            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _ws?.Dispose();
            _ws = new ClientWebSocket();

            // Gắn các HTTP Headers bắt buộc để server Tenclass chấp nhận thiết bị
            _ws.Options.SetRequestHeader("Device-Id", _deviceId);
            _ws.Options.SetRequestHeader("Client-Id", _clientId);
            if (!string.IsNullOrEmpty(_token))
            {
                _ws.Options.SetRequestHeader("Authorization", $"Bearer {_token}");
            }
            _ws.Options.SetRequestHeader("Protocol-Version", "2");

            Log($"Connecting to: {_serverUrl} | DeviceId: {_deviceId}");
            OnStatusChanged?.Invoke("Đang kết nối WebSocket...");

            await _ws.ConnectAsync(new Uri(_serverUrl), _cts.Token);

            Log("WebSocket Connected successfully!");
            OnStatusChanged?.Invoke("Đã kết nối! Đang bắt tay phiên...");

            if (OnConnected != null)
            {
                _ = OnConnected.Invoke();
            }

            // Bắt đầu luồng nền liên tục nhận dữ liệu từ server
            _ = ReceiveLoopAsync(_cts.Token);

            // Gửi gói tin 'hello' để bắt đầu phiên
            await SendHelloAsync();
        }
        catch (Exception ex)
        {
            Log($"Connect failed: {ex.Message}");
            OnStatusChanged?.Invoke($"Lỗi kết nối: {ex.Message}");
            if (OnError != null)
            {
                _ = OnError.Invoke(ex);
            }
            throw;
        }
        finally
        {
            _connectLock.Release();
        }
    }

    /// <summary>
    /// Gửi gói tin 'hello' bắt tay xác định cấu hình âm thanh Opus (16kHz, 60ms)
    /// </summary>
    public async Task SendHelloAsync()
    {
        var helloMsg = new
        {
            type = "hello",
            version = 1,
            transport = "websocket",
            features = new { mcp = false, aec = false },
            audio_params = new
            {
                format = "opus",
                sample_rate = 16000,
                channels = 1,
                frame_duration = 60
            }
        };

        await SendJsonAsync(helloMsg);
        Log("Sent Hello handshake.");
    }

    /// <summary>
    /// Bắt đầu phiên lắng nghe giọng nói (listen: start)
    /// </summary>
    public async Task StartListeningAsync(string mode = "auto")
    {
        var msg = new
        {
            session_id = _sessionId,
            type = "listen",
            state = "start",
            mode = mode
        };
        await SendJsonAsync(msg);
        Log($"Sent Listen Start (mode={mode})");
    }

    /// <summary>
    /// Dừng phiên lắng nghe giọng nói (listen: stop)
    /// </summary>
    public async Task StopListeningAsync()
    {
        var msg = new
        {
            session_id = _sessionId,
            type = "listen",
            state = "stop"
        };
        await SendJsonAsync(msg);
        Log("Sent Listen Stop");
    }

    /// <summary>
    /// Gửi chuỗi text thô lên server qua WebSocket (implement IProtocol)
    /// </summary>
    public async Task SendTextAsync(string text)
    {
        await SendTextQueryAsync(text);
    }

    /// <summary>
    /// Gửi câu hỏi văn bản trực tiếp (listen: detect)
    /// </summary>
    public async Task SendTextQueryAsync(string text)
    {
        var msg = new
        {
            session_id = _sessionId,
            type = "listen",
            state = "detect",
            text = text
        };
        await SendJsonAsync(msg);
        Log($"Sent Text Query: {text}");
    }

    /// <summary>
    /// Ngắt lời AI ngay lập tức (abort)
    /// </summary>
    public async Task SendAbortAsync(string reason = "user_interrupt")
    {
        var msg = new
        {
            session_id = _sessionId,
            type = "abort",
            reason = reason
        };
        await SendJsonAsync(msg);
        Log($"Sent Abort (reason={reason})");
    }

    /// <summary>
    /// Gửi một gói tin âm thanh nén Opus lên server
    /// </summary>
    public async Task SendAudioAsync(byte[] opusData)
    {
        if (!IsConnected || _ws == null) return;

        await _sendLock.WaitAsync();
        try
        {
            await _ws.SendAsync(
                new ArraySegment<byte>(opusData),
                WebSocketMessageType.Binary,
                true,
                CancellationToken.None);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// Helper gửi đối tượng C# dưới dạng chuỗi JSON
    /// </summary>
    public async Task SendJsonAsync(object obj)
    {
        if (!IsConnected || _ws == null) return;

        string json = JsonSerializer.Serialize(obj);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        await _sendLock.WaitAsync();
        try
        {
            await _ws.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// Luồng nền liên tục lắng nghe và tiếp nhận gói tin từ server
    /// </summary>
    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[65536];
        var ms = new MemoryStream();

        try
        {
            while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
            {
                ms.SetLength(0);
                WebSocketReceiveResult result;

                do
                {
                    result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Log("Server sent Close message.");
                        await CloseInternalAsync();
                        return;
                    }
                    ms.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                byte[] receivedBytes = ms.ToArray();

                // 1. Gói tin VĂN BẢN (Text JSON)
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string json = Encoding.UTF8.GetString(receivedBytes);
                    Log($"Recv Text: {json}");

                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("session_id", out var sidProp))
                        {
                            _sessionId = sidProp.GetString();
                        }

                        if (root.TryGetProperty("type", out var typeProp))
                        {
                            string type = typeProp.GetString() ?? "";
                            if (type == "stt" && root.TryGetProperty("text", out var sttText))
                            {
                                if (OnSttReceived != null)
                                {
                                    _ = OnSttReceived.Invoke(sttText.GetString() ?? "");
                                }
                            }
                            else if (type == "llm" && root.TryGetProperty("text", out var llmText))
                            {
                                string text = llmText.GetString() ?? "";
                                string? emotion = root.TryGetProperty("emotion", out var emProp) ? emProp.GetString() : null;
                                if (OnLlmResponse != null)
                                {
                                    _ = OnLlmResponse.Invoke(text, emotion);
                                }
                            }
                            else if (type == "tts" && root.TryGetProperty("state", out var ttsState))
                            {
                                if (OnTtsStateChanged != null)
                                {
                                    _ = OnTtsStateChanged.Invoke(ttsState.GetString() ?? "");
                                }
                            }
                        }
                    }
                    catch { }

                    if (OnIncomingText != null)
                    {
                        _ = OnIncomingText.Invoke(json);
                    }
                }
                // 2. Gói tin NHỊ PHÂN (Binary Opus Audio)
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    // ====================================================================
                    // BÓC TÁCH HEADER 16-BYTE CHUẨN TENCLASS:
                    // | Version u16 | Type u16 | Reserved u32 | Timestamp u32 | Size u32 | Opus Data |
                    // ====================================================================
                    byte[] opusPayload = receivedBytes;

                    if (receivedBytes.Length > 16)
                    {
                        uint payloadSize = BinaryPrimitives.ReadUInt32BigEndian(receivedBytes.AsSpan(12, 4));
                        if (payloadSize == (uint)(receivedBytes.Length - 16))
                        {
                            opusPayload = new byte[payloadSize];
                            Buffer.BlockCopy(receivedBytes, 16, opusPayload, 0, (int)payloadSize);
                        }
                    }

                    if (OnIncomingAudio != null)
                    {
                        _ = OnIncomingAudio.Invoke(opusPayload);
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Log($"ReceiveLoop error: {ex.Message}");
            if (OnError != null)
            {
                _ = OnError.Invoke(ex);
            }
        }
        finally
        {
            if (OnDisconnected != null)
            {
                _ = OnDisconnected.Invoke("WebSocket connection terminated.");
            }
        }
    }

    private async Task CloseInternalAsync()
    {
        try
        {
            if (_ws != null && _ws.State == WebSocketState.Open)
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
        catch { }
    }

    public async Task DisconnectAsync()
    {
        _cts?.Cancel();
        await CloseInternalAsync();
        _ws?.Dispose();
        _ws = null;
        OnStatusChanged?.Invoke("Đã ngắt kết nối.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _cts?.Cancel();
        await CloseInternalAsync();
        _ws?.Dispose();
        _sendLock.Dispose();
        _connectLock.Dispose();
    }
}
