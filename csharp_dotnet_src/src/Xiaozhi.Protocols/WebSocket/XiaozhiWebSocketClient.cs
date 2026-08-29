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
/// 4. Đóng gói âm thanh gửi lên: Đính kèm 16-byte Header chuẩn Tenclass trước khi gửi.
/// 5. Quản lý trạng thái: lắng nghe (listen), ngắt lời (abort), gửi câu hỏi văn bản (detect).
/// 6. Bắn các sự kiện: OnSttReceived, OnLlmResponse, OnTtsStateChanged, OnIncomingAudio, OnConnected, OnDisconnected.
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

    // Đường dẫn ghi log traffic mạng để debug khi cần
    private static readonly string LogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "XiaozhiLily", "ws_traffic.log");

    public static event Action<string>? OnRawLog;

    /// <summary>Ghi log chẩn đoán ra Debug và file tạm mà không làm gián đoạn luồng chính.</summary>
    public static void Log(string msg)
    {
        var formatted = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogFile)!);
            File.AppendAllText(LogFile, $"{formatted}\n");
        }
        catch { }
        try { OnRawLog?.Invoke(formatted); } catch { }
    }

    public string? SessionId => _sessionId;

    // Các sự kiện chuẩn theo interface IProtocol
    public event Func<byte[], Task>? OnIncomingAudio;              // Dữ liệu âm thanh Opus sau khi bóc tách header 16 byte
    public event Func<string, Task>? OnIncomingText;               // Toàn bộ JSON hoặc text thông điệp từ server
    public event Func<Task>? OnConnected;                          // Báo kết nối thành công
    public event Func<string, Task>? OnDisconnected;               // Báo ngắt kết nối kèm lý do
    public event Func<Exception, Task>? OnError;                   // Báo lỗi ngoại lệ mạng

    // Các sự kiện nghiệp vụ chuyên biệt cho WPF & MAUI
    public event Func<string, Task>? OnSttReceived;                 // Khi server nhận dạng giọng nói thành văn bản (STT)
    public event Func<string, string?, Task>? OnLlmResponse;        // Khi AI trả lời nội dung text (LLM)
    public event Func<string, Task>? OnTtsStateChanged;            // Trạng thái phát âm thanh AI: start, stop, sentence_start/end
    public event Action<string>? OnStatusChanged;                  // Chuỗi trạng thái hiển thị UI

    public bool IsConnected => _ws?.State == WebSocketState.Open;

    /// <summary>Lưu endpoint/token/danh tính và chuẩn bị ClientWebSocket chưa kết nối.</summary>
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

            // Bắt đầu luồng nền liên tục nhận dữ liệu từ server
            _ = ReceiveLoopAsync(_cts.Token);

            // Gửi gói tin 'hello' để bắt đầu phiên
            await SendHelloHandshakeAsync();
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
    public Task SendHelloHandshakeAsync()
    {
        var hello = new HelloMessage
        {
            Type = "hello",
            Version = 1,
            Transport = "websocket",
            Features = new HelloFeatures { Mcp = false, Aec = false },
            AudioParams = new HelloAudioParams
            {
                Format = "opus",
                SampleRate = 16000,
                Channels = 1,
                FrameDuration = 60
            }
        };
        return SendJsonAsync(hello);
    }

    /// <summary>
    /// Bắt đầu phiên lắng nghe giọng nói (listen: start)
    /// </summary>
    public Task StartListeningAsync(string mode = "manual")
    {
        var msg = new ListenMessage
        {
            SessionId = _sessionId,
            Type = "listen",
            State = "start",
            Mode = mode
        };
        return SendJsonAsync(msg);
    }

    /// <summary>
    /// Dừng phiên lắng nghe giọng nói (listen: stop)
    /// </summary>
    public Task StopListeningAsync()
    {
        var msg = new ListenMessage
        {
            SessionId = _sessionId,
            Type = "listen",
            State = "stop"
        };
        return SendJsonAsync(msg);
    }

    /// <summary>
    /// Gửi câu hỏi văn bản trực tiếp (listen: detect)
    /// </summary>
    public async Task SendTextQueryAsync(string text)
    {
        if (!IsConnected)
        {
            await ConnectAsync();
        }

        // Đảm bảo session đã bắt tay hello xong để lấy SessionId
        int waitAttempts = 0;
        while (string.IsNullOrEmpty(_sessionId) && waitAttempts < 10)
        {
            await Task.Delay(100);
            waitAttempts++;
        }

        // Nhường 100% quyền xử lý nội dung & cấu hình cho Server Xiaozhi (xiaozhi.me)
        var msg = new ListenMessage
        {
            SessionId = _sessionId,
            Type = "listen",
            State = "detect",
            Text = text
        };
        await SendJsonAsync(msg);
    }

    /// <summary>
    /// Ngắt lời AI ngay lập tức (abort)
    /// </summary>
    public Task SendAbortAsync(string reason = "user_interrupt")
    {
        var msg = new AbortMessage
        {
            SessionId = _sessionId,
            Reason = reason
        };
        return SendJsonAsync(msg);
    }

    /// <summary>
    /// Gửi gói tin âm thanh nén Opus lên server kèm theo 16-byte Header chuẩn Tenclass
    /// </summary>
    public async Task SendAudioAsync(byte[] opusData)
    {
        if (!IsConnected || _ws == null) return;
        try
        {
            // Cấu trúc 16-byte header: | u16 ver (2) | u16 type (0) | u32 res (0) | u32 ts (0) | u32 payload_size |
            var packet = new byte[16 + opusData.Length];
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(0, 2), 2);
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2, 2), 0);
            BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(4, 4), 0);
            BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(8, 4), 0);
            BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(12, 4), (uint)opusData.Length);
            Buffer.BlockCopy(opusData, 0, packet, 16, opusData.Length);

            await _sendLock.WaitAsync();
            try
            {
                if (IsConnected && _ws != null)
                {
                    await _ws.SendAsync(packet, WebSocketMessageType.Binary, true, CancellationToken.None);
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }
        catch { }
    }

    /// <summary>
    /// Gửi chuỗi text thô lên server qua WebSocket
    /// </summary>
    public async Task SendTextAsync(string text)
    {
        if (!IsConnected || _ws == null)
        {
            Log($"[WS] Send failed - Not connected");
            return;
        }

        try
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            Log($">> WS SEND: {text}");

            await _sendLock.WaitAsync();
            try
            {
                if (IsConnected && _ws != null)
                {
                    await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }
        catch (Exception ex)
        {
            Log($"[WS] Send Exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Gửi đối tượng C# được Serialize thành JSON
    /// </summary>
    public Task SendJsonAsync(object data)
    {
        return SendTextAsync(JsonSerializer.Serialize(data));
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

                byte[] bytes = ms.ToArray();

                // 1. Gói tin VĂN BẢN (Text JSON)
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(bytes);
                    Log($"[WS RECV JSON] {json}");
                    await HandleServerJsonMessageAsync(json);
                }
                // 2. Gói tin NHỊ PHÂN (Binary Opus Audio)
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    // ====================================================================
                    // BÓC TÁCH HEADER 16-BYTE CHUẨN TENCLASS:
                    // | Version u16 | Type u16 | Reserved u32 | Timestamp u32 | Size u32 | Opus Data |
                    // ====================================================================
                    byte[] opusPayload = bytes;

                    if (bytes.Length > 16)
                    {
                        uint payloadSize = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(12, 4));
                        if (payloadSize == (uint)(bytes.Length - 16))
                        {
                            opusPayload = new byte[payloadSize];
                            Buffer.BlockCopy(bytes, 16, opusPayload, 0, (int)payloadSize);
                        }
                    }
                    // Bóc tách 4-byte length prefix nếu có
                    else if (bytes.Length > 4)
                    {
                        uint payloadSize = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(0, 4));
                        if (payloadSize == (uint)(bytes.Length - 4))
                        {
                            opusPayload = new byte[payloadSize];
                            Buffer.BlockCopy(bytes, 4, opusPayload, 0, (int)payloadSize);
                        }
                    }

                    if (OnIncomingAudio != null)
                    {
                        await OnIncomingAudio.Invoke(opusPayload);
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Log($"[WS] ReceiveLoop Exception: {ex.Message}");
        }
        finally
        {
            Log("[WS] ReceiveLoop finished.");
            if (OnDisconnected != null)
            {
                await OnDisconnected.Invoke("Closed");
            }
        }
    }

    /// <summary>
    /// Bộ điều phối phân tích thông điệp JSON từ server
    /// </summary>
    private async Task HandleServerJsonMessageAsync(string jsonText)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeProp)) return;
            var type = typeProp.GetString();

            if (root.TryGetProperty("session_id", out var sidProp))
            {
                _sessionId = sidProp.GetString();
                Log($"[SessionId] {_sessionId}");
            }

            switch (type)
            {
                case "hello":
                    if (OnConnected != null) await OnConnected.Invoke();
                    break;

                case "error":
                    if (root.TryGetProperty("message", out var errorMsgElem))
                    {
                        var errMsg = errorMsgElem.GetString();
                        Log($"[WS RECV ERROR] {errMsg}");
                        if (OnIncomingText != null)
                            await OnIncomingText.Invoke($"⚠️ Server báo lỗi: {errMsg}");
                    }
                    break;

                case "alert":
                    if (root.TryGetProperty("message", out var alertMsg) && OnIncomingText != null)
                        await OnIncomingText.Invoke($"[Thông báo]: {alertMsg.GetString()}");
                    break;

                case "stt":
                    if (root.TryGetProperty("text", out var sttText))
                    {
                        var textStr = sttText.GetString();
                        if (!string.IsNullOrEmpty(textStr))
                        {
                            if (OnSttReceived != null) await OnSttReceived.Invoke(textStr);
                            if (OnIncomingText != null) await OnIncomingText.Invoke($"[STT]: {textStr}");
                        }
                    }
                    break;

                case "llm":
                    var llmText = root.TryGetProperty("text", out var tp) ? tp.GetString() : null;
                    var emotion = root.TryGetProperty("emotion", out var ep) ? ep.GetString() : null;
                    if (!string.IsNullOrEmpty(llmText) && llmText != "😊" && llmText != "🤔")
                    {
                        if (OnLlmResponse != null) await OnLlmResponse.Invoke(llmText, emotion);
                    }
                    break;

                case "tts":
                    if (root.TryGetProperty("state", out var ttsState))
                    {
                        var state = ttsState.GetString() ?? "";
                        if ((state == "sentence_start" || state == "start") && root.TryGetProperty("text", out var sentenceText))
                        {
                            var s = sentenceText.GetString();
                            if (!string.IsNullOrEmpty(s) && OnLlmResponse != null)
                                await OnLlmResponse.Invoke(s, null);
                        }
                        if (OnTtsStateChanged != null)
                            await OnTtsStateChanged.Invoke(state);
                    }
                    break;

                case "goodbye":
                    Log("Server sent goodbye");
                    if (OnDisconnected != null) await OnDisconnected.Invoke("goodbye");
                    break;
            }
        }
        catch (Exception ex)
        {
            Log($"[WS] Error parsing json: {ex.Message}");
            if (OnError != null) await OnError.Invoke(ex);
        }
    }

    /// <summary>Đóng socket nội bộ nếu còn ở trạng thái có thể gửi close frame.</summary>
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

    /// <summary>Hủy receive loop, đóng socket chủ động và phát trạng thái ngắt kết nối.</summary>
    public async Task DisconnectAsync()
    {
        _cts?.Cancel();
        if (_ws != null)
        {
            try
            {
                if (_ws.State == WebSocketState.Open)
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            catch { }
            finally
            {
                _ws.Dispose();
                _ws = null;
            }
        }
    }

    /// <summary>Ngắt kết nối rồi dispose socket cùng cancellation source của client.</summary>
    public async ValueTask DisposeAsync()
    {
        _isDisposed = true;
        await DisconnectAsync();
        _sendLock.Dispose();
        _connectLock.Dispose();
    }
}
