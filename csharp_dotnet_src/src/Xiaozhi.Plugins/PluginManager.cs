using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xiaozhi.Plugins;

public interface IPlugin
{
    // Tên dùng để nhận diện plugin khi log/hiển thị.
    string Name { get; }
    // Chuẩn bị tài nguyên của plugin.
    Task InitializeAsync();
    // Nhả tài nguyên trước khi ứng dụng đóng.
    Task ShutdownAsync();
}

public class PluginManager
{
    private readonly List<IPlugin> _plugins = new();

    /// <summary>Thêm plugin vào danh sách sẽ được quản lý theo vòng đời chung.</summary>
    public void RegisterPlugin(IPlugin plugin)
    {
        _plugins.Add(plugin);
    }

    /// <summary>Khởi tạo tuần tự mọi plugin đã đăng ký.</summary>
    public async Task InitializeAllAsync()
    {
        foreach (var plugin in _plugins)
        {
            await plugin.InitializeAsync();
        }
    }

    /// <summary>Tắt tuần tự mọi plugin để nhả hotkey/hook/tài nguyên.</summary>
    public async Task ShutdownAllAsync()
    {
        foreach (var plugin in _plugins)
        {
            await plugin.ShutdownAsync();
        }
    }
}
