using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Xiaozhi.Audio.Services;
using Xiaozhi.Protocols.WebSocket;

namespace Xiaozhi.App.Maui;

public partial class MainPage : ContentPage
{
    private XiaozhiWebSocketClient _client;
    private readonly TextToAudioStreamer _textStreamer;
    private bool _isRecording = false;
    private bool _handsFree = false;
    private bool _receivedResponse = false;

    private string _wsUrl = "wss://api.tenclass.net/xiaozhi/v1/";
    private string _token = "test-token";
    private string _deviceId = "a0:36:bc:2c:ed:40";

    private float _currentVolume = 1.0f;
    private readonly VoiceActivityDetector _vad = new();

    public MainPage()
    {
        InitializeComponent();

        _wsUrl = Preferences.Default.Get("lily_ws_url", _wsUrl);
        _token = Preferences.Default.Get("lily_token", _token);
        _deviceId = Preferences.Default.Get("lily_device_id", _deviceId);

        _client = new XiaozhiWebSocketClient(_wsUrl, _token, _deviceId, "maui-ios-client");
        _textStreamer = new TextToAudioStreamer();

        // Auto VAD Silence Detection (Matching WPF App)
        _vad.OnSpeechEnded += () =>
        {
            if (_isRecording)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await StopRecordingAndProcessAsync();
                });
            }
        };

        SetupClientHandlers();
        _ = ConnectWithOtaAsync();

        // Auto-Scroll chat content above keyboard when typing
        TextInput.Focused += (s, e) => ScrollToBottom(350);
    }

    private string _lastSentText = string.Empty;

    private void SetupClientHandlers()
    {
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
                StatusLabel.Text = "✅ Sẵn sàng";
                CurrentMsgLabel.Text = text;
                AddChatMessage(text, isUser: false);
                _ = SpeakAsync(text);
            });
            await Task.CompletedTask;
        };
    }

    private async Task ConnectWithOtaAsync()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = "🔄 Đang kết nối...";
        });

        try
        {
            await _client.ConnectAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = "✅ Sẵn sàng";
                CurrentMsgLabel.Text = "✅ Đã kết nối với trợ lý Tony!";
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
        MicButton.Text = "🎙️";
        MicButton.BackgroundColor = Colors.Transparent;
        MicButton.TextColor = Color.FromArgb("#7C5CFC");
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

    private void OnHandsFreeClicked(object sender, EventArgs e)
    {
        _handsFree = !_handsFree;
        HandsFreeBtn.Text = _handsFree ? "🎙️ Rảnh tay: Bật" : "🎙️ Rảnh tay: Tắt";
        HandsFreeBtn.BackgroundColor = _handsFree ? Color.FromArgb("#2F8F68") : Color.FromArgb("#171A20");
        HandsFreeBtn.TextColor = _handsFree ? Colors.White : Color.FromArgb("#B8C1CC");
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await ConnectWithOtaAsync();
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        var settingsPage = new SettingsPage();
        settingsPage.SettingsSaved += async (s, args) =>
        {
            _wsUrl = Preferences.Default.Get("lily_ws_url", _wsUrl);
            _token = Preferences.Default.Get("lily_token", _token);
            _deviceId = Preferences.Default.Get("lily_device_id", _deviceId);
            var clientId = Preferences.Default.Get("lily_client_id", "maui-ios-client");

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
        AddChatMessage(text, isUser: true);
        StatusLabel.Text = "🧠 Đang xử lý...";
        _receivedResponse = false;

        if (_client.IsConnected)
        {
            await _client.SendTextQueryAsync(text);
        }

        // Check for iOS App Launcher voice commands
        if (await TryOpenAppByVoiceCommandAsync(text)) return;

        // Wait 1.8 seconds; if no response from server, use smart local fallback & iOS speech!
        _ = Task.Run(async () =>
        {
            await Task.Delay(1800);
            if (!_receivedResponse)
            {
                _receivedResponse = true;
                string reply = $"Dạ SUSU FILM AI đây! Mình đã nhận được câu hỏi \"{text}\" từ bạn. Mình sẵn sàng hỗ trợ sếp!";
                if (text.ToLower().Contains("chào") || text.ToLower().Contains("hello"))
                {
                    reply = "Xin chào sếp! Em là SUSU FILM AI. Em có thể giúp gì cho sếp hôm nay?";
                }
                else if (text.ToLower().Contains("ôm") || text.ToLower().Contains("thương"))
                {
                    reply = "Gửi sếp một cái ôm thật ấm áp! SUSU FILM AI luôn ở đây để lắng nghe và đồng hành cùng sếp nhé! ❤️";
                }

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
            string safetyReply = "🛡️ Tony tuân thủ quy tắc bảo mật: Để đảm bảo an toàn tuyệt đối cho tài sản và tài khoản ngân hàng của sếp, Tony được lập trình nghiêm cấm truy cập vào các ứng dụng Ngân hàng và Ví điện tử ạ!";
            StatusLabel.Text = "🛡️ Bảo mật Ngân hàng";
            CurrentMsgLabel.Text = safetyReply;
            AddChatMessage(safetyReply, isUser: false);
            _ = SpeakAsync(safetyReply);
            return true;
        }

        // 0. CAMERA & VIDEO SMART COMMANDS ("Mở camera", "Bật máy ảnh", "Quay video")
        if (lower.Contains("mở camera") || lower.Contains("bật camera") || lower.Contains("mở máy ảnh") || lower.Contains("bật máy ảnh") || lower.Contains("chụp ảnh"))
        {
            string reply = "Dạ, Tony đang mở Camera chụp ảnh cho sếp đây ạ!";
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
            string reply = "Dạ, Tony đang mở chế độ Quay Video cho sếp đây ạ!";
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
            string reply = $"Dạ, Tony đã tăng âm lượng phát giọng nói lên {(int)(_currentVolume * 100)}% cho sếp rồi ạ!";
            StatusLabel.Text = "🔊 Tăng âm lượng";
            CurrentMsgLabel.Text = reply;
            AddChatMessage(reply, isUser: false);
            _ = SpeakAsync(reply);
            return true;
        }

        if (lower.Contains("giảm âm lượng") || lower.Contains("nhỏ âm lượng") || lower.Contains("bật nhỏ lại"))
        {
            _currentVolume = Math.Max(0.2f, _currentVolume - 0.3f);
            string reply = $"Dạ, Tony đã giảm âm lượng phát giọng nói xuống {(int)(_currentVolume * 100)}% cho sếp rồi ạ!";
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
            string reply = $"Dạ, bây giờ là {now:HH:mm} (giờ Việt Nam). Chúc sếp có một khoảng thời gian tuyệt vời!";
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
            string reply = "Dạ, dự báo thời tiết hôm nay trời mây thoáng, nhiệt độ khoảng 28°C - 32°C, rất lý tưởng cho công việc của sếp ạ!";
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
            string reply = $"Dạ, em đang khởi chạy Bản đồ dẫn đường tới {destination} cho sếp!";

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
                ? "Dạ, em mở Zalo cho sếp đây ạ!"
                : $"Dạ, em đang mở Zalo để sếp liên hệ với {target} ạ!";

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
                string reply = $"Dạ, em đang kết nối cuộc gọi tới số {digitsOnly} cho sếp!";
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
                string reply = $"Dạ, em đang mở ứng dụng Điện thoại & Danh bạ để sếp gọi cho {target} đây ạ!";
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
            string reply = $"Dạ, em đang mở ứng dụng {appName} cho sếp đây ạ!";
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

    private async Task SpeakAsync(string text)
    {
        try
        {
            await TextToSpeech.Default.SpeakAsync(text, new SpeechOptions
            {
                Volume = _currentVolume,
                Pitch = 1.1f
            });
        }
        catch { }
    }

    private async void OnPlusClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet("Tùy chọn SUSU FILM AI", "Hủy", null, "📸 Ghi âm giọng nói", "🖼️ Tải hình ảnh", "⚙️ Cài đặt Server");
        if (action == "⚙️ Cài đặt Server")
        {
            OnSettingsClicked(sender, e);
        }
        else if (action == "📸 Ghi âm giọng nói")
        {
            OnMicButtonClicked(sender, e);
        }
    }

    private void AddChatMessage(string text, bool isUser)
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
        }

        ScrollToBottom(150);
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
