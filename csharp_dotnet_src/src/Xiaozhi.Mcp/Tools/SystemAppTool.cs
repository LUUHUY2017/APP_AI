using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Xiaozhi.Mcp.Tools;

public class SystemAppTool
{
    /// <summary>Liệt kê tối đa 30 process có cửa sổ hiển thị cho người dùng.</summary>
    public string[] ListRunningApps()
    {
        return Process.GetProcesses()
            .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
            .Select(p => $"{p.ProcessName} ({p.MainWindowTitle})")
            .Take(30)
            .ToArray();
    }

    /// <summary>Quét shortcut Start Menu/Desktop để suy ra danh sách ứng dụng đã cài.</summary>
    public List<string> FindInstalledApps()
    {
        var appNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string[] searchFolders = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };

        foreach (var folder in searchFolders)
        {
            if (!Directory.Exists(folder)) continue;
            try
            {
                var files = Directory.GetFiles(folder, "*.lnk", SearchOption.AllDirectories);
                foreach (var f in files)
                {
                    string name = Path.GetFileNameWithoutExtension(f);
                    if (!string.IsNullOrWhiteSpace(name) && !name.StartsWith("Uninstall", StringComparison.OrdinalIgnoreCase))
                    {
                        appNames.Add(name);
                    }
                }
            }
            catch { }
        }

        return appNames.Take(50).ToList();
    }

    /// <summary>Mở đường dẫn/tên ứng dụng bằng Windows shell; trả false nếu cả hai cách đều lỗi.</summary>
    public bool LaunchApp(string appPathOrName)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = appPathOrName,
                UseShellExecute = true
            };
            Process.Start(psi);
            return true;
        }
        catch
        {
            // Thử mở qua cmd /c start
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start \"\" \"{appPathOrName}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>Tìm tất cả process trùng tên (có thể kèm .exe) và yêu cầu kết thúc chúng.</summary>
    public bool KillApp(string processName)
    {
        try
        {
            string cleanName = processName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
            var procs = Process.GetProcessesByName(cleanName);
            foreach (var p in procs)
            {
                p.Kill();
            }
            return procs.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Tổng hợp phiên bản Windows, bộ nhớ managed của app và dung lượng các ổ sẵn sàng.</summary>
    public string GetSystemStatus()
    {
        try
        {
            var gcMemoryMb = GC.GetTotalMemory(false) / (1024 * 1024);
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d => $"{d.Name} {d.AvailableFreeSpace / (1024 * 1024 * 1024)}GB trống / {d.TotalSize / (1024 * 1024 * 1024)}GB");

            return $"OS: Windows {Environment.OSVersion.Version} | RAM App: {gcMemoryMb}MB | Ổ đĩa: {string.Join(", ", drives)}";
        }
        catch
        {
            return "Không thể lấy thông tin hệ thống.";
        }
    }
}
