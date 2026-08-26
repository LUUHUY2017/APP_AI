using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Xiaozhi.App.Maui;

public partial class SettingsPage : ContentPage
{
    public event EventHandler? SettingsSaved;

    public SettingsPage()
    {
        InitializeComponent();

        WsUrlEntry.Text = Preferences.Default.Get("lily_ws_url", "wss://api.tenclass.net/xiaozhi/v1/");
        TokenEntry.Text = Preferences.Default.Get("lily_token", "test-token");
        DeviceIdEntry.Text = Preferences.Default.Get("lily_device_id", "a0:36:bc:2c:ed:40");
        ClientIdEntry.Text = Preferences.Default.Get("lily_client_id", "maui-ios-client");
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
