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
    private readonly HttpClient _httpClient = new();
    private bool _isListening = false;
    private CancellationTokenSource? _connectCts;
    private System.Timers.Timer? _ttsResetTimer;
    private System.Timers.Timer? _requestTimeoutTimer;

    private bool _isConnected;
    private bool _isRecording;
    private bool _isSpeaking;
    private string _statusText = "✅ Sẵn sàng";
    private string _currentChatMessage = "Bấm nút 🎤 để nói chuyện với AI...";

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

        // Auto-reset speaking state after audio finishes
        _ttsResetTimer = new System.Timers.Timer(3000) { AutoReset = false };
        _ttsResetTimer.Elapsed += (s, e) =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                IsSpeaking = false;
                if (!IsRecording) StatusText = "✅ Sẵn sàng";
            });
        };

        // Safety timeout for requests (reset UI if server doesn't respond in 8s)
        _requestTimeoutTimer = new System.Timers.Timer(8000) { AutoReset = false };
        _requestTimeoutTimer.Elapsed += (s, e) =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                if (CurrentChatMessage.StartsWith("⏳"))
                {
                    CurrentChatMessage = "💡 Hãy bấm nút 🎤 và nói bằng giọng nói để AI trả lời tốt nhất!";
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
                if (text.StartsWith("[Thông báo]:"))
                {
                    // Server alert for long text detect
                    CurrentChatMessage = "💡 Server Xiaozhi là AI thoại. Hãy bấm nút 🎤 để nói trực tiếp bằng giọng nói!";
                    StatusText = "✅ Sẵn sàng";
                }
                else
                {
                    StatusText = text;
                }
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

        _isListening = true;
        IsRecording = true;
        IsSpeaking = false;
        StatusText = "🎤 Đang nghe...";
        CurrentChatMessage = "🎤 Đang ghi âm giọng nói của bạn... Hãy nói câu hỏi.";
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
        CurrentChatMessage = "⏳ Đang gửi giọng nói lên AI...";

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

        await _protocolClient!.SendTextQueryAsync(text);
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
