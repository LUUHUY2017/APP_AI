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

// INotifyPropertyChanged là "hợp đồng" của MVVM: ViewModel phải phát sự kiện khi một property đổi.
// Nhờ đó giao diện không cần liên tục hỏi lại trạng thái mà chỉ phản ứng đúng lúc có thay đổi.
public class MainViewModel : INotifyPropertyChanged
{
    // Các dịch vụ dài hạn của một phiên chạy: mạng, codec, micro/loa, VAD và chuyển văn bản thành audio.
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
    private string? _lastSentUserText;

    private bool _isConnected;
    private bool _isRecording;
    private bool _isSpeaking;
    private string _statusText = "✅ Sẵn sàng";
    private string _currentChatMessage = "Bấm 🎤 để nói (hoặc nói xong tự ngắt).";

    public bool IsConnected
    {
        // get trả trạng thái kết nối hiện tại cho UI hoặc lớp gọi.
        get => _isConnected;
        set
        {
            // Lưu trạng thái mới vào backing field.
            _isConnected = value;

            // Không truyền tên property: CallerMemberName tự suy ra tên "IsConnected".
            OnPropertyChanged();
        }
    }

    public bool IsRecording
    {
        get => _isRecording;
        set
        {
            // Property này chỉ công bố trạng thái cho UI; nó không trực tiếp mở/tắt micro.
            // Micro thật được điều khiển bởi _audioService.StartRecording/StopRecording.
            _isRecording = value;
            OnPropertyChanged();
        }
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
        set
        {
            _statusText = value;

            // MainWindow nhận event này và chép StatusText sang StatusLabel.Text.
            OnPropertyChanged();
        }
    }

    public string CurrentChatMessage
    {
        get => _currentChatMessage;
        set { _currentChatMessage = value; OnPropertyChanged(); }
    }

    public event Action<ChatMessage>? MessageAdded;

