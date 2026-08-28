using System;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace Xiaozhi.Core.Utils;

public static class DeviceFingerprint
{
    public static string GetMacAddress()
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus == OperationalStatus.Up && 
                nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            {
                return nic.GetPhysicalAddress().ToString();
            }
        }
        return "001122334455";
    }

    public static string GenerateDeviceId()
    {
        var raw = $"{Environment.MachineName}-{Environment.UserName}-{GetMacAddress()}";
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string GenerateSerialNumber(string macAddress)
    {
        var cleanMac = (macAddress ?? "").Replace(":", "").Replace("-", "").ToLowerInvariant();
        if (cleanMac.Length > 12) cleanMac = cleanMac.Substring(0, 12);
        else if (cleanMac.Length < 12) cleanMac = cleanMac.PadRight(12, '0');

        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(cleanMac));
        var hex8 = Convert.ToHexString(hash).Substring(0, 8).ToUpperInvariant();
        return $"SN-{hex8}-{cleanMac}";
    }

    public static string GenerateHmacKey(string macAddress)
    {
        var cleanMac = (macAddress ?? "").Replace(":", "").Replace("-", "").ToLowerInvariant();
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(cleanMac));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
