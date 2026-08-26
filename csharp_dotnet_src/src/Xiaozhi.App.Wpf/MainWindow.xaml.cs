using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Xiaozhi.App.Wpf.ViewModels;
using Xiaozhi.App.Wpf.Views;
using Xiaozhi.Core.Models;

namespace Xiaozhi.App.Wpf;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private bool _isRecordingActive = false;
    private Stopwatch _pressTimer = new();

    public MainWindow()
    {
        InitializeComponent();
        _vm = (MainViewModel)DataContext;
        _vm.PropertyChanged += Vm_PropertyChanged;
        _vm.MessageAdded += OnMessageAdded;
        Loaded += async (s, e) => await _vm.InitializeAsync();
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
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
            }
        });
    }

    private void UpdateRecordingUI(bool recording)
    {
        _isRecordingActive = recording;
        if (recording)
        {
            TalkBtnBorder.Background = new SolidColorBrush(Color.FromRgb(220, 40, 70));
            TalkBtnIcon.Text = "⏹";
            TalkBtnLabel.Text = "Bấm để dừng và gửi";
            TalkHintLabel.Text = "🔴 Đang ghi âm — Bấm lại nút này khi nói xong";
            CurrentMsgLabel.Text = "🎤 Đang lắng nghe giọng nói của bạn...";

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
            TalkHintLabel.Text = "👇 Bấm 1 lần để nói, bấm lại để gửi (hoặc giữ để nói)";
            PulseRing.BeginAnimation(OpacityProperty, null);
            PulseRing.Opacity = 0;
        }
    }

    private void OnMessageAdded(ChatMessage msg)
    {
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

    // Toggle on Click & Support Long-Press
    private void TalkBtn_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!_vm.IsConnected) return;
        _pressTimer.Restart();

        if (!_isRecordingActive)
        {
            _ = _vm.StartListeningAsync();
        }
        else
        {
            // Already recording -> click to stop
            _ = _vm.StopListeningAsync();
        }
        e.Handled = true;
    }

    private void TalkBtn_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _pressTimer.Stop();
        // If held for longer than 600ms, release acts as stop (Push-to-Talk)
        if (_pressTimer.ElapsedMilliseconds > 600 && _isRecordingActive)
        {
            _ = _vm.StopListeningAsync();
        }
        e.Handled = true;
    }

    private void TalkBtn_MouseLeave(object sender, MouseEventArgs e)
    {
        _pressTimer.Stop();
        if (_pressTimer.ElapsedMilliseconds > 600 && _isRecordingActive)
        {
            _ = _vm.StopListeningAsync();
        }
    }

    private void SendText_Click(object sender, RoutedEventArgs e)
    {
        SendCurrentText();
    }

    private void TxtInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SendCurrentText();
            e.Handled = true;
        }
    }

    private void SendCurrentText()
    {
        var text = TxtInput.Text?.Trim();
        if (!string.IsNullOrEmpty(text))
        {
            TxtInput.Clear();
            _ = _vm.SendTextMessageAsync(text);
        }
    }

    private void AbortBtn_Click(object sender, MouseButtonEventArgs e)
    {
        _ = _vm.AbortAsync();
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        var win = new SettingsWindow();
        win.Owner = this;
        if (win.ShowDialog() == true)
        {
            _ = _vm.ReconnectAsync();
        }
    }
}