    public MainViewModel()
    {
        // Dữ liệu PCM từ micro được đẩy ngay sang pipeline VAD -> Opus -> WebSocket.
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
        // Mỗi buffer PCM 16 kHz mono vừa phục vụ VAD cục bộ, vừa được nén Opus để giảm băng thông.
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

    /// <summary>Điểm khởi tạo công khai của ViewModel; hiện chỉ bảo đảm kết nối server.</summary>
    public async Task InitializeAsync()
    {
        await EnsureConnectedAsync();
    }

    public async Task<bool> EnsureConnectedAsync()
    {
        // Tái sử dụng phiên còn sống; tránh tạo nhiều WebSocket khi người dùng bấm liên tục.
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
            // OTA discovery trả về endpoint và token WebSocket hiện hành cho thiết bị.
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
        // Hủy lần kết nối đang chờ, bỏ client cũ và dựng lại toàn bộ phiên từ cấu hình hiện tại.
        _connectCts?.Cancel();
        _protocolClient = null;
        IsConnected = false;
        await EnsureConnectedAsync();
    }

    private void WireEvents()
    {
        // Chuyển các gói/sự kiện mức giao thức thành trạng thái mà giao diện có thể hiển thị.
        if (_protocolClient == null) return;

        _protocolClient.OnIncomingAudio += async (opusData) =>
        {
            // Server gửi TTS dạng Opus 24 kHz; giải mã về PCM rồi đưa vào bộ đệm loa.
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

        _protocolClient.OnSttReceived += async (sttText) =>
        {
            // STT là câu server nhận dạng được từ audio của người dùng.
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                _requestTimeoutTimer?.Stop();
                StatusText = "🧠 AI đang suy nghĩ...";
                CurrentChatMessage = $"🗣️ AI đã nghe: \"{sttText}\"";

                // Thêm bong bóng chat người dùng nếu câu này chưa được gửi trước đó qua khung gõ chữ
                if (!string.Equals(_lastSentUserText, sttText.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    MessageAdded?.Invoke(new ChatMessage
                    {
                        Content = sttText,
                        Role = "user",
                        Timestamp = DateTime.Now
                    });
                }
                _lastSentUserText = null;
            });
            await Task.CompletedTask;
        };

        _protocolClient.OnLlmResponse += async (text, emotion) =>
        {
            // Phần chữ của câu trả lời được hiển thị ngay, độc lập với audio TTS đến sau.
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
            // TTS start/stop điều khiển avatar, trạng thái và việc dừng bộ đệm phát.
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
        // BƯỚC 1: Phải có WebSocket trước khi mở micro.
        // "await" chờ kết nối nhưng không khóa UI thread, nên cửa sổ vẫn phản hồi bình thường.
        // Nếu kết nối thất bại, return ngăn việc thu âm vô ích vì không có nơi nhận audio.
        if (!await EnsureConnectedAsync())
            return;

        // BƯỚC 2: Xóa bộ đếm giọng nói/im lặng của câu trước.
        // Nếu không reset, VAD có thể dùng trạng thái cũ và kết thúc nhầm câu mới.
        _vad.Reset();

        // BƯỚC 3: Cho phép callback OnAudioCaptured xử lý các buffer micro sắp nhận.
        // Đây là cờ nghiệp vụ nội bộ, khác với IsRecording dùng để thông báo cho UI.
        _isListening = true;

        // BƯỚC 4: Công bố trạng thái mới. Mỗi phép gán bên dưới gọi OnPropertyChanged(),
        // sau đó MainWindow đổi màu nút, chạy animation và cập nhật các nhãn.
        IsRecording = true;
        IsSpeaking = false;
        StatusText = "🎤 Đang nghe...";
        CurrentChatMessage = "🎤 Đang lắng nghe... Nói xong AI sẽ tự động gửi.";

        // BƯỚC 5: Hủy các timer thuộc phiên trả lời trước để chúng không sửa UI giữa lúc đang thu.
        _ttsResetTimer?.Stop();
        _requestTimeoutTimer?.Stop();

        // BƯỚC 6: Báo server bắt đầu một lượt nói mới. Lệnh này CHƯA mở micro;
        // nó chỉ gửi message điều khiển "listen/start" qua WebSocket.
        // Dấu ! khẳng định với compiler rằng client không null vì bước 1 đã kết nối thành công.
        await _protocolClient!.StartListeningAsync(mode: "manual");

        // BƯỚC 7: Mở micro Windows thật sự. NAudio sẽ phát OnAudioRecorded cho từng buffer PCM;
        // constructor đã nối event đó với OnAudioCaptured để chạy VAD -> Opus -> WebSocket.
        _audioService.StartRecording();
    }

    public async Task StopListeningAsync()
    {
        // Đóng micro trước, sau đó báo server kết thúc câu để kích hoạt STT/LLM/TTS.
        // Guard clause tránh gửi stop hai lần khi VAD và thao tác người dùng xảy ra gần nhau.
        if (!_isListening) return;

        // Chặn OnAudioCaptured gửi thêm buffer ngay từ thời điểm này.
        _isListening = false;

        // Dừng và Dispose thiết bị thu để Windows nhả micro.
        _audioService.StopRecording();

        // Phát PropertyChanged để giao diện tắt animation thu và hiện trạng thái xử lý.
        IsRecording = false;
        StatusText = "🧠 Đang xử lý...";
        CurrentChatMessage = "⏳ Đang xử lý câu trả lời...";

        // Nếu server không phản hồi trong 15 giây, timer sẽ đưa UI về trạng thái sẵn sàng.
        _requestTimeoutTimer?.Stop();
        _requestTimeoutTimer?.Start();

        // Message stop đánh dấu cuối câu; server từ đây mới chạy STT -> LLM -> TTS.
        if (_protocolClient?.IsConnected == true)
            await _protocolClient.StopListeningAsync();
    }

    public async Task SendTextMessageAsync(string text)
    {
        // Câu ngắn dùng truy vấn text trực tiếp; câu dài được stream như audio để tương thích server.
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!await EnsureConnectedAsync()) return;

        _lastSentUserText = text.Trim();
        MessageAdded?.Invoke(new ChatMessage { Content = text, Role = "user", Timestamp = DateTime.Now });
        StatusText = "🧠 Đang xử lý...";
        CurrentChatMessage = "⏳ Đang gửi câu hỏi...";

        _requestTimeoutTimer?.Stop();
        _requestTimeoutTimer?.Start();

        if (text.Length <= 8)
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
        // Dừng phát cục bộ ngay rồi gửi abort để server ngừng phần trả lời còn lại.
        _audioService.StopPlayback();
        _ttsResetTimer?.Stop();
        _requestTimeoutTimer?.Stop();
        IsSpeaking = false;
        StatusText = "⛔ Đã dừng";
        if (_protocolClient?.IsConnected == true)
            await _protocolClient.SendAbortAsync("user_interrupt");
    }

    // Các đối tượng quan sát ViewModel (ở đây là MainWindow) đăng ký vào event này.
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Thông báo một property của ViewModel vừa thay đổi để giao diện đọc lại giá trị mới.
    /// </summary>
    /// <param name="name">
    /// Tên property thay đổi. CallerMemberName tự điền tên hàm/property gọi nó,
    /// ví dụ gọi OnPropertyChanged() trong setter IsRecording sẽ tạo tên "IsRecording".
    /// </param>
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        // ?. bảo đảm chỉ Invoke khi đã có ít nhất một subscriber; nếu chưa có thì bỏ qua an toàn.
        // "this" là ViewModel phát event; EventArgs mang tên property sang phía nhận.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
