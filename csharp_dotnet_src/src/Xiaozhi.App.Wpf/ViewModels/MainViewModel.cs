using System;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xiaozhi.Audio.Codecs;
using Xiaozhi.Audio.Services;
using Xiaozhi.Core.Constants;
using Xiaozhi.Core.Models;
using Xiaozhi.Core.Utils;
using Xiaozhi.Protocols.WebSocket;

namespace Xiaozhi.App.Wpf.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private XiaozhiWebSocketClient? _protocolClient;
    private readonly NAudioAudioService _audioService;
    private readonly OpusCodec _opusCodec;
    private readonly TextToAudioStreamer _textStreamer = new();
    private readonly VoiceActivityDetector _vad = new();
    private readonly HttpClient _httpClient = new();
    private bool _isListening = false;
    private bool _handsFreeMode = false;
    private CancellationTokenSource? _connectCts;
    private System.Timers.Timer? _ttsResetTimer;
    private System.Timers.Timer? _requestTimeoutTimer;

    private bool _isConnected;
    private bool _isRecording;
    private bool _isSpeaking;
    private string _statusText = "✅ Sẵn sàng";
    private string _currentChatMessage = "Bấm 🎤 để nói (hoặc nói xong tự ngắt).";

    public bool IsConnected
    {
        get => _isConnected;
        set { _isConnected = value; OnPropertyChanged(); }
    }

    public bool IsRecording
    {
        get => _isRecording;
        set { _isRecording = value; OnPropertyChanged(); }
    }

    public bool IsSpeaking
    {
        get => _isSpeaking;
        set { _isSpeaking = value; OnPropertyChanged(); }
    }

    public bool HandsFreeMode
    {
        get => _handsFreeMode;
        set { _handsFreeMode = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public string CurrentChatMessage
    {
        get => _currentChatMessage;
        set { _currentChatMessage = value; OnPropertyChanged(); }
    }

    public event Action<ChatMessage>? MessageAdded;

    public MainViewModel()
    {
        _opusCodec = new OpusCodec();
        _audioService = new NAudioAudioService();
        _audioService.OnAudioRecorded += OnAudioCaptured;

        // VAD tự động ngắt và gửi câu hỏi khi người dùng dừng nói (im lặng > 1.2s)
        _vad.OnSpeechEnded += () =>
        {
            if (_isListening && IsRecording)
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(async () =>
                {
                    await StopListeningAsync();
                });
            }
        };

        // Auto-reset speaking state after audio finishes
        _ttsResetTimer = new System.Timers.Timer(3000) { AutoReset = false };
        _ttsResetTimer.Elapsed += (s, e) =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(async () =>
            {
                IsSpeaking = false;
                if (!IsRecording) StatusText = "✅ Sẵn sàng";

                // Trong chế độ Hands-Free: Tự động sẵn sàng lắng nghe câu tiếp theo
                if (HandsFreeMode && !IsRecording)
                {
                    await Task.Delay(500);
                    await StartListeningAsync();
                }
            });
        };

        // Safety timeout for requests
        _requestTimeoutTimer = new System.Timers.Timer(15000) { AutoReset = false };
        _requestTimeoutTimer.Elapsed += (s, e) =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                if (CurrentChatMessage.StartsWith("⏳"))
                {
                    CurrentChatMessage = "Bấm nút 🎤 để nói hoặc gõ câu hỏi tiếp theo...";
                    StatusText = "✅ Sẵn sàng";
                }
            });
        };
    }

    private async void OnAudioCaptured(byte[] pcmBytes)
    {
        if (!_isListening || _protocolClient?.IsConnected != true) return;
        try
        {
            // Xử lý VAD phát hiện im lặng
            _vad.ProcessPcm(pcmBytes);

            var pcmShorts = new short[pcmBytes.Length / 2];
            Buffer.BlockCopy(pcmBytes, 0, pcmShorts, 0, pcmBytes.Length);
            var opusData = _opusCodec.Encode(pcmShorts);
            await _protocolClient.SendAudioAsync(opusData);
        }
        catch { }
    }

    public async Task InitializeAsync()
    {
        await EnsureConnectedAsync();
    }

    public async Task<bool> EnsureConnectedAsync()
    {
        if (_protocolClient != null && _protocolClient.IsConnected)
        {
            IsConnected = true;
            return true;
        }

        var config = ConfigManager.Instance.Config;
        var otaUrl = config.SystemOptions.Network.OtaVersionUrl;
        var mac = config.SystemOptions.DeviceId;
        var clientId = config.SystemOptions.ClientId;
        var token = config.SystemOptions.Network.WebSocketAccessToken;
        var wsUrl = config.SystemOptions.Network.WebSocketUrl;

        try
        {
            var otaPayload = new
            {
                application = new { version = "1.7.2" },
                board = new { name = "xiaozhi-test" }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, otaUrl);
            req.Headers.Add("Device-Id", mac);
            req.Headers.Add("Client-Id", clientId);
            req.Content = new StringContent(JsonSerializer.Serialize(otaPayload), Encoding.UTF8, "application/json");

            var resp = await _httpClient.SendAsync(req);
            if (resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("websocket", out var wsElem))
                {
                    if (wsElem.TryGetProperty("url", out var urlProp) && !string.IsNullOrEmpty(urlProp.GetString()))
                        wsUrl = urlProp.GetString()!;
                    if (wsElem.TryGetProperty("token", out var tokProp) && !string.IsNullOrEmpty(tokProp.GetString()))
                        token = tokProp.GetString()!;
                }
            }
        }
        catch { }

        if (string.IsNullOrWhiteSpace(token))
            token = "test-token";

        _protocolClient = new XiaozhiWebSocketClient(
            wsUrl,
            token,
            mac,
            clientId
        );

        WireEvents();

        try
        {
            _connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _protocolClient.ConnectAsync(_connectCts.Token);
            IsConnected = true;
            StatusText = "✅ Sẵn sàng";
            return true;
        }
        catch (Exception ex)
        {
            IsConnected = false;
            StatusText = "Lỗi kết nối";
            CurrentChatMessage = $"Lỗi: {ex.Message}";
            return false;
        }
    }

    public async Task ReconnectAsync()
    {
        _connectCts?.Cancel();
        _protocolClient = null;
        IsConnected = false;
        await EnsureConnectedAsync();
    }

    private void WireEvents()
    {
        if (_protocolClient == null) return;

        _protocolClient.OnIncomingAudio += async (opusData) =>
        {
            try
            {
                var pcmShorts = _opusCodec.Decode24k(opusData);
                if (pcmShorts.Length > 0)
                {
                    var pcmBytes = new byte[pcmShorts.Length * 2];
                    Buffer.BlockCopy(pcmShorts, 0, pcmBytes, 0, pcmBytes.Length);
                    _audioService.PlayAudio(pcmBytes);

                    _ttsResetTimer?.Stop();
                    _ttsResetTimer?.Start();
                }
            }
            catch { }
            await Task.CompletedTask;
        };

        _protocolClient.OnIncomingText += async (text) =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                _requestTimeoutTimer?.Stop();
                StatusText = text;
            });
            await Task.CompletedTask;
        };

        _protocolClient.OnLlmResponse += async (text, emotion) =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                _requestTimeoutTimer?.Stop();
                CurrentChatMessage = text;
                MessageAdded?.Invoke(new ChatMessage
                {
                    Content = text,
                    Role = "assistant",
                    Timestamp = DateTime.Now
                });

                _ttsResetTimer?.Stop();
                _ttsResetTimer?.Start();
            });
            await Task.CompletedTask;
        };

        _protocolClient.OnTtsStateChanged += async (state) =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                _requestTimeoutTimer?.Stop();
                if (state == "start" || state == "sentence_start")
                {
                    IsSpeaking = true;
                    StatusText = "🔊 AI đang trả lời...";
                    _ttsResetTimer?.Stop();
                    _ttsResetTimer?.Start();
                }
                else if (state == "stop" || state == "sentence_end")
                {
                    _audioService.StopPlayback();
                    IsSpeaking = false;
                    StatusText = "✅ Sẵn sàng";
                }
            });
            await Task.CompletedTask;
        };

        _protocolClient.OnConnected += async () =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                IsConnected = true;
                StatusText = "✅ Sẵn sàng";
            });
            await Task.CompletedTask;
        };

        _protocolClient.OnDisconnected += async (reason) =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                IsRecording = false;
                IsSpeaking = false;
                _isListening = false;
                IsConnected = false;
                _requestTimeoutTimer?.Stop();
            });
            await Task.CompletedTask;
        };
    }

    public async Task StartListeningAsync()
    {
        if (!await EnsureConnectedAsync()) return;

        _vad.Reset();
        _isListening = true;
        IsRecording = true;
        IsSpeaking = false;
        StatusText = "🎤 Đang nghe...";
        CurrentChatMessage = "🎤 Đang lắng nghe... Nói xong AI sẽ tự động gửi.";
        _ttsResetTimer?.Stop();
        _requestTimeoutTimer?.Stop();

        await _protocolClient!.StartListeningAsync(mode: "manual");
        _audioService.StartRecording();
    }

    public async Task StopListeningAsync()
    {
        if (!_isListening) return;
        _isListening = false;
        _audioService.StopRecording();
        IsRecording = false;
        StatusText = "🧠 Đang xử lý...";
        CurrentChatMessage = "⏳ Đang xử lý câu trả lời...";

        _requestTimeoutTimer?.Stop();
        _requestTimeoutTimer?.Start();

        if (_protocolClient?.IsConnected == true)
            await _protocolClient.StopListeningAsync();
    }

    public async Task SendTextMessageAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!await EnsureConnectedAsync()) return;

        MessageAdded?.Invoke(new ChatMessage { Content = text, Role = "user", Timestamp = DateTime.Now });
        StatusText = "🧠 Đang xử lý...";
        CurrentChatMessage = "⏳ Đang gửi câu hỏi...";

        _requestTimeoutTimer?.Stop();
        _requestTimeoutTimer?.Start();

        if (text.Length <= 12)
        {
            await _protocolClient!.SendTextQueryAsync(text);
        }
        else
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _textStreamer.StreamTextAsAudioAsync(_protocolClient!, text);
                }
                catch (Exception ex)
                {
                    XiaozhiWebSocketClient.Log($"StreamTextAsAudio Exception: {ex.Message}");
                }
            });
        }
    }

    public async Task AbortAsync()
    {
        _audioService.StopPlayback();
        _ttsResetTimer?.Stop();
        _requestTimeoutTimer?.Stop();
        IsSpeaking = false;
        StatusText = "⛔ Đã dừng";
        if (_protocolClient?.IsConnected == true)
            await _protocolClient.SendAbortAsync("user_interrupt");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
