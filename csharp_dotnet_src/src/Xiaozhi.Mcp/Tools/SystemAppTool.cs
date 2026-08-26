using System;
using System.Diagnostics;
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

    public bool LaunchApp(string appPath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = appPath,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool KillApp(string processName)
    {
        try
        {
            var procs = Process.GetProcessesByName(processName);
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
}
