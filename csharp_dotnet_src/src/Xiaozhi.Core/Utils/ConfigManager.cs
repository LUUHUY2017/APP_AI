using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.Json;
using Xiaozhi.Core.Models;

namespace Xiaozhi.Core.Utils;

public class ConfigManager
{
    private static ConfigManager? _instance;
    public static ConfigManager Instance => _instance ??= new ConfigManager();

    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "XiaozhiLily");
    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    public AppConfig Config { get; private set; }

    private ConfigManager()
    {
        Config = LoadOrCreate();
    }

    private AppConfig LoadOrCreate()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? CreateDefault();
            }
        }
        catch { }

        var config = CreateDefault();
        SaveConfig(config);
        return config;
    }

    private AppConfig CreateDefault()
    {
        return new AppConfig
        {
            SystemOptions = new SystemOptions
            {
                ClientId = "a927bd19-f917-4a3a-9f5a-4e453603c9b4",
                DeviceId = "cc:30:80:20:64:7c",
                Network = new NetworkOptions
                {
                    OtaVersionUrl = "https://api.tenclass.net/xiaozhi/ota/",
                    WebSocketUrl = "wss://api.tenclass.net/xiaozhi/v1/",
                    WebSocketAccessToken = "test-token",
                    AuthorizationUrl = "https://xiaozhi.me/"
                }
            },
            WakeWordOptions = new WakeWordOptions
            {
                UseWakeWord = true,
                ModelPath = "models",
                NumThreads = 4,
                KeywordsScore = 1.8f,
                KeywordsThreshold = 0.2f
            }
        };
    }

    public void SaveConfig(AppConfig config)
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, options));
            Config = config;
        }
        catch { }
    }

    public static string GetMacAddress()
    {
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up &&
                    nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    var bytes = nic.GetPhysicalAddress().GetAddressBytes();
                    if (bytes != null && bytes.Length == 6)
                    {
                        var raw = string.Concat(bytes.Select(b => b.ToString("x2")));
                        if (raw != "000000000000")
                        {
                            return string.Join(":", Enumerable.Range(0, 6)
                                .Select(i => raw.Substring(i * 2, 2)));
                        }
                    }
                }
            }
        }
        catch { }
        return "00:00:00:00:00:00";
    }
}
