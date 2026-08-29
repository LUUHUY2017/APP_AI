using System;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace Xiaozhi.Core.Utils;

public static class DeviceFingerprint
{
    /// <summary>Tìm MAC của card mạng đang hoạt động; dùng giá trị dự phòng nếu không tìm thấy.</summary>
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

    /// <summary>Ghép tên máy, tài khoản và MAC rồi băm MD5 để tạo ID ổn định cho thiết bị.</summary>
    public static string GenerateDeviceId()
    {
        var raw = $"{Environment.MachineName}-{Environment.UserName}-{GetMacAddress()}";
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>Chuẩn hóa MAC thành 12 ký tự và tạo serial có dạng SN-HASH-MAC.</summary>
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

    /// <summary>Băm MAC bằng SHA-256 để tạo khóa nhận dạng dùng trong efuse cục bộ.</summary>
    public static string GenerateHmacKey(string macAddress)
    {
        var cleanMac = (macAddress ?? "").Replace(":", "").Replace("-", "").ToLowerInvariant();
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(cleanMac));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
