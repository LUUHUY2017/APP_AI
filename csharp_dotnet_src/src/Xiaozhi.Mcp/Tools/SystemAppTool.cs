using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Xiaozhi.Mcp.Tools;

public class SystemAppTool
{
    public string[] ListRunningApps()
    {
        return Process.GetProcesses()
            .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
            .Select(p => $"{p.ProcessName} ({p.MainWindowTitle})")
            .Take(30)
            .ToArray();
    }

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
