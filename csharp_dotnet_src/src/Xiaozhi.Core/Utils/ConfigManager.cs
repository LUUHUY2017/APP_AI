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

    private static readonly string EfusePath = Path.Combine(ConfigDir, "efuse.json");
    private static readonly string LocalEfusePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "efuse.json");

    public AppConfig Config { get; private set; }
    public EfuseConfig Efuse { get; private set; }

    private ConfigManager()
    {
        Config = LoadOrCreate();
        Efuse = LoadOrCreateEfuse();
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

    private EfuseConfig LoadOrCreateEfuse()
    {
        try
        {
            if (File.Exists(EfusePath))
            {
                var json = File.ReadAllText(EfusePath);
                var efuse = JsonSerializer.Deserialize<EfuseConfig>(json);
                if (efuse != null && !string.IsNullOrWhiteSpace(efuse.MacAddress)) return efuse;
            }

            if (File.Exists(LocalEfusePath))
            {
                var json = File.ReadAllText(LocalEfusePath);
                var efuse = JsonSerializer.Deserialize<EfuseConfig>(json);
                if (efuse != null && !string.IsNullOrWhiteSpace(efuse.MacAddress)) return efuse;
            }
        }
        catch { }

        var defaultEfuse = CreateDefaultEfuse();
        SaveEfuse(defaultEfuse);
        return defaultEfuse;
    }

    private AppConfig CreateDefault()
    {
        return new AppConfig
        {
            SystemOptions = new SystemOptions
            {
                ClientId = "d7377f0a-2682-4e4f-a125-e0a78c730cf8",
                DeviceId = "38:60:77:dc:90:11",
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

    private EfuseConfig CreateDefaultEfuse()
    {
        var mac = Config?.SystemOptions?.DeviceId ?? "38:60:77:dc:90:11";
        var serial = DeviceFingerprint.GenerateSerialNumber(mac);
        return new EfuseConfig
        {
            MacAddress = mac,
            SerialNumber = serial,
            HmacKey = DeviceFingerprint.GenerateHmacKey(mac),
            ActivationStatus = true
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

    public void SaveEfuse(EfuseConfig efuse)
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(EfusePath, JsonSerializer.Serialize(efuse, options));
            Efuse = efuse;
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
