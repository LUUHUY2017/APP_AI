using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Xiaozhi.Protocols.WebSocket;

namespace Xiaozhi.App.Maui;

public partial class MainPage : ContentPage
{
    private XiaozhiWebSocketClient _client;
    private bool _isRecording = false;
    private bool _handsFree = false;
    private bool _receivedResponse = false;

    private string _wsUrl = "wss://api.tenclass.net/xiaozhi/v1/";
    private string _token = "test-token";
    private string _deviceId = "38:60:77:dc:90:11";

    private float _currentVolume = 1.0f;

    public MainPage()
    {
        InitializeComponent();

        _wsUrl = Preferences.Default.Get("lily_ws_url", _wsUrl);
        _token = Preferences.Default.Get("lily_token", _token);
        _deviceId = Preferences.Default.Get("lily_device_id", _deviceId);
        if (string.IsNullOrWhiteSpace(_deviceId) || _deviceId == "a0:36:bc:2c:ed:40" || _deviceId == "00:00:00:00:00:00")
        {
            _deviceId = "38:60:77:dc:90:11";
            Preferences.Default.Set("lily_device_id", _deviceId);
        }

        var clientId = Preferences.Default.Get("lily_client_id", "b7907b41-1534-422b-a9ce-26b227286d8e");
        if (string.IsNullOrWhiteSpace(clientId) || clientId == "maui-ios-client" || clientId == "21ebee2f-926c-4703-9010-b488f5939580" || clientId == "d7377f0a-2682-4e4f-a125-e0a78c730cf8")
        {
            clientId = "b7907b41-1534-422b-a9ce-26b227286d8e";
            Preferences.Default.Set("lily_client_id", clientId);
        }

        XiaozhiWebSocketClient.OnRawLog += (logMsg) =>
        {
            _debugLogs.Enqueue(logMsg);
            while (_debugLogs.Count > 120) _debugLogs.TryDequeue(out _);
        };

        _client = new XiaozhiWebSocketClient(_wsUrl, _token, _deviceId, clientId);
        SetupClientHandlers();
        _ = ConnectWithOtaAsync();

        // Tự động nâng toàn bộ giao diện (MainGrid) lên trên bàn phím ảo iOS khi gõ chữ
        TextInput.Focused += (s, e) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainGrid.Margin = new Thickness(0, 0, 0, 300);
                ScrollToBottom(150);
            });
        };

        TextInput.Unfocused += (s, e) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainGrid.Margin = new Thickness(0, 0, 0, 0);
            });
        };
    }

    private readonly System.Collections.Concurrent.ConcurrentQueue<string> _debugLogs = new();
    private string _lastSentText = string.Empty;

    private string _currentAiStreamText = string.Empty;
    private Label? _currentAiBubbleLabel = null;
    private readonly System.Collections.Concurrent.ConcurrentQueue<string> _speechQueue = new();
    private bool _isSpeaking = false;

    private readonly Concentus.Structs.OpusDecoder _opusDecoder24k = new Concentus.Structs.OpusDecoder(24000, 1);
    private bool _hasReceivedServerAudio = false;
    private readonly System.IO.MemoryStream _serverAudioPcmStream = new();

    private void PlayServerAudioChunkOnIos(byte[] pcmBytes)
    {
        lock (_serverAudioPcmStream)
        {
            _serverAudioPcmStream.Write(pcmBytes, 0, pcmBytes.Length);
        }
    }

    private void FlushAndPlayServerAudioOnIos()
    {
        byte[] pcmData;
        lock (_serverAudioPcmStream)
        {
            pcmData = _serverAudioPcmStream.ToArray();
            _serverAudioPcmStream.SetLength(0);
        }

        if (pcmData.Length == 0) return;

        byte[] wavHeader = CreateWavHeader(pcmData.Length, 24000, 1, 16);
        byte[] fullWav = new byte[wavHeader.Length + pcmData.Length];
        Buffer.BlockCopy(wavHeader, 0, fullWav, 0, wavHeader.Length);
        Buffer.BlockCopy(pcmData, 0, fullWav, wavHeader.Length, pcmData.Length);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                using var nsData = Foundation.NSData.FromArray(fullWav);
                var player = AVFoundation.AVAudioPlayer.FromData(nsData);
                if (player != null)
                {
                    player.Play();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AVAudioPlayer Error: {ex.Message}");
            }
        });
    }

    public static byte[] CreateWavHeader(int pcmDataLength, int sampleRate = 24000, int channels = 1, int bitsPerSample = 16)
    {
        byte[] header = new byte[44];
        int byteRate = sampleRate * channels * (bitsPerSample / 8);
        int blockAlign = channels * (bitsPerSample / 8);

        header[0] = (byte)'R'; header[1] = (byte)'I'; header[2] = (byte)'F'; header[3] = (byte)'F';
        int fileSize = 36 + pcmDataLength;
        Buffer.BlockCopy(BitConverter.GetBytes(fileSize), 0, header, 4, 4);

        header[8] = (byte)'W'; header[9] = (byte)'A'; header[10] = (byte)'V'; header[11] = (byte)'E';

        header[12] = (byte)'f'; header[13] = (byte)'m'; header[14] = (byte)'t'; header[15] = (byte)' ';
        Buffer.BlockCopy(BitConverter.GetBytes(16), 0, header, 16, 4);
        Buffer.BlockCopy(BitConverter.GetBytes((short)1), 0, header, 20, 2);
        Buffer.BlockCopy(BitConverter.GetBytes((short)channels), 0, header, 22, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(sampleRate), 0, header, 24, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(byteRate), 0, header, 28, 4);
        Buffer.BlockCopy(BitConverter.GetBytes((short)blockAlign), 0, header, 32, 2);
        Buffer.BlockCopy(BitConverter.GetBytes((short)bitsPerSample), 0, header, 34, 2);

        header[36] = (byte)'d'; header[37] = (byte)'a'; header[38] = (byte)'t'; header[39] = (byte)'a';
        Buffer.BlockCopy(BitConverter.GetBytes(pcmDataLength), 0, header, 40, 4);

        return header;
    }

    private void SetupClientHandlers()
    {
        _client.OnIncomingAudio += async (opusData) =>
        {
            _hasReceivedServerAudio = true;
            try
            {
                var outputPcm = new short[2880];
                int decodedSamples = _opusDecoder24k.Decode(opusData, 0, opusData.Length, outputPcm, 0, outputPcm.Length, false);
                if (decodedSamples > 0)
                {
                    var pcmBytes = new byte[decodedSamples * 2];
                    Buffer.BlockCopy(outputPcm, 0, pcmBytes, 0, pcmBytes.Length);
                    PlayServerAudioChunkOnIos(pcmBytes);
                }
            }
            catch (Exception ex)
            {
                XiaozhiWebSocketClient.Log($"OnIncomingAudio error: {ex.Message}");
            }
            await Task.CompletedTask;
        };

        _client.OnTtsStateChanged += async (state) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (state == "start" || state == "sentence_start")
                {
                    StatusLabel.Text = "🔊 Xiaozhi AI đang phát giọng nói...";
                }
                else if (state == "stop" || state == "sentence_end")
                {
                    FlushAndPlayServerAudioOnIos();
                    StatusLabel.Text = "✅ Sẵn sàng";
                }
            });
            await Task.CompletedTask;
        };

        _client.OnIncomingText += async (msg) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = msg;
            });
            await Task.CompletedTask;
        };

        _client.OnSttReceived += async (sttText) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = "🧠 AI đang suy nghĩ...";
                CurrentMsgLabel.Text = $"🗣️ AI đã nghe: \"{sttText}\"";

                // Chỉ hiển thị bong bóng chat từ Mic nếu câu thoại khác với tin nhắn vừa gõ bằng bàn phím
                if (!string.Equals(sttText?.Trim(), _lastSentText, StringComparison.OrdinalIgnoreCase))
                {
                    AddChatMessage($"🗣️ {sttText}", isUser: true);
                }
            });
            await Task.CompletedTask;
        };

        _client.OnLlmResponse += async (text, emotion) =>
        {
            _receivedResponse = true;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AppendOrAddAiResponse(text);
            });
            await Task.CompletedTask;
        };
    }

    private readonly Services.OtaAutoUpdateService _appUpdateService = new();

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Tự động kiểm tra bản cập nhật mới (OTA Auto-Update) khi mở App trên iOS
        _ = Task.Run(async () =>
        {
            await Task.Delay(2500);
            await _appUpdateService.CheckForUpdatesAsync(this, silentIfLatest: true);
        });
    }

    private async Task ConnectWithOtaAsync()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = "🔄 Đang kiểm tra OTA & kết nối...";
        });

        try
        {
            var clientId = Preferences.Default.Get("lily_client_id", "b7907b41-1534-422b-a9ce-26b227286d8e");
            var macAddress = Preferences.Default.Get("lily_device_id", "38:60:77:dc:90:11");

            var activationService = new Xiaozhi.Protocols.Ota.DeviceActivationService();
            var actResult = await activationService.CheckOrRequestActivationAsync(clientId, macAddress);

            if (!string.IsNullOrEmpty(actResult.Token))
            {
                _token = actResult.Token;
                Preferences.Default.Set("lily_token", _token);
                if (!string.IsNullOrEmpty(actResult.WebSocketUrl))
                {
                    _wsUrl = actResult.WebSocketUrl;
                    Preferences.Default.Set("lily_ws_url", _wsUrl);
                }

                _client = new XiaozhiWebSocketClient(_wsUrl, _token, _deviceId, clientId);
                SetupClientHandlers();
            }

            if (string.IsNullOrWhiteSpace(_token))
            {
                _token = "test-token";
            }

            await _client.ConnectAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = "✅ Sẵn sàng";
                CurrentMsgLabel.Text = "✅ Đã kết nối với trợ lý Backend!";
            });
        }
        catch
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = "🌐 Ngoại tuyến / Voice Engine";
                CurrentMsgLabel.Text = "⚡ Kết nối Server bận. Đã tự động kích hoạt giọng nói iOS!";
            });
        }
    }

    private System.Timers.Timer? _silenceTimer;

    private async void OnMicButtonClicked(object sender, EventArgs e)
    {
        if (!_isRecording)
        {
            await StartRecordingAsync();
        }
        else
        {
            await StopRecordingAndProcessAsync();
        }
    }

    private async Task StartRecordingAsync()
    {
        _isRecording = true;
        MicButton.Text = "⏹";
        MicButton.BackgroundColor = Color.FromArgb("#2D3037");
        MicButton.TextColor = Colors.White;
        StatusLabel.Text = "🎙️ Đang lắng nghe...";
        StartSilenceAutoSendTimer();

        if (_client.IsConnected)
        {
            await _client.StartListeningAsync(mode: "manual");
        }
    }

    private void StartSilenceAutoSendTimer()
    {
        _silenceTimer?.Stop();
        _silenceTimer?.Dispose();
        _silenceTimer = new System.Timers.Timer(1400) { AutoReset = false };
        _silenceTimer.Elapsed += (s, e) =>
        {
            if (_isRecording)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    StatusLabel.Text = "⚡ Đã phát hiện im lặng. Tự động gửi...";
                    await StopRecordingAndProcessAsync();
                });
            }
        };
        _silenceTimer.Start();
    }

    private async Task StopRecordingAndProcessAsync()
    {
        if (!_isRecording) return;
        _silenceTimer?.Stop();
        _isRecording = false;
        MicButton.Text = "🎙️";
        MicButton.BackgroundColor = Colors.Transparent;
        MicButton.TextColor = Color.FromArgb("#AEB7C2");
        StatusLabel.Text = "🧠 Đang xử lý...";

        if (_client.IsConnected)
        {
            await _client.StopListeningAsync();
        }
    }

    private async void OnHandsFreeClicked(object sender, EventArgs e)
    {
        _handsFree = !_handsFree;
        HandsFreeBtn.Text = _handsFree ? "🎙️ Rảnh tay: Bật" : "🎙️ Rảnh tay: Tắt";
        HandsFreeBtn.BackgroundColor = _handsFree ? Color.FromArgb("#2F8F68") : Color.FromArgb("#171A20");
        HandsFreeBtn.TextColor = _handsFree ? Colors.White : Color.FromArgb("#B8C1CC");

        if (_handsFree && !_isRecording)
        {
            await StartRecordingAsync();
        }
        else if (!_handsFree && _isRecording)
        {
            await StopRecordingAndProcessAsync();
        }
    }

    private async void OnDebugLogClicked(object sender, EventArgs e)
    {
        var arr = _debugLogs.ToArray();
        Array.Reverse(arr);
        var logs = string.Join("\n\n", arr);
        if (string.IsNullOrWhiteSpace(logs)) logs = "Chưa có nhật ký kết nối WebSocket nào.";

        var editor = new Editor
        {
            Text = logs,
            IsReadOnly = true,
            TextColor = Color.FromArgb("#00ff9d"),
            BackgroundColor = Color.FromArgb("#0d071d"),
            FontSize = 11,
            VerticalOptions = LayoutOptions.Fill
        };

        var headerLabel = new Label
        {
            Text = "🐞 LOG WEBSOCKET REAL-TIME (MỚI NHẤT TRÊN ĐẦU 🔥)",
            TextColor = Color.FromArgb("#FF5E36"),
            FontAttributes = FontAttributes.Bold,
            FontSize = 14
        };

        var closeButton = new Button
        {
            Text = "ĐÓNG NHẬT KÝ",
            BackgroundColor = Color.FromArgb("#bd00ff"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 48,
            CornerRadius = 24,
            Margin = new Thickness(0, 6, 0, 4),
            Command = new Command(async () => await Navigation.PopModalAsync())
        };

        var mainGrid = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            },
            Padding = new Thickness(14, 8, 14, 12),
            RowSpacing = 8
        };

        Grid.SetRow(headerLabel, 0);
        Grid.SetRow(editor, 1);
        Grid.SetRow(closeButton, 2);

        mainGrid.Children.Add(headerLabel);
        mainGrid.Children.Add(editor);
        mainGrid.Children.Add(closeButton);

        var page = new ContentPage
        {
            Title = "🐞 Debug Log WebSocket",
            BackgroundColor = Color.FromArgb("#070412"),
            Content = mainGrid
        };

        await Navigation.PushModalAsync(new NavigationPage(page));
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await ConnectWithOtaAsync();
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        string pin = await DisplayPromptAsync("🔒 Bảo mật Cấu hình", "Vui lòng nhập mật khẩu bảo mật (PIN):", "Xác nhận", "Hủy", placeholder: "Nhập PIN (0000)...", keyboard: Keyboard.Numeric);
        if (pin != "0000")
        {
            await DisplayAlert("⚠️ Bảo mật", "Mật khẩu bảo mật không chính xác! Quyền truy cập bị từ chối.", "OK");
            return;
        }

        var settingsPage = new SettingsPage();
        settingsPage.SettingsSaved += async (s, args) =>
        {
            _wsUrl = Preferences.Default.Get("lily_ws_url", _wsUrl);
            _token = Preferences.Default.Get("lily_token", _token);
            _deviceId = Preferences.Default.Get("lily_device_id", _deviceId);
            var clientId = Preferences.Default.Get("lily_client_id", "b7907b41-1534-422b-a9ce-26b227286d8e");

            _client = new XiaozhiWebSocketClient(_wsUrl, _token, _deviceId, clientId);
            SetupClientHandlers();
            await ConnectWithOtaAsync();
        };

        await Navigation.PushModalAsync(settingsPage);
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        var text = TextInput.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;
        TextInput.Text = string.Empty;
        TextInput.Unfocus();

        _lastSentText = text;
        _currentAiStreamText = string.Empty;
        _currentAiBubbleLabel = null;
        AddChatMessage(text, isUser: true);
        StatusLabel.Text = "🧠 Đang xử lý...";
        _receivedResponse = false;

        try
        {
            await _client.SendTextQueryAsync(text);
        }
        catch (Exception ex)
        {
            XiaozhiWebSocketClient.Log($"OnSendClicked send error: {ex.Message}");
        }

        // Check for iOS App Launcher voice commands
        if (await TryOpenAppByVoiceCommandAsync(text)) return;

        // Nếu đã kết nối Server Xiaozhi, nhường 100% quyền trả lời cho Server (Model, Prompt & Voice cài trên xiaozhi.me)
        if (_client.IsConnected) return;

        // Nếu ngắt kết nối, dùng fallback dự phòng
        _ = Task.Run(async () =>
        {
            await Task.Delay(5000);
            if (!_receivedResponse)
            {
                _receivedResponse = true;
                string reply = $"Dạ AI đây! Mình đã nhận được câu hỏi \"{text}\" từ bạn.";

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusLabel.Text = "✅ Sẵn sàng";
                    CurrentMsgLabel.Text = reply;
                    AddChatMessage(reply, isUser: false);
                    _ = SpeakAsync(reply);
                });
            }
        });
    }

    private async Task<bool> TryOpenAppByVoiceCommandAsync(string text)
    {
        var lower = text.ToLower();
        string? uri = null;
        string appName = "";

        // 🔒 STRICT BANKING SAFETY SHIELD (Nghiêm cấm truy cập ứng dụng ngân hàng và tài chính)
        string[] bankingKeywords = { 
            "ngân hàng", "bank", "vietcombank", "vcb", "techcombank", "tcb", "bidv", "agribank", 
            "mbbank", "mb bank", "tpbank", "vpbank", "vib", "acb", "sacombank", "shb", "hdbank",
            "momo", "zalopay", "viettelpay", "vnpay", "chuyển tiền", "rút tiền", "chuyển khoản",
            "tài khoản ngân hàng", "ví điện tử"
        };

        if (bankingKeywords.Any(k => lower.Contains(k)))
        {
            string safetyReply = "🛡️ Backend tuân thủ quy tắc bảo mật: Để đảm bảo an toàn tuyệt đối cho tài sản và tài khoản ngân hàng của bạn, Backend được lập trình nghiêm cấm truy cập vào các ứng dụng Ngân hàng và Ví điện tử ạ!";
            StatusLabel.Text = "🛡️ Bảo mật Ngân hàng";
            CurrentMsgLabel.Text = safetyReply;
            AddChatMessage(safetyReply, isUser: false);
            _ = SpeakAsync(safetyReply);
            return true;
        }

        // 0. CAMERA & VIDEO SMART COMMANDS ("Mở camera", "Bật máy ảnh", "Quay video")
        if (lower.Contains("mở camera") || lower.Contains("bật camera") || lower.Contains("mở máy ảnh") || lower.Contains("bật máy ảnh") || lower.Contains("chụp ảnh"))
        {
            string reply = "Dạ, Backend đang mở Camera chụp ảnh cho bạn đây ạ!";
            StatusLabel.Text = "📸 Đang mở Camera...";
            CurrentMsgLabel.Text = reply;
            AddChatMessage(reply, isUser: false);
            _ = SpeakAsync(reply);

            await Task.Delay(800);
            try
            {
                if (MediaPicker.Default.IsCaptureSupported)
                {
                    await MediaPicker.Default.CapturePhotoAsync();
                }
            }
            catch { }
            return true;
        }

        if (lower.Contains("quay video") || lower.Contains("quay phim") || lower.Contains("quay clip"))
        {
            string reply = "Dạ, Backend đang mở chế độ Quay Video cho bạn đây ạ!";
            StatusLabel.Text = "🎥 Đang mở Quay Video...";
            CurrentMsgLabel.Text = reply;
            AddChatMessage(reply, isUser: false);
            _ = SpeakAsync(reply);

            await Task.Delay(800);
            try
            {
                if (MediaPicker.Default.IsCaptureSupported)
                {
                    await MediaPicker.Default.CaptureVideoAsync();
                }
            }
            catch { }
            return true;
        }

        // 0.0 VOLUME CONTROL COMMANDS ("Tăng âm lượng", "Giảm âm lượng")
        if (lower.Contains("tăng âm lượng") || lower.Contains("to âm lượng") || lower.Contains("bật to lên") || lower.Contains("max âm lượng"))
        {
            _currentVolume = Math.Min(1.0f, _currentVolume + 0.3f);
            string reply = $"Dạ, Backend đã tăng âm lượng phát giọng nói lên {(int)(_currentVolume * 100)}% cho bạn rồi ạ!";
            StatusLabel.Text = "🔊 Tăng âm lượng";
            CurrentMsgLabel.Text = reply;
            AddChatMessage(reply, isUser: false);
            _ = SpeakAsync(reply);
            return true;
        }

        if (lower.Contains("giảm âm lượng") || lower.Contains("nhỏ âm lượng") || lower.Contains("bật nhỏ lại"))
        {
            _currentVolume = Math.Max(0.2f, _currentVolume - 0.3f);
            string reply = $"Dạ, Backend đã giảm âm lượng phát giọng nói xuống {(int)(_currentVolume * 100)}% cho bạn rồi ạ!";
            StatusLabel.Text = "🔉 Giảm âm lượng";
            CurrentMsgLabel.Text = reply;
            AddChatMessage(reply, isUser: false);
            _ = SpeakAsync(reply);
            return true;
        }

        // 0. TIME & DATE SMART COMMANDS ("Mấy giờ rồi", "Hôm nay ngày mấy")
        if (lower.Contains("mấy giờ") || lower.Contains("xem giờ") || lower.Contains("thời gian"))
        {
            var now = DateTime.Now;
            string reply = $"Dạ, bây giờ là {now:HH:mm} (giờ Việt Nam). Chúc bạn có một khoảng thời gian tuyệt vời!";
            StatusLabel.Text = "⏰ Xem giờ";
            CurrentMsgLabel.Text = reply;
            AddChatMessage(reply, isUser: false);
            _ = SpeakAsync(reply);
            return true;
        }
        if (lower.Contains("ngày mấy") || lower.Contains("ngày bao nhiêu") || lower.Contains("thứ mấy"))
        {
            var now = DateTime.Now;
            string reply = $"Dạ, hôm nay là {now:dddd, dd/MM/yyyy} ạ!";
            StatusLabel.Text = "📅 Xem ngày";
            CurrentMsgLabel.Text = reply;
            AddChatMessage(reply, isUser: false);
            _ = SpeakAsync(reply);
            return true;
        }

        // 0.1 WEATHER SMART COMMANDS ("Thời tiết hôm nay", "Thời tiết...")
        if (lower.Contains("thời tiết") || lower.Contains("nhiệt độ") || lower.Contains("mưa không"))
        {
            string reply = "Dạ, dự báo thời tiết hôm nay trời mây thoáng, nhiệt độ khoảng 28°C - 32°C, rất lý tưởng cho công việc của bạn ạ!";
            StatusLabel.Text = "☀️ Thời tiết";
            CurrentMsgLabel.Text = reply;
            AddChatMessage(reply, isUser: false);
            _ = SpeakAsync(reply);
            return true;
        }

        // 1. MAPS NAVIGATION COMMANDS ("Chỉ đường tới...", "Dẫn đường đi...")
        if (lower.Contains("chỉ đường") || lower.Contains("dẫn đường") || lower.Contains("tìm đường"))
        {
            string destination = text.Replace("chỉ đường tới", "", StringComparison.OrdinalIgnoreCase)
                                     .Replace("chỉ đường đi", "", StringComparison.OrdinalIgnoreCase)
                                     .Replace("dẫn đường đi", "", StringComparison.OrdinalIgnoreCase)
                                     .Replace("dẫn đường tới", "", StringComparison.OrdinalIgnoreCase)
                                     .Replace("tìm đường đi", "", StringComparison.OrdinalIgnoreCase)
                                     .Replace("tìm đường tới", "", StringComparison.OrdinalIgnoreCase)
                                     .Trim();

            if (string.IsNullOrWhiteSpace(destination)) destination = "Hà Nội";
            string mapsUrl = $"http://maps.apple.com/?daddr={Uri.EscapeDataString(destination)}";
            string reply = $"Dạ, em đang khởi chạy Bản đồ dẫn đường tới {destination} cho bạn!";

            StatusLabel.Text = "🗺️ Đang khởi chạy Bản đồ...";
            CurrentMsgLabel.Text = reply;
            AddChatMessage(reply, isUser: false);
            _ = SpeakAsync(reply);

            await Task.Delay(800);
            await Launcher.Default.OpenAsync(new Uri(mapsUrl));
            return true;
        }

        // 2. ZALO CALL / MESSAGE COMMANDS ("Gọi Zalo cho...", "Nhắn Zalo cho...")
        if (lower.Contains("gọi zalo") || lower.Contains("nhắn zalo") || lower.Contains("chát zalo"))
        {
            string target = text.Replace("gọi zalo cho", "", StringComparison.OrdinalIgnoreCase)
                                .Replace("nhắn zalo cho", "", StringComparison.OrdinalIgnoreCase)
                                .Replace("gọi zalo", "", StringComparison.OrdinalIgnoreCase)
                                .Replace("nhắn zalo", "", StringComparison.OrdinalIgnoreCase)
                                .Trim();

            string reply = string.IsNullOrWhiteSpace(target)
                ? "Dạ, em mở Zalo cho bạn đây ạ!"
                : $"Dạ, em đang mở Zalo để bạn liên hệ với {target} ạ!";

            StatusLabel.Text = "💬 Đang mở Zalo...";
            CurrentMsgLabel.Text = reply;
            AddChatMessage(reply, isUser: false);
            _ = SpeakAsync(reply);

            await Task.Delay(800);
            try { await Launcher.Default.OpenAsync(new Uri("zalo://")); }
            catch { await Launcher.Default.OpenAsync(new Uri("https://zalo.me")); }
            return true;
        }

        // 3. PHONE CALL COMMANDS ("Gọi điện cho...", "Gọi cho...")
        if (lower.Contains("gọi điện cho") || lower.Contains("gọi điện") || lower.Contains("gọi cho"))
        {
            string target = text.Replace("gọi điện cho", "", StringComparison.OrdinalIgnoreCase)
                                .Replace("gọi cho", "", StringComparison.OrdinalIgnoreCase)
                                .Replace("gọi điện", "", StringComparison.OrdinalIgnoreCase)
                                .Trim();

            // Extract phone number if digits present
            var digitsOnly = new string(target.Where(char.IsDigit).ToArray());
            if (!string.IsNullOrEmpty(digitsOnly) && digitsOnly.Length >= 3)
            {
                string callUri = $"tel:{digitsOnly}";
                string reply = $"Dạ, em đang kết nối cuộc gọi tới số {digitsOnly} cho bạn!";
                StatusLabel.Text = "📞 Đang gọi điện...";
                CurrentMsgLabel.Text = reply;
                AddChatMessage(reply, isUser: false);
                _ = SpeakAsync(reply);

                await Task.Delay(800);
                await Launcher.Default.OpenAsync(new Uri(callUri));
                return true;
            }
            else
            {
                string reply = $"Dạ, em đang mở ứng dụng Điện thoại & Danh bạ để bạn gọi cho {target} đây ạ!";
                StatusLabel.Text = "📞 Đang mở Danh bạ...";
                CurrentMsgLabel.Text = reply;
                AddChatMessage(reply, isUser: false);
                _ = SpeakAsync(reply);

                await Task.Delay(800);
                try { await Launcher.Default.OpenAsync(new Uri("tel:")); } catch { }
                return true;
            }
        }

        // 4. APP LAUNCHERS (YouTube, Facebook, TikTok, Maps)
        if (lower.Contains("mở youtube") || lower.Contains("bật youtube"))
        {
            uri = "youtube://";
            appName = "YouTube";
        }
        else if (lower.Contains("mở zalo") || lower.Contains("bật zalo"))
        {
            uri = "zalo://";
            appName = "Zalo";
        }
        else if (lower.Contains("mở facebook") || lower.Contains("bật facebook") || lower.Contains("mở fb"))
        {
            uri = "fb://";
            appName = "Facebook";
        }
        else if (lower.Contains("mở tiktok") || lower.Contains("bật tiktok"))
        {
            uri = "snssdk1128://";
            appName = "TikTok";
        }
        else if (lower.Contains("mở bản đồ") || lower.Contains("mở maps"))
        {
            uri = "maps://";
            appName = "Bản đồ";
        }

        if (uri != null)
        {
            string reply = $"Dạ, em đang mở ứng dụng {appName} cho bạn đây ạ!";
            StatusLabel.Text = "🚀 Đang mở ứng dụng...";
            CurrentMsgLabel.Text = reply;
            AddChatMessage(reply, isUser: false);
            _ = SpeakAsync(reply);

            await Task.Delay(800);
            try
            {
                await Launcher.Default.OpenAsync(new Uri(uri));
                return true;
            }
            catch
            {
                if (appName == "YouTube") await Launcher.Default.OpenAsync(new Uri("https://youtube.com"));
                else if (appName == "Facebook") await Launcher.Default.OpenAsync(new Uri("https://facebook.com"));
                return true;
            }
        }
        return false;
    }

    private void AppendOrAddAiResponse(string chunkText)
    {
        if (string.IsNullOrWhiteSpace(chunkText)) return;

        if (string.IsNullOrEmpty(_currentAiStreamText))
        {
            _currentAiStreamText = chunkText;
            _currentAiBubbleLabel = AddChatMessage(_currentAiStreamText, isUser: false);
        }
        else
        {
            _currentAiStreamText += " " + chunkText;
            if (_currentAiBubbleLabel != null)
            {
                _currentAiBubbleLabel.Text = _currentAiStreamText;
            }
            else
            {
                _currentAiBubbleLabel = AddChatMessage(_currentAiStreamText, isUser: false);
            }
        }

        CurrentMsgLabel.Text = _currentAiStreamText;
        if (!_hasReceivedServerAudio)
        {
            _ = EnqueueSpeechAsync(chunkText);
        }
    }

    private async Task EnqueueSpeechAsync(string text)
    {
        var cleanText = text.Replace("~", "").Replace("*", "").Replace("_", "").Trim();
        if (string.IsNullOrWhiteSpace(cleanText)) return;

        _speechQueue.Enqueue(cleanText);
        if (!_isSpeaking)
        {
            await ProcessSpeechQueueAsync();
        }
    }

    private async Task ProcessSpeechQueueAsync()
    {
        _isSpeaking = true;

        Locale? viLocale = null;
        try
        {
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            viLocale = locales.FirstOrDefault(l => l.Language != null && l.Language.StartsWith("vi", StringComparison.OrdinalIgnoreCase));
        }
        catch { }

        while (_speechQueue.TryDequeue(out var textToSpeak))
        {
            try
            {
                var options = new SpeechOptions
                {
                    Volume = _currentVolume,
                    Pitch = 1.0f // Giữ nguyên cao độ tự nhiên chuẩn theo cấu hình từ Server Xiaozhi
                };
                if (viLocale != null)
                {
                    options.Locale = viLocale;
                }

                await TextToSpeech.Default.SpeakAsync(textToSpeak, options);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TTS Exception: {ex.Message}");
            }
        }
        _isSpeaking = false;

        if (_handsFree && !_isRecording)
        {
            await StartRecordingAsync();
        }
    }

    private async Task SpeakAsync(string text)
    {
        await EnqueueSpeechAsync(text);
    }

    private async void OnPlusClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet("Tùy chọn BACKEND AI", "Hủy", null, "📸 Ghi âm giọng nói", "🖼️ Tải hình ảnh", "⚙️ Cài đặt Server");
        if (action == "⚙️ Cài đặt Server")
        {
            OnSettingsClicked(sender, e);
        }
        else if (action == "📸 Ghi âm giọng nói")
        {
            OnMicButtonClicked(sender, e);
        }
    }

    private Label? AddChatMessage(string text, bool isUser)
    {
        if (isUser)
        {
            // User Bubble (Gemini pill on right)
            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#2F2F2F"),
                CornerRadius = 18,
                Padding = new Thickness(14, 10),
                HasShadow = false,
                BorderColor = Colors.Transparent,
                HorizontalOptions = LayoutOptions.End,
                MaximumWidthRequest = 290
            };

            var label = new Label
            {
                Text = text,
                TextColor = Color.FromArgb("#F1F1F1"),
                FontSize = 15,
                LineHeight = 1.35
            };

            frame.Content = label;
            ChatStack.Children.Add(frame);
        }
        else
        {
            // AI Message (Gemini style clean text on black with action row)
            var container = new VerticalStackLayout
            {
                Spacing = 6,
                HorizontalOptions = LayoutOptions.Start,
                MaximumWidthRequest = 340
            };

            var aiText = new Label
            {
                Text = text,
                TextColor = Color.FromArgb("#E3E3E3"),
                FontSize = 15,
                LineHeight = 1.45
            };

            // Gemini Action Bar: Copy, Share, Speak, Like, Dislike
            var actionBar = new HorizontalStackLayout
            {
                Spacing = 12,
                Margin = new Thickness(0, 4, 0, 0)
            };

            var btnCopy = new Button { Text = "📋", BackgroundColor = Colors.Transparent, TextColor = Color.FromArgb("#C4C7C5"), FontSize = 14, Padding = 2, WidthRequest = 32, HeightRequest = 32 };
            btnCopy.Clicked += async (s, e) => { await Clipboard.SetTextAsync(text); await DisplayAlert("Thông báo", "Đã sao chép câu trả lời!", "OK"); };

            var btnSpeak = new Button { Text = "🔊", BackgroundColor = Colors.Transparent, TextColor = Color.FromArgb("#C4C7C5"), FontSize = 14, Padding = 2, WidthRequest = 32, HeightRequest = 32 };
            btnSpeak.Clicked += (s, e) => { _ = SpeakAsync(text); };

            var btnLike = new Button { Text = "👍", BackgroundColor = Colors.Transparent, TextColor = Color.FromArgb("#C4C7C5"), FontSize = 14, Padding = 2, WidthRequest = 32, HeightRequest = 32 };
            var btnDislike = new Button { Text = "👎", BackgroundColor = Colors.Transparent, TextColor = Color.FromArgb("#C4C7C5"), FontSize = 14, Padding = 2, WidthRequest = 32, HeightRequest = 32 };

            actionBar.Children.Add(btnCopy);
            actionBar.Children.Add(btnSpeak);
            actionBar.Children.Add(btnLike);
            actionBar.Children.Add(btnDislike);

            container.Children.Add(aiText);
            container.Children.Add(actionBar);

            ChatStack.Children.Add(container);
            ScrollToBottom(150);
            return aiText;
        }

        ScrollToBottom(150);
        return null;
    }

    private void ScrollToBottom(int delayMs = 150)
    {
        Task.Delay(delayMs).ContinueWith(_ =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await ChatScrollView.ScrollToAsync(ChatStack, ScrollToPosition.End, true);
                }
                catch { }
            });
        });
    }
}
