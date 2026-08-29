using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Xiaozhi.Core.Models;
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

        var savedMac = Preferences.Default.Get("lily_device_id", config.SystemOptions.DeviceId);
        if (string.IsNullOrWhiteSpace(savedMac))
        {
            savedMac = "38:60:77:dc:90:11";
            Preferences.Default.Set("lily_device_id", savedMac);
        }
        DeviceIdEntry.Text = savedMac;

        var savedClientId = Preferences.Default.Get("lily_client_id", config.SystemOptions.ClientId);
        if (string.IsNullOrWhiteSpace(savedClientId) || savedClientId == "maui-ios-client" || savedClientId == "21ebee2f-926c-4703-9010-b488f5939580" || savedClientId == "d7377f0a-2682-4e4f-a125-e0a78c730cf8")
        {
            savedClientId = "b7907b41-1534-422b-a9ce-26b227286d8e";
            Preferences.Default.Set("lily_client_id", savedClientId);
        }
        ClientIdEntry.Text = savedClientId;

        SerialNoLabel.Text = DeviceFingerprint.GenerateSerialNumber(savedMac);

        _ = LoadEfuseJsonAssetAsync();

        Unloaded += (s, e) => _cts?.Cancel();
    }

    private async Task LoadEfuseJsonAssetAsync()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("efuse.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("mac_address", out var macProp) && !string.IsNullOrWhiteSpace(macProp.GetString()))
            {
                var mac = macProp.GetString()!;
                DeviceIdEntry.Text = mac;
                Preferences.Default.Set("lily_device_id", mac);
                SerialNoLabel.Text = DeviceFingerprint.GenerateSerialNumber(mac);
            }

            if (root.TryGetProperty("device_fingerprint", out var fpElem))
            {
                if (fpElem.TryGetProperty("machine_id", out var idProp) && !string.IsNullOrWhiteSpace(idProp.GetString()))
                {
                    var clientId = idProp.GetString()!;
                    ClientIdEntry.Text = clientId;
                    Preferences.Default.Set("lily_client_id", clientId);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load efuse.json asset: {ex.Message}");
        }
    }

    private async void OnGetOtpClicked(object sender, EventArgs e)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        var deviceId = ClientIdEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(deviceId) || deviceId == "maui-ios-client" || !Guid.TryParse(deviceId, out _))
        {
            deviceId = "b7907b41-1534-422b-a9ce-26b227286d8e";
            ClientIdEntry.Text = deviceId;
        }

        var macAddress = SanitizeMacAddress(DeviceIdEntry.Text?.Trim());
        if (string.IsNullOrWhiteSpace(macAddress) || macAddress == "a0:36:bc:2c:ed:40" || macAddress == "00:00:00:00:00:00")
        {
            macAddress = GetOrCreateUniqueMacAddress();
        }
        DeviceIdEntry.Text = macAddress;

        // Server đăng ký "serial_number" là MAC đã bỏ dấu ":" (xem DeviceActivationService.cs),
        // nên phải hiển thị/copy đúng định dạng này để nhập trên xiaozhi.me, tránh lỗi "Serial number required/invalid".
        var serialNumber = DeviceFingerprint.GenerateSerialNumber(macAddress);
        SerialNoLabel.Text = serialNumber;
        OtpStatusLabel.Text = "⏳ Đang kết nối OTA Server lấy mã OTP...";
        OtpCodeLabel.Text = "******";

        var result = await _activationService.CheckOrRequestActivationAsync(deviceId, macAddress);

        OtaLogEditor.Text = $"📤 GÓI TIN GỬI ĐI (REQUEST):\n{result.RawRequest}\n\n📥 PHẢN HỒI NGUYÊN VĂN TỪ SERVER (RESPONSE):\n{result.RawResponse}";

        if (!string.IsNullOrEmpty(result.Token))
        {
            TokenEntry.Text = result.Token;
            if (!string.IsNullOrEmpty(result.WebSocketUrl)) WsUrlEntry.Text = result.WebSocketUrl;
            SaveConfigToStorage(WsUrlEntry.Text, result.Token, macAddress, deviceId);
        }

        if (result.IsActivated && !string.IsNullOrEmpty(result.Token))
        {
            OtpCodeLabel.Text = "ACTIVE";
            OtpStatusLabel.Text = "🎉 Thiết bị đã được kích hoạt thành công!";
            return;
        }

        if (!string.IsNullOrEmpty(result.Code))
        {
            var code = result.Code;
            _activeWebUrl = result.QrUrl ?? $"https://xiaozhi.me/active?code={code}";

            OtpCodeLabel.Text = code;
            OtpStatusLabel.Text = $"👉 Chỉ nhập mã OTP {code} trên xiaozhi.me; không nhập Serial. Ứng dụng sẽ tự nhận Token sau khi xác minh.";
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
            var rawMac = DeviceIdEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(rawMac))
            {
                rawMac = GetOrCreateUniqueMacAddress();
            }
            // Luôn chuẩn hoá về dạng không dấu ":" để khớp với serial_number đã đăng ký với OTA server.
            serial = DeviceFingerprint.GenerateSerialNumber(rawMac);
        }

        if (!string.IsNullOrWhiteSpace(serial))
        {
            await Clipboard.SetTextAsync(serial);
            await DisplayAlert("Thông báo", $"Đã sao chép Số Serial: {serial}\nHãy dán vào ô 'Số Serial' trên xiaozhi.me!", "OK");
        }
    }

    private async void OnPasteTokenClicked(object sender, EventArgs e)
    {
        if (Clipboard.HasText)
        {
            var pasted = await Clipboard.GetTextAsync();
            if (!string.IsNullOrWhiteSpace(pasted))
            {
                TokenEntry.Text = pasted.Trim();
                await DisplayAlert("Thông báo", "Đã dán Token từ bộ nhớ tạm!", "OK");
            }
        }
        else
        {
            await DisplayAlert("Thông báo", "Bộ nhớ tạm (Clipboard) đang trống.", "OK");
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

    public static string SanitizeMacAddress(string? macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress)) return "";
        var firstPart = macAddress.Split(new[] { ' ', ',', ';', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var rawMac = new string(firstPart.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        if (rawMac.Length >= 12)
        {
            rawMac = rawMac.Substring(0, 12);
            return string.Join(":", Enumerable.Range(0, 6).Select(i => rawMac.Substring(i * 2, 2)));
        }
        return "";
    }

    public static string GetOrCreateUniqueMacAddress()
    {
        var savedMac = SanitizeMacAddress(Preferences.Default.Get("lily_device_id", ""));
        if (!string.IsNullOrWhiteSpace(savedMac) && savedMac != "a0:36:bc:2c:ed:40" && savedMac != "00:00:00:00:00:00")
        {
            return savedMac;
        }

        var defaultMac = "38:60:77:dc:90:11";
        Preferences.Default.Set("lily_device_id", defaultMac);
        return defaultMac;
    }

    private static string GenerateRandomMacAddress()
    {
        var random = new Random();
        byte[] bytes = new byte[6];
        random.NextBytes(bytes);
        bytes[0] = (byte)((bytes[0] & 0xFE) | 0x02);
        return string.Join(":", bytes.Select(b => b.ToString("x2")));
    }

    private async void OnCheckAppUpdateClicked(object sender, EventArgs e)
    {
        var service = new Services.OtaAutoUpdateService();
        await service.CheckForUpdatesAsync(this, silentIfLatest: false);
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        _cts?.Cancel();
        await Navigation.PopModalAsync();
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

        var efuse = ConfigManager.Instance.Efuse ?? new EfuseConfig();
        efuse.MacAddress = deviceId;
        efuse.SerialNumber = DeviceFingerprint.GenerateSerialNumber(deviceId);
        efuse.HmacKey = DeviceFingerprint.GenerateHmacKey(deviceId);
        efuse.ActivationStatus = true;
        ConfigManager.Instance.SaveEfuse(efuse);
    }

    private async void OnResetClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Xác nhận", "Khôi phục cấu hình server về mặc định?", "Đồng ý", "Hủy");
        if (confirm)
        {
            WsUrlEntry.Text = "wss://api.tenclass.net/xiaozhi/v1/";
            TokenEntry.Text = "test-token";
            DeviceIdEntry.Text = "38:60:77:dc:90:11";
            ClientIdEntry.Text = "b7907b41-1534-422b-a9ce-26b227286d8e";

            Preferences.Default.Remove("lily_ws_url");
            Preferences.Default.Remove("lily_token");
            Preferences.Default.Remove("lily_device_id");
            Preferences.Default.Remove("lily_client_id");

            SaveConfigToStorage(WsUrlEntry.Text, TokenEntry.Text, DeviceIdEntry.Text, ClientIdEntry.Text);
        }
    }
}
