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
                ClientId = Guid.NewGuid().ToString(),
                DeviceId = GetMacAddress(),
                Network = new NetworkOptions
                {
                    OtaVersionUrl = "https://api.tenclass.net/xiaozhi/ota/",
                    WebSocketUrl = "wss://api.tenclass.net/xiaozhi/v1/",
                    WebSocketAccessToken = string.Empty,
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
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus == OperationalStatus.Up &&
                nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            {
                var mac = nic.GetPhysicalAddress().ToString();
                if (mac.Length == 12)
                {
                    // Format as xx:xx:xx:xx:xx:xx
                    return string.Join(":", Enumerable.Range(0, 6)
                        .Select(i => mac.Substring(i * 2, 2).ToLower()));
                }
            }
        }
        return "00:00:00:00:00:00";
    }
}
