using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Xiaozhi.App.Wpf.ViewModels;
using Xiaozhi.App.Wpf.Views;
using Xiaozhi.Core.Models;
using Xiaozhi.Plugins;

namespace Xiaozhi.App.Wpf;

public partial class MainWindow : Window
{
    // ViewModel giữ toàn bộ trạng thái/nghiệp vụ; code-behind này chỉ điều phối sự kiện và cập nhật giao diện.
    private readonly MainViewModel _vm;
    private bool _isRecordingActive = false;
    private Stopwatch _pressTimer = new();
    private readonly GlobalShortcutPlugin _shortcutPlugin = new();

    public MainWindow()
    {
        // Nạp cây điều khiển từ MainWindow.xaml rồi lấy ViewModel đã khai báo trong DataContext.
        InitializeComponent();
        _vm = (MainViewModel)DataContext;

        // Đăng ký hai "kênh" thông báo từ ViewModel:
        // PropertyChanged dành cho trạng thái đơn (đang thu, đang nói, nội dung nhãn...).
        // MessageAdded dành cho một chat message mới cần tạo thành bong bóng trên giao diện.
        _vm.PropertyChanged += Vm_PropertyChanged;
        _vm.MessageAdded += OnMessageAdded;
        Loaded += async (s, e) =>
        {
            // Hotkey của Windows cần HWND thật, vì vậy chỉ đăng ký sau khi cửa sổ đã Loaded.
            var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            _shortcutPlugin.RegisterWindow(handle);
            _shortcutPlugin.OnManualTalkTriggered += async () =>
            {
                if (!_isRecordingActive) await _vm.StartListeningAsync();
                else await _vm.StopListeningAsync();
            };
            _shortcutPlugin.OnAutoTalkToggled += () => HandsFree_Click(this, new RoutedEventArgs());
            _shortcutPlugin.OnAbortTriggered += () => _ = _vm.AbortAsync();

            await _vm.InitializeAsync();
        };
        // Nhả các hotkey toàn cục khi cửa sổ đóng để ứng dụng khác có thể sử dụng lại.
        Unloaded += (s, e) => _ = _shortcutPlugin.ShutdownAsync();
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Callback mạng/audio có thể chạy ở luồng nền; Dispatcher đưa mọi thay đổi về UI thread.
        Dispatcher.Invoke(() =>
        {
            // e.PropertyName chính là tên do OnPropertyChanged tạo ra ở ViewModel.
            // nameof(...) tránh chuỗi viết tay: đổi tên property bằng refactor thì code vẫn đúng.
            switch (e.PropertyName)
            {
                case nameof(_vm.StatusText):
                    StatusLabel.Text = _vm.StatusText;
                    break;
                case nameof(_vm.CurrentChatMessage):
                    CurrentMsgLabel.Text = _vm.CurrentChatMessage;
                    break;
                case nameof(_vm.IsConnected):
                    StatusDot.Fill = _vm.IsConnected
                        ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                        : new SolidColorBrush(Color.FromRgb(255, 68, 68));
                    break;
                case nameof(_vm.IsRecording):
                    UpdateRecordingUI(_vm.IsRecording);
                    break;
                case nameof(_vm.IsSpeaking):
                    AvatarEmoji.Text = _vm.IsSpeaking ? "💬" : "🌸";
                    AbortBtnBorder.Visibility = _vm.IsSpeaking ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case nameof(_vm.HandsFreeMode):
                    UpdateHandsFreeUI(_vm.HandsFreeMode);
                    break;
            }
        });
    }

    private void UpdateHandsFreeUI(bool enabled)
    {
        // Đồng bộ nhãn và màu nút với trạng thái HandsFreeMode trong ViewModel.
        var label = (TextBlock?)HandsFreeBtn.Template.FindName("HandsFreeLabel", HandsFreeBtn);
        if (label != null)
        {
            label.Text = enabled ? "🎙️ Rảnh tay: BẬT" : "🎙️ Rảnh tay: Tắt";
        }
        HandsFreeBtn.Background = enabled
            ? new SolidColorBrush(Color.FromRgb(80, 30, 150))
            : new SolidColorBrush(Color.FromRgb(34, 34, 56));
    }

