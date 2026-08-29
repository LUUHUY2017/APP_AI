using System.Configuration;
using System.Data;
using System.Windows;

namespace Xiaozhi.App.Wpf;

/// <summary>
/// Đối tượng ứng dụng WPF cấp cao nhất. App.xaml chỉ định MainWindow.xaml là StartupUri,
/// nên WPF tự tạo và hiển thị MainWindow khi tiến trình .exe khởi động.
/// Lớp hiện không override startup/shutdown; toàn bộ khởi tạo nghiệp vụ bắt đầu ở MainWindow.Loaded.
/// </summary>
public partial class App : Application
{
}

