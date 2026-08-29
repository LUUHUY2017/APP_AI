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

// Cửa sổ thực hiện quy trình nhận token thiết bị qua OTA và trang xiaozhi.me.
public partial class ActivationWindow : Window
{
    // Service đóng gói các HTTP request kiểm tra, yêu cầu và polling trạng thái kích hoạt.
    private readonly DeviceActivationService _activationService;

    // Token hủy giúp dừng vòng polling khi người dùng đóng cửa sổ.
    private CancellationTokenSource? _cts;

    // Cửa sổ Settings đọc property này sau khi ShowDialog trả về true.
    public string? ActivatedToken { get; private set; }

    public ActivationWindow()
    {
        // Tạo control từ ActivationWindow.xaml.
        InitializeComponent();

        // Mỗi cửa sổ sử dụng một service activation riêng.
        _activationService = new DeviceActivationService();

        // Chỉ bắt đầu gọi mạng sau Loaded để control đã sẵn sàng nhận trạng thái/QR.
        Loaded += async (s, e) => await StartActivationFlowAsync();

        // Khi cửa sổ đóng vì bất kỳ lý do gì, yêu cầu task polling kết thúc.
        Closed += (s, e) => _cts?.Cancel();
    }

    private async Task StartActivationFlowAsync()
    {
        // Tạo nguồn hủy mới cho toàn bộ phiên activation hiện tại.
        _cts = new CancellationTokenSource();

        // Lấy danh tính thiết bị dùng làm tham số OTA.
        var config = ConfigManager.Instance.Config;
        var mac = config.SystemOptions.DeviceId;
        var deviceId = config.SystemOptions.ClientId;

        // Phản hồi ngay cho người dùng trước khi bắt đầu HTTP request.
        TxtStatus.Text = "Đang kết nối OTA Server...";

        // Kiểm tra thiết bị đã có token chưa; nếu chưa, server có thể trả code và QR URL.
        var result = await _activationService.CheckOrRequestActivationAsync(deviceId, mac);

        // Nhánh nhanh: thiết bị từng kích hoạt, không cần hiển thị QR hoặc polling.
        if (result.IsActivated && !string.IsNullOrEmpty(result.Token))
        {
            // Công bố token cho SettingsWindow.
            ActivatedToken = result.Token;

            // Cập nhật thông báo và mã hiển thị.
            TxtStatus.Text = "Thiết bị đã được kích hoạt từ trước!";
            TxtActivationCode.Text = "ACTIVE";

            // Cho người dùng đủ thời gian đọc trạng thái trước khi tự đóng.
            await Task.Delay(1500);

            // true biểu thị activation thành công.
            DialogResult = true;
            Close();
            return;
        }

        // Nếu server không trả code, tạo mã sáu chữ số để vẫn có dữ liệu hiển thị.
        var code = result.Code ?? new Random().Next(100000, 999999).ToString();

        // Nếu thiếu URL, dựng đường dẫn kích hoạt mặc định từ code.
        var url = result.QrUrl ?? $"https://xiaozhi.me/active?code={code}";

        // Hiển thị code, hướng dẫn và ảnh QR cho người dùng quét.
        TxtActivationCode.Text = code;
        TxtStatus.Text = "Quét mã QR bằng điện thoại để kích hoạt trên xiaozhi.me";
        RenderQrCode(url);

        // Polling chạy nền để không khóa giao diện trong tối đa 60 lần x 3 giây.
        _ = Task.Run(async () =>
        {
            // Hỏi server định kỳ cho đến khi có token, hết số lần thử hoặc bị hủy.
            var token = await _activationService.PollForTokenAsync(deviceId, mac, maxRetries: 60, intervalSeconds: 3, _cts.Token);

            // Chỉ hoàn tất dialog khi nhận được token hợp lệ.
            if (!string.IsNullOrEmpty(token))
            {
                // Task.Run đang ở worker thread; mọi thay đổi control phải quay về UI Dispatcher.
                Dispatcher.Invoke(() =>
                {
                    // Trả token cho nơi mở dialog và thông báo thành công.
                    ActivatedToken = token;
                    TxtStatus.Text = "🎉 Kích hoạt thành công!";

                    // Đồng thời lưu token vào config dùng chung và file AppData.
                    var cfg = ConfigManager.Instance.Config;
                    cfg.SystemOptions.Network.WebSocketAccessToken = token;
                    ConfigManager.Instance.SaveConfig(cfg);

                    // Đóng dialog với kết quả thành công.
                    DialogResult = true;
                    Close();
                });
            }
        // Truyền token cho Task.Run để task có thể chuyển sang trạng thái canceled khi cửa sổ đóng.
        }, _cts.Token);
    }

    private void RenderQrCode(string data)
    {
        try
        {
            // QRCodeGenerator phân tích URL thành ma trận QR; using bảo đảm nhả tài nguyên.
            using var qrGenerator = new QRCodeGenerator();

            // ECCLevel.Q cho phép QR chịu hỏng khoảng 25% nhưng vẫn đọc được.
            using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);

            // Chọn renderer PNG dạng byte để chuyển tiếp sang BitmapImage của WPF.
            using var qrCode = new PngByteQRCode(qrCodeData);

            // Mỗi module QR được vẽ 20 pixel để ảnh đủ rõ khi hiển thị.
            var qrCodeBytes = qrCode.GetGraphic(20);

            // BitmapImage là kiểu ảnh mà control Image của WPF nhận trực tiếp.
            var image = new BitmapImage();

            // MemoryStream giúp nạp byte PNG mà không cần tạo file tạm trên đĩa.
            using (var mem = new MemoryStream(qrCodeBytes))
            {
                // Đặt con trỏ về đầu stream trước khi decoder đọc.
                mem.Position = 0;

                // BeginInit/EndInit bao quanh việc thiết lập các thuộc tính khởi tạo BitmapImage.
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;

                // OnLoad buộc đọc hết byte ngay để stream có thể Dispose sau khối using.
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = mem;
                image.EndInit();
            }

            // Freeze làm ảnh bất biến, an toàn hơn khi WPF truy cập qua nhiều ngữ cảnh thread.
            image.Freeze();

            // Gán ảnh đã giải mã vào control QR trên cửa sổ.
            ImgQrCode.Source = image;
        }
        // Lỗi tạo QR hiện bị bỏ qua để cửa sổ không crash; production nên ghi log tại đây.
        catch { }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        // Yêu cầu vòng polling nền dừng càng sớm càng tốt.
        _cts?.Cancel();

        // false biểu thị người dùng đóng/hủy trước khi có token.
        DialogResult = false;

        // Đóng cửa sổ activation.
        Close();
    }
}
