using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Xiaozhi.Core.Utils;
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

        var config = ConfigManager.Instance.Config;
        WsUrlEntry.Text = Preferences.Default.Get("lily_ws_url", config.SystemOptions.Network.WebSocketUrl);
        TokenEntry.Text = Preferences.Default.Get("lily_token", config.SystemOptions.Network.WebSocketAccessToken);
        DeviceIdEntry.Text = Preferences.Default.Get("lily_device_id", config.SystemOptions.DeviceId);
        ClientIdEntry.Text = Preferences.Default.Get("lily_client_id", config.SystemOptions.ClientId);

        Unloaded += (s, e) => _cts?.Cancel();
    }

    private async void OnGetOtpClicked(object sender, EventArgs e)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        var deviceId = ClientIdEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(deviceId) || deviceId == "maui-ios-client" || !Guid.TryParse(deviceId, out _))
        {
            deviceId = Guid.NewGuid().ToString();
            ClientIdEntry.Text = deviceId;
        }

        var macAddress = DeviceIdEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(macAddress) || macAddress == "a0:36:bc:2c:ed:40" || macAddress == "00:00:00:00:00:00")
        {
            macAddress = GetOrCreateUniqueMacAddress();
            DeviceIdEntry.Text = macAddress;
        }

        SerialNoLabel.Text = macAddress;
        OtpStatusLabel.Text = "⏳ Đang kết nối OTA Server lấy mã OTP...";
        OtpCodeLabel.Text = "******";

        var result = await _activationService.CheckOrRequestActivationAsync(deviceId, macAddress);

        if (result.IsActivated && !string.IsNullOrEmpty(result.Token))
        {
            TokenEntry.Text = result.Token;
            OtpCodeLabel.Text = "ACTIVE";
            OtpStatusLabel.Text = "🎉 Thiết bị đã được kích hoạt thành công!";
            SaveConfigToStorage(WsUrlEntry.Text, result.Token, macAddress, deviceId);
            return;
        }

        if (!string.IsNullOrEmpty(result.Code))
        {
            var code = result.Code;
            _activeWebUrl = result.QrUrl ?? $"https://xiaozhi.me/active?code={code}";

            OtpCodeLabel.Text = code;
            OtpStatusLabel.Text = $"👉 Nhập Mã xác minh: {code} và Số Serial: {macAddress} trên trang xiaozhi.me:";
            OpenActiveWebBtn.IsEnabled = true;
        }
        else
        {
            OtpCodeLabel.Text = "NO CODE";
            OtpStatusLabel.Text = !string.IsNullOrEmpty(result.Message)
                ? $"⚠️ {result.Message}"
                : "⚠️ Server chưa cấp mã OTP cho MAC này.";
            OpenActiveWebBtn.IsEnabled = false;
            return;
        }

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
                    SaveConfigToStorage(WsUrlEntry.Text, token, macAddress, deviceId);
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

    private void OnAutoGenerateTokenClicked(object sender, EventArgs e)
    {
        var randomToken = $"auto-token-{Guid.NewGuid().ToString("N")[..12]}";
        TokenEntry.Text = randomToken;
        OtpCodeLabel.Text = "READY";
        OtpStatusLabel.Text = "🎉 Đã tự tạo Token thành công! Bạn có thể bấm Lưu & Kết nối ngay.";
        SaveConfigToStorage(WsUrlEntry.Text, randomToken, DeviceIdEntry.Text, ClientIdEntry.Text);
    }

    private async void OnCopyOtpClicked(object sender, EventArgs e)
    {
        var code = OtpCodeLabel.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(code) && code != "------" && code != "******" && code != "NO CODE")
        {
            await Clipboard.SetTextAsync(code);
            await DisplayAlert("Thông báo", $"Đã sao chép Mã OTP: {code}", "OK");
        }
        else
        {
            await DisplayAlert("Thông báo", "Vui lòng bấm 'Tạo mã OTP' trước khi sao chép.", "OK");
        }
    }

    private async void OnCopySerialClicked(object sender, EventArgs e)
    {
        var serial = SerialNoLabel.Text?.Trim();
        if (string.IsNullOrWhiteSpace(serial) || serial == "------")
        {
            serial = DeviceIdEntry.Text?.Trim() ?? GetOrCreateUniqueMacAddress();
        }

        if (!string.IsNullOrWhiteSpace(serial))
        {
            await Clipboard.SetTextAsync(serial);
            await DisplayAlert("Thông báo", $"Đã sao chép Số Serial (MAC): {serial}\nHãy dán vào ô 'Số Serial' trên xiaozhi.me!", "OK");
        }
    }

    private void OnRandomMacClicked(object sender, EventArgs e)
    {
        var newMac = GenerateRandomMacAddress();
        DeviceIdEntry.Text = newMac;
        Preferences.Default.Set("lily_device_id", newMac);
        OtpCodeLabel.Text = "******";
        OtpStatusLabel.Text = $"🎲 Đã tạo MAC mới ({newMac}). Hãy bấm nút 'Tạo mã OTP' bên trên để ghép nối!";
    }

    public static string GetOrCreateUniqueMacAddress()
    {
        var savedMac = Preferences.Default.Get("lily_device_id", "");
        if (!string.IsNullOrWhiteSpace(savedMac) && savedMac != "a0:36:bc:2c:ed:40" && savedMac != "00:00:00:00:00:00")
        {
            return savedMac;
        }

        var newMac = GenerateRandomMacAddress();
        Preferences.Default.Set("lily_device_id", newMac);
        return newMac;
    }

    private static string GenerateRandomMacAddress()
    {
        var random = new Random();
        byte[] bytes = new byte[6];
        random.NextBytes(bytes);
        bytes[0] = (byte)((bytes[0] & 0xFE) | 0x02);
        return string.Join(":", bytes.Select(b => b.ToString("x2")));
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

        SaveConfigToStorage(wsUrl, token ?? "", deviceId ?? "", clientId ?? "");

        SettingsSaved?.Invoke(this, EventArgs.Empty);
        await Navigation.PopModalAsync();
    }

    private void SaveConfigToStorage(string wsUrl, string token, string deviceId, string clientId)
    {
        Preferences.Default.Set("lily_ws_url", wsUrl);
        Preferences.Default.Set("lily_token", token);
        Preferences.Default.Set("lily_device_id", deviceId);
        Preferences.Default.Set("lily_client_id", clientId);

        var config = ConfigManager.Instance.Config;
        config.SystemOptions.Network.WebSocketUrl = wsUrl;
        config.SystemOptions.Network.WebSocketAccessToken = token;
        config.SystemOptions.DeviceId = deviceId;
        config.SystemOptions.ClientId = clientId;
        ConfigManager.Instance.SaveConfig(config);
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

            SaveConfigToStorage(WsUrlEntry.Text, TokenEntry.Text, DeviceIdEntry.Text, ClientIdEntry.Text);
        }
    }
}
