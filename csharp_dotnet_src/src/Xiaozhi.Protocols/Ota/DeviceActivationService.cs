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
            // Chuẩn hoá và làm sạch MAC
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

            // Software client: only use a deterministic firmware fingerprint.
            // Do not claim a factory-programmed eFuse serial/HMAC identity.
            var hmacKey = DeviceFingerprint.GenerateHmacKey(cleanMac);

            var payload = new
            {
                application = new
                {
                    version = SystemConstants.AppVersion,
                    elf_sha256 = hmacKey
                },
                board = new
                {
                    type = SystemConstants.BoardType,
                    name = SystemConstants.AppName,
                    ip = GetLocalIpAddress(),
                    mac = macAddress,
                    mac_address = macAddress
                },
                mac = macAddress,
                mac_address = macAddress
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
            request.Headers.Add("Activation-Version", SystemConstants.ActivationVersion);
            request.Headers.Add("Mac-Address", macAddress);

            result.RawRequest = $"POST {_otaUrl}\nHeaders:\n  Device-Id: {macAddress}\n  Client-Id: {deviceId}\n  User-Agent: {SystemConstants.BoardType}/{SystemConstants.AppName}-{SystemConstants.AppVersion}\n  Activation-Version: {SystemConstants.ActivationVersion}\nBody:\n{jsonBody}";

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            result.RawResponse = $"HTTP {(int)response.StatusCode} {response.StatusCode}\nBody:\n{responseBody}";

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // 1. Luôn trích xuất Token và WebSocket URL nếu có trong response bất kể có mã activation hay không
            string? token = null;
            string? wsUrl = null;

            if (root.TryGetProperty("websocket", out var wsElem))
            {
                if (wsElem.TryGetProperty("token", out var wsTokenProp)) token = wsTokenProp.GetString();
                if (wsElem.TryGetProperty("url", out var wsUrlProp)) wsUrl = wsUrlProp.GetString();
            }

            if (string.IsNullOrEmpty(token) && root.TryGetProperty("token", out var topTokenProp))
            {
                token = topTokenProp.GetString();
            }
            if (string.IsNullOrEmpty(wsUrl) && root.TryGetProperty("websocket_url", out var topWsProp))
            {
                wsUrl = topWsProp.GetString();
            }

            result.Token = token;
            result.WebSocketUrl = wsUrl ?? "wss://api.tenclass.net/xiaozhi/v1/";

            // 2. Kiểm tra nếu có mã OTP yêu cầu kích hoạt (activation.code)
            if (root.TryGetProperty("activation", out var actElem))
            {
                if (actElem.TryGetProperty("code", out var codeProp)) result.Code = codeProp.GetString();
                if (actElem.TryGetProperty("url", out var urlProp)) result.QrUrl = urlProp.GetString();
                if (actElem.TryGetProperty("message", out var msgProp)) result.Message = msgProp.GetString();
                if (!string.IsNullOrEmpty(result.Code))
                {
                    // Nếu server chưa trả token chính thức mà có mã OTP, đánh dấu chưa kích hoạt hoàn toàn nhưng vẫn giữ token tạm (nếu có)
                    result.IsActivated = string.IsNullOrEmpty(result.Code);
                    return result;
                }
            }

            // Parse direct properties: code, activation_code, otp, verification_code
            string[] possibleCodeProps = { "code", "activation_code", "otp", "verification_code" };
            foreach (var prop in possibleCodeProps)
            {
                if (root.TryGetProperty(prop, out var codeElem))
                {
                    result.Code = codeElem.GetString();
                    result.QrUrl = $"https://xiaozhi.me/active?code={result.Code}";
                    return result;
                }
            }

            if (!string.IsNullOrEmpty(token))
            {
                result.IsActivated = true;
                return result;
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
