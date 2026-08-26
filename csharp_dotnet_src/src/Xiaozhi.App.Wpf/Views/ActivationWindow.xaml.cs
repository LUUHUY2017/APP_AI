using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using QRCoder;
using Xiaozhi.Core.Utils;
using Xiaozhi.Protocols.Ota;

namespace Xiaozhi.App.Wpf.Views;

public partial class ActivationWindow : Window
{
    private readonly DeviceActivationService _activationService;
    private CancellationTokenSource? _cts;
    public string? ActivatedToken { get; private set; }

    public ActivationWindow()
    {
        InitializeComponent();
        _activationService = new DeviceActivationService();
        Loaded += async (s, e) => await StartActivationFlowAsync();
        Closed += (s, e) => _cts?.Cancel();
    }

    private async Task StartActivationFlowAsync()
    {
        _cts = new CancellationTokenSource();
        var config = ConfigManager.Instance.Config;
        var mac = config.SystemOptions.DeviceId;
        var deviceId = config.SystemOptions.ClientId;

        TxtStatus.Text = "Đang kết nối OTA Server...";

        var result = await _activationService.CheckOrRequestActivationAsync(deviceId, mac);

        if (result.IsActivated && !string.IsNullOrEmpty(result.Token))
        {
            ActivatedToken = result.Token;
            TxtStatus.Text = "Thiết bị đã được kích hoạt từ trước!";
            TxtActivationCode.Text = "ACTIVE";
            await Task.Delay(1500);
            DialogResult = true;
            Close();
            return;
        }

        var code = result.Code ?? new Random().Next(100000, 999999).ToString();
        var url = result.QrUrl ?? $"https://xiaozhi.me/active?code={code}";

        TxtActivationCode.Text = code;
        TxtStatus.Text = "Quét mã QR bằng điện thoại để kích hoạt trên xiaozhi.me";
        RenderQrCode(url);

        _ = Task.Run(async () =>
        {
            var token = await _activationService.PollForTokenAsync(deviceId, mac, maxRetries: 60, intervalSeconds: 3, _cts.Token);
            if (!string.IsNullOrEmpty(token))
            {
                Dispatcher.Invoke(() =>
                {
                    ActivatedToken = token;
                    TxtStatus.Text = "🎉 Kích hoạt thành công!";
                    var cfg = ConfigManager.Instance.Config;
                    cfg.SystemOptions.Network.WebSocketAccessToken = token;
                    ConfigManager.Instance.SaveConfig(cfg);
                    DialogResult = true;
                    Close();
                });
            }
        }, _cts.Token);
    }

    private void RenderQrCode(string data)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);
            var image = new BitmapImage();
            using (var mem = new MemoryStream(qrCodeBytes))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            ImgQrCode.Source = image;
        }
        catch { }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        DialogResult = false;
        Close();
    }
}
