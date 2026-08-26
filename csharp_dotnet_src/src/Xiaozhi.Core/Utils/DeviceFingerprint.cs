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
}
