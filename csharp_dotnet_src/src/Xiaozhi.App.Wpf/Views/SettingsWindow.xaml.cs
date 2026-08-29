using System.Windows;
using Xiaozhi.Core.Utils;

namespace Xiaozhi.App.Wpf.Views;

// Cửa sổ modal cho phép người dùng xem/sửa cấu hình kết nối và từ đánh thức.
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        // Khởi tạo các control đã khai báo trong SettingsWindow.xaml.
        InitializeComponent();

        // Điền cấu hình hiện tại vào control ngay khi cửa sổ được tạo.
        LoadSettings();
    }

    private void LoadSettings()
    {
        // ConfigManager là singleton, do đó đây là cùng đối tượng cấu hình MainViewModel sử dụng.
        var config = ConfigManager.Instance.Config;

        // Hiển thị endpoint WebSocket hiện tại vào ô nhập server.
        TxtServerUrl.Text = config.SystemOptions.Network.WebSocketUrl;

        // Hiển thị access token; giá trị này sẽ được dùng khi tạo WebSocket client.
        TxtToken.Text = config.SystemOptions.Network.WebSocketAccessToken;

        // UI hiện chỉ hiển thị từ mặc định; giá trị này chưa được lưu ngược trong Save_Click.
        TxtWakeWord.Text = "xiaozhi";

        // Đồng bộ checkbox với cờ bật/tắt wake word trong cấu hình.
        ChkWakeWord.IsChecked = config.WakeWordOptions.UseWakeWord;
    }

    private void GetTokenQr_Click(object sender, RoutedEventArgs e)
    {
        // Tạo cửa sổ kích hoạt để lấy token bằng mã QR.
        var actWindow = new ActivationWindow();

        // Gán Owner để cửa sổ kích hoạt nằm trên Settings và đóng theo đúng quan hệ modal.
        actWindow.Owner = this;

        // ShowDialog chặn riêng cửa sổ Settings cho tới khi activation đóng.
        // Chỉ chép token khi activation trả về thành công và token không rỗng.
        if (actWindow.ShowDialog() == true && !string.IsNullOrEmpty(actWindow.ActivatedToken))
        {
            // Đưa token vào ô nhập; token chỉ được ghi xuống file khi người dùng bấm Lưu.
            TxtToken.Text = actWindow.ActivatedToken;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Lấy đối tượng cấu hình đang sống trong bộ nhớ.
        var config = ConfigManager.Instance.Config;

        // Chép các giá trị người dùng nhập từ UI trở lại model.
        config.SystemOptions.Network.WebSocketUrl = TxtServerUrl.Text;
        config.SystemOptions.Network.WebSocketAccessToken = TxtToken.Text;

        // Checkbox WPF có ba trạng thái nên dùng ?? true khi giá trị là null.
        config.WakeWordOptions.UseWakeWord = ChkWakeWord.IsChecked ?? true;

        // Serialize model thành JSON tại %APPDATA%/XiaozhiLily/config.json.
        ConfigManager.Instance.SaveConfig(config);

        // true báo cho MainWindow rằng cấu hình đã đổi và cần ReconnectAsync.
        DialogResult = true;

        // Đóng hộp thoại sau khi lưu thành công.
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        // false báo cho MainWindow không kết nối lại vì người dùng đã hủy.
        DialogResult = false;

        // Đóng mà không gọi SaveConfig.
        Close();
    }
}
