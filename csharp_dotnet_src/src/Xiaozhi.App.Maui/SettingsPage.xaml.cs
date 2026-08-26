using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Xiaozhi.Protocols.Ota;

namespace Xiaozhi.App.Maui;

public partial class SettingsPage : ContentPage
{
    public event EventHandler? SettingsSaved;

    private readonly DeviceActivationService _activationService = new();
    private CancellationTokenSource? _cts;
    private string? _activeWebUrl;

    public SettingsPage()
    {
        InitializeComponent();

        WsUrlEntry.Text = Preferences.Default.Get("lily_ws_url", "wss://api.tenclass.net/xiaozhi/v1/");
        TokenEntry.Text = Preferences.Default.Get("lily_token", "test-token");
        DeviceIdEntry.Text = Preferences.Default.Get("lily_device_id", "a0:36:bc:2c:ed:40");
        ClientIdEntry.Text = Preferences.Default.Get("lily_client_id", "maui-ios-client");

        Unloaded += (s, e) => _cts?.Cancel();
    }

    private async void OnGetOtpClicked(object sender, EventArgs e)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        var deviceId = ClientIdEntry.Text?.Trim() ?? "maui-ios-client";
        var macAddress = DeviceIdEntry.Text?.Trim() ?? "a0:36:bc:2c:ed:40";

        OtpStatusLabel.Text = "⏳ Đang kết nối OTA Server lấy mã OTP...";
        OtpCodeLabel.Text = "******";

        var result = await _activationService.CheckOrRequestActivationAsync(deviceId, macAddress);

        if (result.IsActivated && !string.IsNullOrEmpty(result.Token))
        {
            TokenEntry.Text = result.Token;
            OtpCodeLabel.Text = "ACTIVE";
            OtpStatusLabel.Text = "🎉 Thiết bị đã được kích hoạt thành công!";
            Preferences.Default.Set("lily_token", result.Token);
            return;
        }

        var code = result.Code ?? new Random().Next(100000, 999999).ToString();
        _activeWebUrl = result.QrUrl ?? $"https://xiaozhi.me/active?code={code}";

        OtpCodeLabel.Text = code;
        OtpStatusLabel.Text = "👉 Hãy nhập mã 6 số này trên trang xiaozhi.me hoặc bấm nút mở Web để kích hoạt!";
        OpenActiveWebBtn.IsEnabled = true;

        // Auto Poll for Token in background matching WPF app
        _ = Task.Run(async () =>
        {
            var token = await _activationService.PollForTokenAsync(deviceId, macAddress, maxRetries: 60, intervalSeconds: 3, _cts.Token);
            if (!string.IsNullOrEmpty(token))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TokenEntry.Text = token;
                    OtpCodeLabel.Text = "SUCCESS";
                    OtpStatusLabel.Text = "🎉 Kích hoạt thành công! Token mới đã được tự động lưu.";
                    Preferences.Default.Set("lily_token", token);
                });
            }
        }, _cts.Token);
    }

    private async void OnOpenActiveWebClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_activeWebUrl))
        {
            await Launcher.Default.OpenAsync(new Uri(_activeWebUrl));
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var wsUrl = WsUrlEntry.Text?.Trim();
        var token = TokenEntry.Text?.Trim();
        var deviceId = DeviceIdEntry.Text?.Trim();
        var clientId = ClientIdEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(wsUrl))
        {
            await DisplayAlert("Lỗi", "Vui lòng nhập Server WebSocket URL.", "OK");
            return;
        }

        Preferences.Default.Set("lily_ws_url", wsUrl);
        Preferences.Default.Set("lily_token", token ?? "");
        Preferences.Default.Set("lily_device_id", deviceId ?? "");
        Preferences.Default.Set("lily_client_id", clientId ?? "");

        SettingsSaved?.Invoke(this, EventArgs.Empty);
        await Navigation.PopModalAsync();
    }

    private async void OnResetClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Xác nhận", "Khôi phục cấu hình server về mặc định?", "Đồng ý", "Hủy");
        if (confirm)
        {
            WsUrlEntry.Text = "wss://api.tenclass.net/xiaozhi/v1/";
            TokenEntry.Text = "test-token";
            DeviceIdEntry.Text = "a0:36:bc:2c:ed:40";
            ClientIdEntry.Text = "maui-ios-client";

            Preferences.Default.Remove("lily_ws_url");
            Preferences.Default.Remove("lily_token");
            Preferences.Default.Remove("lily_device_id");
            Preferences.Default.Remove("lily_client_id");
        }
    }
}