    private void UpdateRecordingUI(bool recording)
    {
        // Đổi nút micro và chạy/tắt vòng sáng nhấp nháy để phản hồi trạng thái thu âm.
        _isRecordingActive = recording;
        if (recording)
        {
            TalkBtnBorder.Background = new SolidColorBrush(Color.FromRgb(220, 40, 70));
            TalkBtnIcon.Text = "⏹";
            TalkBtnLabel.Text = "Đang nghe...";
            TalkHintLabel.Text = "🔴 Đang nghe... Nói xong AI sẽ tự gửi";
            CurrentMsgLabel.Text = "🎤 Đang lắng nghe... Nói xong dừng lại 1s để AI trả lời.";

            var pulse = new DoubleAnimation(0.0, 0.9, new Duration(TimeSpan.FromMilliseconds(500)))
            {
                AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever
            };
            PulseRing.BeginAnimation(OpacityProperty, pulse);
        }
        else
        {
            TalkBtnBorder.Background = new SolidColorBrush(Color.FromRgb(107, 63, 204));
            TalkBtnIcon.Text = "🎤";
            TalkBtnLabel.Text = "Bấm để nói";
            TalkHintLabel.Text = "👇 Bấm để nói (nói xong tự gửi)";
            PulseRing.BeginAnimation(OpacityProperty, null);
            PulseRing.Opacity = 0;
        }
    }

