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

    private bool _isConnected;
    private bool _isRecording;
    private bool _isSpeaking;
    private string _statusText = "Đang kết nối...";
    private string _currentChatMessage = "Bấm hoặc giữ 🎤 để nói, hoặc gõ tin nhắn bên dưới.";

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
        await ConnectAsync();
    }

    private async Task ConnectAsync()
    {
        var config = ConfigManager.Instance.Config;
        var otaUrl = config.SystemOptions.Network.OtaVersionUrl;
        var mac = config.SystemOptions.DeviceId;
        var clientId = config.SystemOptions.ClientId;
        var token = config.SystemOptions.Network.WebSocketAccessToken;
        var wsUrl = config.SystemOptions.Network.WebSocketUrl;

        // 1. Đăng ký firmware qua OTA trước khi kết nối WebSocket
        try
        {
            var otaPayload = new
            {
                application = new { version = "1.7.2" },
                board = new { name = "xiaozhi-test", mac = mac }
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
            _connectCts = new CancellationTokenSource();
            await _protocolClient.ConnectAsync(_connectCts.Token);
        }
        catch (Exception ex)
        {
            IsConnected = false;
            StatusText = "Lỗi kết nối";
            CurrentChatMessage = $"Lỗi: {ex.Message}.";
        }
    }

    public async Task ReconnectAsync()
    {
        _connectCts?.Cancel();
        _protocolClient = null;
        IsConnected = false;
        await ConnectAsync();
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
                }
            }
            catch { }
            await Task.CompletedTask;
        };

        _protocolClient.OnIncomingText += async (text) =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                StatusText = text;
            });
            await Task.CompletedTask;
        };

        _protocolClient.OnLlmResponse += async (text, emotion) =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                CurrentChatMessage = text;
                MessageAdded?.Invoke(new ChatMessage
                {
                    Content = text,
                    Role = "assistant",
                    Timestamp = DateTime.Now
                });
            });
            await Task.CompletedTask;
        };

        _protocolClient.OnTtsStateChanged += async (state) =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                IsSpeaking = state == "start" || state == "sentence_start";
                if (state == "stop")
                {
                    _audioService.StopPlayback();
                    IsSpeaking = false;
                    StatusText = "✅ Sẵn sàng";
                }
                else if (state == "start" || state == "sentence_start")
                {
                    StatusText = "🔊 AI đang trả lời...";
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
            });
            await Task.CompletedTask;
        };
    }

    public async Task StartListeningAsync()
    {
        if (_protocolClient?.IsConnected != true) return;
        _isListening = true;
        IsRecording = true;
        IsSpeaking = false;
        StatusText = "🎤 Đang nghe...";
        await _protocolClient.StartListeningAsync(mode: "manual");
        _audioService.StartRecording();
    }

    public async Task StopListeningAsync()
    {
        if (!_isListening) return;
        _isListening = false;
        _audioService.StopRecording();
        IsRecording = false;
        StatusText = "🧠 Đang xử lý...";
        CurrentChatMessage = "⏳ Đang xử lý...";
        if (_protocolClient?.IsConnected == true)
            await _protocolClient.StopListeningAsync();
    }

    public async Task SendTextMessageAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || _protocolClient?.IsConnected != true) return;
        MessageAdded?.Invoke(new ChatMessage { Content = text, Role = "user", Timestamp = DateTime.Now });
        StatusText = "🧠 Đang xử lý...";
        CurrentChatMessage = "⏳ Đang gửi câu hỏi...";
        await _protocolClient.SendTextQueryAsync(text);
    }

    public async Task AbortAsync()
    {
        _audioService.StopPlayback();
        IsSpeaking = false;
        StatusText = "⛔ Đã dừng";
        if (_protocolClient?.IsConnected == true)
            await _protocolClient.SendAbortAsync("user_interrupt");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
