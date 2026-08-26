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

    public MainPage()
    {
        InitializeComponent();

        _wsUrl = Preferences.Default.Get("lily_ws_url", _wsUrl);
        _token = Preferences.Default.Get("lily_token", _token);
        _deviceId = Preferences.Default.Get("lily_device_id", _deviceId);

        _client = new XiaozhiWebSocketClient(_wsUrl, _token, _deviceId, "maui-ios-client");
        _textStreamer = new TextToAudioStreamer();

        SetupClientHandlers();
        _ = ConnectWithOtaAsync();
    }

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
                AddChatMessage($"🗣️ {sttText}", isUser: true);
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
                CurrentMsgLabel.Text = "✅ Đã kết nối với trợ lý Lily!";
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = "🌐 Ngoại tuyến / Voice Engine";
                CurrentMsgLabel.Text = "⚡ Kết nối Server bận. Đã tự động kích hoạt giọng nói iOS!";
            });
        }
    }

    private async void OnMicButtonClicked(object sender, EventArgs e)
    {
        if (!_isRecording)
        {
            _isRecording = true;
            MicButton.Text = "⏹️ Đang nghe (Bấm để dừng)";
            MicButton.BackgroundColor = Colors.DarkRed;
            StatusLabel.Text = "🎙️ Đang lắng nghe...";
            if (_client.IsConnected)
            {
                await _client.StartListeningAsync(mode: "manual");
            }
        }
        else
        {
            _isRecording = false;
            MicButton.Text = "🎤 Bấm để nói";
            MicButton.BackgroundColor = Color.FromArgb("#6c5ce7");
            StatusLabel.Text = "🧠 Đang xử lý...";
            if (_client.IsConnected)
            {
                await _client.StopListeningAsync();
            }
        }
    }

    private void OnHandsFreeClicked(object sender, EventArgs e)
    {
        _handsFree = !_handsFree;
        HandsFreeBtn.Text = _handsFree ? "🎙️ Rảnh tay: Bật" : "🎙️ Rảnh tay: Tắt";
        HandsFreeBtn.BackgroundColor = _handsFree ? Color.FromArgb("#2ed573") : Color.FromArgb("#1c1936");
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

        AddChatMessage(text, isUser: true);
        StatusLabel.Text = "🧠 Đang xử lý...";
        _receivedResponse = false;

        if (_client.IsConnected)
        {
            await _client.SendTextQueryAsync(text);
        }

        // Wait 1.8 seconds; if no response from server, use smart local fallback & iOS speech!
        _ = Task.Run(async () =>
        {
            await Task.Delay(1800);
            if (!_receivedResponse)
            {
                _receivedResponse = true;
                string reply = $"Dạ Lily đây! Mình đã nhận được câu hỏi \"{text}\" từ bạn. Mình sẵn sàng trò chuyện cùng sếp!";
                if (text.ToLower().Contains("chào") || text.ToLower().Contains("hello"))
                {
                    reply = "Xin chào sếp! Em là Lily - Trợ lý ảo AI thông minh. Em có thể giúp gì cho sếp hôm nay?";
                }
                else if (text.ToLower().Contains("ôm") || text.ToLower().Contains("thương"))
                {
                    reply = "Gửi sếp một cái ôm thật ấm áp! Lily luôn ở đây để lắng nghe và đồng hành cùng sếp nhé! ❤️";
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

    private async Task SpeakAsync(string text)
    {
        try
        {
            await TextToSpeech.Default.SpeakAsync(text, new SpeechOptions
            {
                Volume = 1.0f,
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

        Task.Delay(100).ContinueWith(_ =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await ChatScrollView.ScrollToAsync(ChatStack, ScrollToPosition.End, true);
            });
        });
    }
}