    private void OnMessageAdded(ChatMessage msg)
    {
        // Tạo bong bóng chat động: AI nằm trái, người dùng nằm phải.
        Dispatcher.Invoke(() =>
        {
            var isAi = msg.Role == "assistant";
            var bubble = new Border
            {
                CornerRadius = isAi
                    ? new CornerRadius(14, 14, 14, 4)
                    : new CornerRadius(14, 14, 4, 14),
                Background = isAi
                    ? new SolidColorBrush(Color.FromRgb(32, 32, 54))
                    : new SolidColorBrush(Color.FromRgb(85, 50, 175)),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = isAi ? new Thickness(0, 4, 50, 4) : new Thickness(50, 4, 0, 4),
                HorizontalAlignment = isAi ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                MaxWidth = 320
            };

            var content = new StackPanel();
            if (isAi)
            {
                content.Children.Add(new TextBlock
                {
                    Text = "🌸 Lily",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 140, 220)),
                    Margin = new Thickness(0, 0, 0, 3)
                });
            }
            content.Children.Add(new TextBlock
            {
                Text = msg.Content,
                TextWrapping = TextWrapping.Wrap,
                Foreground = isAi ? new SolidColorBrush(Color.FromRgb(235, 235, 250)) : new SolidColorBrush(Colors.White),
                FontSize = 13
            });

            bubble.Child = content;

            var wrapper = new Grid();
            wrapper.Children.Add(bubble);
            ChatStack.Children.Add(wrapper);
            ChatScroll.ScrollToEnd();
        });
    }

    private async void TalkBtn_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Một lần bấm sẽ bật/tắt micro; Stopwatch còn hỗ trợ thao tác nhấn giữ.
        _pressTimer.Restart();

        if (!_isRecordingActive)
        {
            await _vm.StartListeningAsync();
        }
        else
        {
            await _vm.StopListeningAsync();
        }
        e.Handled = true;
    }

    private void TalkBtn_MouseUp(object sender, MouseButtonEventArgs e)
    {
        // Nhấn giữ quá 600 ms có nghĩa là dừng thu khi người dùng thả chuột.
        _pressTimer.Stop();
        if (_pressTimer.ElapsedMilliseconds > 600 && _isRecordingActive)
        {
            _ = _vm.StopListeningAsync();
        }
        e.Handled = true;
    }

    /// <summary>Nếu con trỏ rời nút sau một lần nhấn giữ, dừng thu giống thao tác thả chuột.</summary>
    private void TalkBtn_MouseLeave(object sender, MouseEventArgs e)
    {
        _pressTimer.Stop();
        if (_pressTimer.ElapsedMilliseconds > 600 && _isRecordingActive)
        {
            _ = _vm.StopListeningAsync();
        }
    }

    /// <summary>Chuyển thao tác bấm nút Gửi về cùng một hàm xử lý nội dung ô nhập.</summary>
    private void SendText_Click(object sender, RoutedEventArgs e)
    {
        SendCurrentText();
    }

    /// <summary>Bắt Enter sớm, kể cả khi IME tiếng Việt đang xử lý phím.</summary>
    private void TxtInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var effectiveKey = e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key;
        if (effectiveKey == Key.Enter || effectiveKey == Key.Return)
        {
            SendCurrentText();
            e.Handled = true;
        }
    }

    /// <summary>Fallback bắt Enter ở sự kiện KeyDown thông thường.</summary>
    private void TxtInput_KeyDown(object sender, KeyEventArgs e)
    {
        var effectiveKey = e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key;
        if (effectiveKey == Key.Enter || effectiveKey == Key.Return)
        {
            SendCurrentText();
            e.Handled = true;
        }
    }

    private void SendCurrentText()
    {
        // Chuẩn hóa đầu vào, xóa ô nhập ngay và giao việc gửi bất đồng bộ cho ViewModel.
        var text = TxtInput.Text?.Trim();
        if (!string.IsNullOrEmpty(text))
        {
            TxtInput.Clear();
            _ = _vm.SendTextMessageAsync(text);
        }
    }

    /// <summary>Chuyển thao tác bấm nút hủy thành lệnh abort bất đồng bộ trên ViewModel.</summary>
    private void AbortBtn_Click(object sender, MouseButtonEventArgs e)
    {
        _ = _vm.AbortAsync();
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        // Chỉ kết nối lại khi hộp thoại trả về true, tức cấu hình mới đã được lưu.
        var win = new SettingsWindow();
        win.Owner = this;
        if (win.ShowDialog() == true)
        {
            _ = _vm.ReconnectAsync();
        }
    }

    private async void RefreshSync_Click(object sender, RoutedEventArgs e)
    {
        // Reconnect chạy lại cả bước OTA discovery nên cũng đồng bộ URL/token mới từ server.
        StatusLabel.Text = "🔄 Đang đồng bộ...";
        CurrentMsgLabel.Text = "⏳ Đang kéo cấu hình mới nhất từ web xiaozhi.me...";
        await _vm.ReconnectAsync();
        CurrentMsgLabel.Text = "✅ Đã đồng bộ cấu hình thành công! Bạn có thể nói chuyện ngay.";
    }

    private bool _isIPhoneMode = false;
    private void SimulateIPhone_Click(object sender, RoutedEventArgs e)
    {
        // Đây chỉ là chế độ mô phỏng kích thước/khung iPhone, không thay đổi nền tảng chạy.
        _isIPhoneMode = !_isIPhoneMode;
        if (_isIPhoneMode)
        {
            Width = 390;
            Height = 844;
            IPhoneClock.Text = DateTime.Now.ToString("HH:mm");
            IPhoneHeaderBar.Visibility = Visibility.Visible;
            IPhoneHomeBar.Visibility = Visibility.Visible;
            Title = "Lily - AI Assistant (iPhone 15 Pro Frame Mode)";
            CurrentMsgLabel.Text = "📱 Đã chuyển sang giao diện iPhone 15 Pro giả lập (Dynamic Island + Status Bar + Home Bar).";
        }
        else
        {
            Width = 440;
            Height = 720;
            IPhoneHeaderBar.Visibility = Visibility.Collapsed;
            IPhoneHomeBar.Visibility = Visibility.Collapsed;
            Title = "Lily - AI Assistant (.NET 10)";
            CurrentMsgLabel.Text = "💻 Đã quay lại kích thước màn hình PC Windows mặc định.";
        }
    }

    private async void HandsFree_Click(object sender, RoutedEventArgs e)
    {
        // Hands-free tạo vòng lặp: nghe -> server trả lời -> timer TTS bật nghe lại.
        _vm.HandsFreeMode = !_vm.HandsFreeMode;
        if (_vm.HandsFreeMode)
        {
            CurrentMsgLabel.Text = "🎙️ Đã BẬT chế độ Rảnh tay! Bạn chỉ cần nói, AI sẽ tự động trả lời liên tục.";
            await _vm.StartListeningAsync();
        }
        else
        {
            CurrentMsgLabel.Text = "🎙️ Đã tắt chế độ Rảnh tay.";
            if (_vm.IsRecording) await _vm.StopListeningAsync();
        }
    }
}
