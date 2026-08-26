using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Xiaozhi.Audio.Services;
using Xiaozhi.Protocols.WebSocket;

namespace Xiaozhi.App.Maui;

public partial class MainPage : ContentPage
{
    private readonly XiaozhiWebSocketClient _client;
    private readonly TextToAudioStreamer _textStreamer;
    private bool _isRecording = false;
    private bool _handsFree = false;

    public MainPage()
    {
        InitializeComponent();
        _client = new XiaozhiWebSocketClient(
            "wss://api.tenclass.net/xiaozhi/v1/",
            "78:21:84:8c:a8:8c",
            "Bearer 01925b68-6058-7505-87d2-3c22ad39e083",
            "csharp-maui-ios-device-01"
        );
        _textStreamer = new TextToAudioStreamer();

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
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentMsgLabel.Text = text;
                AddChatMessage(text, isUser: false);
            });
            await Task.CompletedTask;
        };

        Task.Run(async () =>
        {
            await _client.ConnectAsync();
        });
    }

    private async void OnMicButtonClicked(object sender, EventArgs e)
    {
        if (!_isRecording)
        {
            _isRecording = true;
            MicButton.Text = "⏹️ Đang nghe (Bấm để dừng)";
            MicButton.BackgroundColor = Colors.DarkRed;
            StatusLabel.Text = "🎙️ Đang lắng nghe...";
            await _client.StartListeningAsync(mode: "manual");
        }
        else
        {
            _isRecording = false;
            MicButton.Text = "🎤 Bấm để nói";
            MicButton.BackgroundColor = Color.FromArgb("#6c5ce7");
            StatusLabel.Text = "🧠 Đang xử lý...";
            await _client.StopListeningAsync();
        }
    }

    private void OnHandsFreeClicked(object sender, EventArgs e)
    {
        _handsFree = !_handsFree;
        HandsFreeBtn.Text = _handsFree ? "🎙️ Rảnh tay: Bật" : "🎙️ Rảnh tay: Tắt";
        HandsFreeBtn.BackgroundColor = _handsFree ? Color.FromArgb("#2ed573") : Color.FromArgb("#1c1936");
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        var text = TextInput.Text;
        if (string.IsNullOrWhiteSpace(text)) return;
        TextInput.Text = string.Empty;

        AddChatMessage(text, isUser: true);
        StatusLabel.Text = "🧠 Đang gửi câu hỏi...";

        if (text.Length <= 8)
        {
            await _client.SendTextQueryAsync(text);
        }
        else
        {
            _ = Task.Run(async () =>
            {
                await _textStreamer.StreamTextAsAudioAsync(_client, text);
            });
        }
    }

    private void AddChatMessage(string text, bool isUser)
    {
        var frame = new Frame
        {
            BackgroundColor = isUser ? Color.FromArgb("#6c5ce7") : Color.FromArgb("#18152e"),
            CornerRadius = 12,
            Padding = 12,
            HasShadow = false,
            BorderColor = Colors.Transparent,
            HorizontalOptions = isUser ? LayoutOptions.End : LayoutOptions.Start,
            MaximumWidthRequest = 280
        };

        var label = new Label
        {
            Text = text,
            TextColor = Colors.White,
            FontSize = 15
        };

        frame.Content = label;
        ChatStack.Children.Add(frame);

        Task.Delay(100).ContinueWith(_ =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await ChatScrollView.ScrollToAsync(ChatStack, ScrollToPosition.End, true);
            });
        });
    }
}
