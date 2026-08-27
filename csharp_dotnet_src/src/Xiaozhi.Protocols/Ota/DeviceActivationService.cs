using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xiaozhi.Core.Constants;
using Xiaozhi.Core.Utils;

namespace Xiaozhi.Protocols.Ota;

public class ActivationResult
{
    public bool IsActivated { get; set; }
    public string? Code { get; set; }
    public string? QrUrl { get; set; }
    public string? Token { get; set; }
    public string? WebSocketUrl { get; set; }
    public string? Message { get; set; }
    public string? RawRequest { get; set; }
    public string? RawResponse { get; set; }
}

public class DeviceActivationService
{
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly string _otaUrl;

    public DeviceActivationService(string? otaUrl = null)
    {
        _otaUrl = otaUrl ?? "https://api.tenclass.net/xiaozhi/ota/";
    }

    public async Task<ActivationResult> CheckOrRequestActivationAsync(string deviceId, string macAddress)
    {
        var result = new ActivationResult();
        try
        {
            // Chuẩn hoá và làm sạch MAC: Xử lý trường hợp có nhiều MAC ngăn cách bằng khoảng trắng (ví dụ trên iOS)
            var firstMacPart = (macAddress ?? "").Split(new[] { ' ', ',', ';', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            var rawMac = new string(firstMacPart.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            if (rawMac.Length >= 12)
            {
                rawMac = rawMac.Substring(0, 12);
                macAddress = string.Join(":", Enumerable.Range(0, 6).Select(i => rawMac.Substring(i * 2, 2)));
            }
            else if (rawMac.Length > 0)
            {
                rawMac = rawMac.PadRight(12, '0').Substring(0, 12);
                macAddress = string.Join(":", Enumerable.Range(0, 6).Select(i => rawMac.Substring(i * 2, 2)));
            }
            else
            {
                rawMac = "000000000000";
                macAddress = "00:00:00:00:00:00";
            }
            var cleanMac = rawMac;

            var payload = new
            {
                application = new
                {
                    version = SystemConstants.AppVersion,
                    elf_sha256 = deviceId
                },
                board = new
                {
                    type = SystemConstants.BoardType,
                    name = SystemConstants.AppName,
                    ip = GetLocalIpAddress(),
                    mac = macAddress,
                    mac_address = macAddress,
                    serial_number = cleanMac,
                    sn = cleanMac
                },
                mac = macAddress,
                mac_address = macAddress,
                serial_number = cleanMac,
                sn = cleanMac
            };

            var jsonBody = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            var jsonContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, _otaUrl)
            {
                Content = jsonContent
            };
            request.Headers.Add("Device-Id", macAddress);
            request.Headers.Add("Client-Id", deviceId);
            request.Headers.Add("User-Agent", $"{SystemConstants.BoardType}/{SystemConstants.AppName}-{SystemConstants.AppVersion}");
            request.Headers.Add("Accept-Language", "zh-CN");
            request.Headers.Add("Activation-Version", SystemConstants.AppVersion);
            request.Headers.Add("Mac-Address", macAddress);
            request.Headers.Add("Serial-Number", cleanMac);

            result.RawRequest = $"POST {_otaUrl}\nHeaders:\n  Device-Id: {macAddress}\n  Client-Id: {deviceId}\n  Serial-Number: {cleanMac}\n  User-Agent: {SystemConstants.BoardType}/{SystemConstants.AppName}-{SystemConstants.AppVersion}\n  Activation-Version: {SystemConstants.AppVersion}\nBody:\n{jsonBody}";

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            result.RawResponse = $"HTTP {(int)response.StatusCode} {response.StatusCode}\nBody:\n{responseBody}";

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("token", out var tokenProp))
            {
                result.IsActivated = true;
                result.Token = tokenProp.GetString();
                if (root.TryGetProperty("websocket_url", out var wsProp))
                {
                    result.WebSocketUrl = wsProp.GetString();
                }
                return result;
            }

            if (root.TryGetProperty("activation", out var actElem))
            {
                result.IsActivated = false;
                if (actElem.TryGetProperty("code", out var codeProp)) result.Code = codeProp.GetString();
                if (actElem.TryGetProperty("url", out var urlProp)) result.QrUrl = urlProp.GetString();
                if (actElem.TryGetProperty("message", out var msgProp)) result.Message = msgProp.GetString();
                if (!string.IsNullOrEmpty(result.Code)) return result;
            }

            // Parse direct properties: code, activation_code, otp, verification_code
            string[] possibleCodeProps = { "code", "activation_code", "otp", "verification_code", "challenge" };
            foreach (var prop in possibleCodeProps)
            {
                if (root.TryGetProperty(prop, out var codeElem))
                {
                    result.Code = codeElem.GetString();
                    result.QrUrl = $"https://xiaozhi.me/active?code={result.Code}";
                    return result;
                }
            }

            result.Message = $"Server phản hồi: {responseBody}";
            return result;
        }
        catch (Exception ex)
        {
            return new ActivationResult
            {
                IsActivated = false,
                Message = $"Lỗi kết nối OTA: {ex.Message}"
            };
        }
    }

    public async Task<string?> PollForTokenAsync(string deviceId, string macAddress, int maxRetries = 60, int intervalSeconds = 3, CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var result = await CheckOrRequestActivationAsync(deviceId, macAddress);
            if (result.IsActivated && !string.IsNullOrEmpty(result.Token))
            {
                return result.Token;
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cancellationToken);
        }

        return null;
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
        }
        catch { }
        return "192.168.1.100";
    }
}
