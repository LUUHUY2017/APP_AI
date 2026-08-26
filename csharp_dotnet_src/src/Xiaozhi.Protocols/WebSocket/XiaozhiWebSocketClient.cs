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
/// WebSocket Client chuẩn cho Tenclass / Xiaozhi.
/// Đảm bảo duy nhất 1 kết nối active tại một thời điểm (Tránh server đá kết nối trùng Device-Id).
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
    private bool _isConnecting = false;

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

    public event Func<byte[], Task>? OnIncomingAudio;
    public event Func<string, Task>? OnIncomingText;
    public event Func<string, string?, Task>? OnLlmResponse;
    public event Func<string, Task>? OnTtsStateChanged;
    public event Func<Task>? OnConnected;
    public event Func<string, Task>? OnDisconnected;
    public event Func<Exception, Task>? OnError;

    public bool IsConnected => _ws != null && _ws.State == WebSocketState.Open;

    public XiaozhiWebSocketClient(string serverUrl, string token, string deviceId, string clientId)
    {
        _serverUrl = serverUrl;
        _token = token;
        _deviceId = deviceId;
        _clientId = clientId;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed) return;
        if (IsConnected) return;

        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected) return;
            await ConnectInternalAsync(cancellationToken);
        }
        finally
        {
            _connectLock.Release();
        }
    }

    private async Task ConnectInternalAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        try
        {
            if (_ws != null)
            {
                try { _ws.Dispose(); } catch { }
                _ws = null;
            }

            _ws = new ClientWebSocket();
            _ws.Options.SetRequestHeader("Authorization", $"Bearer {_token}");
            _ws.Options.SetRequestHeader("Device-Id", _deviceId);
            _ws.Options.SetRequestHeader("Client-Id", _clientId);
            _ws.Options.SetRequestHeader("Protocol-Version", "2");
            _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);

            var uri = new Uri(_serverUrl);
            Log($"[Connect] Connecting to {uri} (DeviceId: {_deviceId})");

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(6));

            await _ws.ConnectAsync(uri, timeoutCts.Token);
            Log($"[Connect] Connected! State={_ws.State}");

            // Start background receive loop
            _ = Task.Run(() => ReceiveLoopAsync(_cts.Token), _cts.Token);

            // Send Hello Handshake
            await SendHelloHandshakeAsync();
        }
        catch (Exception ex)
        {
            Log($"[Connect] Error: {ex.Message}");
            if (_ws != null)
            {
                try { _ws.Dispose(); } catch { }
                _ws = null;
            }
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[32 * 1024];

        try
        {
            while (!ct.IsCancellationRequested && _ws != null && _ws.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Log($"[WS] Close frame received: {result.CloseStatus}");
                        break;
                    }
                    ms.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                var bytes = ms.ToArray();

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var text = Encoding.UTF8.GetString(bytes);
                    Log($"<< RECV JSON: {text}");
                    await HandleServerJsonMessageAsync(text);
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var opusPayload = bytes;
                    if (bytes.Length > 4)
                    {
                        int lengthPrefix = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
                        if (lengthPrefix == bytes.Length - 4)
                        {
                            opusPayload = new byte[lengthPrefix];
                            Buffer.BlockCopy(bytes, 4, opusPayload, 0, lengthPrefix);
                        }
                    }

                    if (OnIncomingAudio != null)
                        await OnIncomingAudio.Invoke(opusPayload);
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
            if (OnDisconnected != null) await OnDisconnected.Invoke("Closed");
        }
    }

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

    public Task SendTextQueryAsync(string text)
    {
        var msg = new ListenMessage
        {
            SessionId = _sessionId,
            Type = "listen",
            State = "detect",
            Text = text
        };
        return SendJsonAsync(msg);
    }

    public Task SendAbortAsync(string reason = "wake_word_detected")
    {
        var msg = new AbortMessage
        {
            SessionId = _sessionId,
            Reason = reason
        };
        return SendJsonAsync(msg);
    }

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

                case "alert":
                    if (root.TryGetProperty("message", out var alertMsg) && OnIncomingText != null)
                        await OnIncomingText.Invoke($"[Thông báo]: {alertMsg.GetString()}");
                    break;

                case "stt":
                    if (root.TryGetProperty("text", out var sttText) && OnIncomingText != null)
                        await OnIncomingText.Invoke($"[STT]: {sttText.GetString()}");
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
                    if (root.TryGetProperty("state", out var ttsState) && OnTtsStateChanged != null)
                    {
                        var state = ttsState.GetString() ?? "";
                        if (state == "sentence_start" && root.TryGetProperty("text", out var sentenceText))
                        {
                            var s = sentenceText.GetString();
                            if (!string.IsNullOrEmpty(s) && OnLlmResponse != null)
                                await OnLlmResponse.Invoke(s, null);
                        }
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

    public async Task SendAudioAsync(byte[] opusData)
    {
        if (!IsConnected || _ws == null) return;
        try
        {
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
                    await _ws.SendAsync(packet, WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            finally
            {
                _sendLock.Release();
            }
        }
        catch { }
    }

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
                    await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
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

    public Task SendJsonAsync(object data)
    {
        return SendTextAsync(JsonSerializer.Serialize(data));
    }

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

    public async ValueTask DisposeAsync()
    {
        _isDisposed = true;
        await DisconnectAsync();
        _sendLock.Dispose();
        _connectLock.Dispose();
    }
}
