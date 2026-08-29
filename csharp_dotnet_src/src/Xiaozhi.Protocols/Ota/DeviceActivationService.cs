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

/// <summary>
/// Kết quả trả về sau khi thực hiện kiểm tra hoặc gửi yêu cầu kích hoạt thiết bị qua OTA Server.
/// </summary>
public class ActivationResult
{
    /// <summary> Cờ đánh dấu thiết bị đã được kích hoạt thành công hay chưa </summary>
    public bool IsActivated { get; set; }

    /// <summary> Mã xác thực OTP (nếu thiết bị chưa được kích hoạt và cần người dùng quét QR / nhập mã) </summary>
    public string? Code { get; set; }

    /// <summary> Đường dẫn URL mã QR để người dùng thực hiện quét kích hoạt </summary>
    public string? QrUrl { get; set; }

    /// <summary> Token xác thực truy cập WebSocket Server nhận được sau khi kích hoạt thành công </summary>
    public string? Token { get; set; }

    /// <summary> Đường dẫn URL kết nối WebSocket Server do OTA chỉ định </summary>
    public string? WebSocketUrl { get; set; }

    /// <summary> Thông báo chi tiết từ Server hoặc thông báo lỗi nếu có </summary>
    public string? Message { get; set; }

    /// <summary> Chuỗi thông tin thô của Request được gửi đi (phục vụ log & debug) </summary>
    public string? RawRequest { get; set; }

    /// <summary> Chuỗi phản hồi thô nhận về từ Server (phục vụ log & debug) </summary>
    public string? RawResponse { get; set; }
}

/// <summary>
/// Service quản lý kích hoạt thiết bị và giao tiếp với OTA (Over-The-Air) Server.
/// </summary>
public class DeviceActivationService
{
    // Đối tượng HttpClient tĩnh được tái sử dụng để tránh cạn kiệt socket, cấu hình timeout 15 giây
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
    
    // Đường dẫn URL của OTA Server
    private readonly string _otaUrl;

    /// <summary>
    /// Khởi tạo DeviceActivationService với URL OTA Server tùy chọn (mặc định lấy theo URL API mặc định).
    /// </summary>
    /// <param name="otaUrl">Đường dẫn OTA Server (nếu null sẽ lấy URL mặc định)</param>
    public DeviceActivationService(string? otaUrl = null)
    {
        _otaUrl = otaUrl ?? "https://api.tenclass.net/xiaozhi/ota/";
    }

    /// <summary>
    /// Kiểm tra trạng thái kích hoạt hoặc gửi yêu cầu kích hoạt thiết bị lên OTA Server.
    /// </summary>
    /// <param name="deviceId">ID định danh client / thiết bị</param>
    /// <param name="macAddress">Địa chỉ MAC của thiết bị</param>
    /// <returns>Đối tượng ActivationResult chứa thông tin kích hoạt</returns>
    public async Task<ActivationResult> CheckOrRequestActivationAsync(string deviceId, string macAddress)
    {
        var result = new ActivationResult();
        try
        {
            // 1. Chuẩn hoá và làm sạch địa chỉ MAC nhập vào (loại bỏ ký tự đặc biệt, khoảng trắng)
            var firstMacPart = (macAddress ?? "").Split(new[] { ' ', ',', ';', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            var rawMac = new string(firstMacPart.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

            // Đảm bảo độ dài chuỗi MAC là 12 ký tự hex (ví dụ: aabbcc112233)
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

            // 2. Tạo khóa HMAC tĩnh theo firmware fingerprint dựa trên MAC sạch
            var hmacKey = DeviceFingerprint.GenerateHmacKey(cleanMac);

            // 3. Xây dựng Payload JSON theo cấu trúc yêu cầu của OTA Server Xiaozhi
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

            // 4. Chuẩn bị Request HTTP POST
            var jsonBody = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            var jsonContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, _otaUrl)
            {
                Content = jsonContent
            };

            // Thêm các thông tin Header bắt buộc vào HTTP Request
            request.Headers.Add("Device-Id", macAddress);
            request.Headers.Add("Client-Id", deviceId);
            request.Headers.Add("User-Agent", $"{SystemConstants.BoardType}/{SystemConstants.AppName}-{SystemConstants.AppVersion}");
            request.Headers.Add("Accept-Language", "zh-CN");
            request.Headers.Add("Activation-Version", SystemConstants.ActivationVersion);
            request.Headers.Add("Mac-Address", macAddress);

            // Ghi lại thô thông tin Request để hỗ trợ hiển thị log debug
            result.RawRequest = $"POST {_otaUrl}\nHeaders:\n  Device-Id: {macAddress}\n  Client-Id: {deviceId}\n  User-Agent: {SystemConstants.BoardType}/{SystemConstants.AppName}-{SystemConstants.AppVersion}\n  Activation-Version: {SystemConstants.ActivationVersion}\nBody:\n{jsonBody}";

            // 5. Gửi HTTP POST Request tới OTA Server và đọc Response thu được
            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Ghi lại thô thông tin Response để hỗ trợ hiển thị log debug
            result.RawResponse = $"HTTP {(int)response.StatusCode} {response.StatusCode}\nBody:\n{responseBody}";

            // Parse nội dung Response JSON nhận về từ OTA Server
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // 6. Trích xuất Token và WebSocket URL nếu có trong response bất kể trạng thái kích hoạt
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

            // 7. Kiểm tra nếu có node "activation" chứa mã OTP yêu cầu kích hoạt từ người dùng
            if (root.TryGetProperty("activation", out var actElem))
            {
                if (actElem.TryGetProperty("code", out var codeProp)) result.Code = codeProp.GetString();
                if (actElem.TryGetProperty("url", out var urlProp)) result.QrUrl = urlProp.GetString();
                if (actElem.TryGetProperty("message", out var msgProp)) result.Message = msgProp.GetString();
                if (!string.IsNullOrEmpty(result.Code))
                {
                    // Đánh dấu trạng thái kích hoạt phụ thuộc vào việc có mã OTP cần xác thực hay không
                    result.IsActivated = string.IsNullOrEmpty(result.Code);
                    return result;
                }
            }

            // 8. Trích xuất trực tiếp mã kích hoạt từ các property phổ biến khác nếu có
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

            // 9. Nếu đã có token và không cần nhập mã kích hoạt, đánh dấu kích hoạt thành công
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
            // Bắt lỗi ngoại lệ mạng / kết nối và trả về kết quả lỗi
            return new ActivationResult
            {
                IsActivated = false,
                Message = $"Lỗi kết nối OTA: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Vòng lặp chờ kích hoạt (Poll Token) định kỳ để lấy Token khi người dùng quét mã OTP trên điện thoại.
    /// </summary>
    /// <param name="deviceId">ID thiết bị</param>
    /// <param name="macAddress">Địa chỉ MAC thiết bị</param>
    /// <param name="maxRetries">Số lần thử tối đa (mặc định 60 lần)</param>
    /// <param name="intervalSeconds">Khoảng thời gian nghỉ giữa các lần thử tính bằng giây (mặc định 3 giây)</param>
    /// <param name="cancellationToken">Token hỗ trợ hủy thao tác từ xa</param>
    /// <returns>Chuỗi Token kích hoạt nếu thành công, hoặc null nếu hết thời gian chờ</returns>
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

    /// <summary>
    /// Lấy địa chỉ IP nội bộ (IPv4) của máy hiện tại để gửi trong payload lên OTA Server.
    /// </summary>
    /// <returns>Chuỗi địa chỉ IP IPv4</returns>
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
