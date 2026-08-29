using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace Xiaozhi.App.Maui.Services;

public class OtaAutoUpdateService
{
    private static readonly HttpClient _httpClient = new();
    private const string GITHUB_RELEASES_URL = "https://api.github.com/repos/LUUHUY2017/APP_AI/releases/latest";
    private const string DEFAULT_DOWNLOAD_URL = "https://github.com/LUUHUY2017/APP_AI/releases/latest";

    public async Task CheckForUpdatesAsync(Page page, bool silentIfLatest = true)
    {
        try
        {
            var currentVersionStr = AppInfo.Current.VersionString; // e.g. "1.0.0"
            if (string.IsNullOrEmpty(currentVersionStr)) currentVersionStr = "1.0.0";

            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Xiaozhi-Lily-App/1.0");
            }

            var resp = await _httpClient.GetAsync(GITHUB_RELEASES_URL);
            if (!resp.IsSuccessStatusCode)
            {
                if (!silentIfLatest)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await page.DisplayAlert("Thông báo", "Hiện tại bạn đang sử dụng phiên bản mới nhất!", "OK");
                    });
                }
                return;
            }

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() : "";
            var releaseNotes = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() : "Cải tiến hiệu năng & tính năng mới.";
            
            string downloadUrl = DEFAULT_DOWNLOAD_URL;
            string directIpaUrl = "";
            string plistUrl = "";

            // Duyệt mảng assets để tìm file manifest.plist (iOS OTA Manifest) hoặc file .ipa
            if (root.TryGetProperty("assets", out var assetsElem) && assetsElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assetsElem.EnumerateArray())
                {
                    var assetName = asset.TryGetProperty("name", out var np) ? np.GetString() : "";
                    if (string.IsNullOrEmpty(assetName)) continue;

                    if (assetName.EndsWith(".plist", StringComparison.OrdinalIgnoreCase))
                    {
                        if (asset.TryGetProperty("browser_download_url", out var bdp) && !string.IsNullOrEmpty(bdp.GetString()))
                        {
                            plistUrl = bdp.GetString()!;
                        }
                    }
                    else if (assetName.EndsWith(".ipa", StringComparison.OrdinalIgnoreCase))
                    {
                        if (asset.TryGetProperty("browser_download_url", out var bdp) && !string.IsNullOrEmpty(bdp.GetString()))
                        {
                            directIpaUrl = bdp.GetString()!;
                        }
                    }
                }
            }

            // Ưu tiên 1: Cài đặt trực tiếp không dây trên iOS qua giao thức itms-services (Enterprise/OTA Manifest)
            if (!string.IsNullOrEmpty(plistUrl))
            {
                downloadUrl = $"itms-services://?action=download-manifest&url={Uri.EscapeDataString(plistUrl)}";
            }
            // Ưu tiên 2: Link trực tiếp tới file .ipa (Dành cho Signer ứng dụng trên iOS như TrollStore / Scarlet / AltStore / ESign)
            else if (!string.IsNullOrEmpty(directIpaUrl))
            {
                downloadUrl = directIpaUrl;
            }
            // Ưu tiên 3: Trang phát hành GitHub Releases
            else if (root.TryGetProperty("html_url", out var urlProp) && !string.IsNullOrEmpty(urlProp.GetString()))
            {
                downloadUrl = urlProp.GetString()!;
            }

            // Clean version string (e.g. "v1.0.1" -> "1.0.1")
            var remoteVersionStr = tagName?.TrimStart('v', 'V') ?? "";

            if (IsNewerVersion(remoteVersionStr, currentVersionStr))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    bool updateNow = await page.DisplayAlert(
                        $"🚀 Cập nhật mới (v{remoteVersionStr})",
                        $"Đã có phiên bản mới v{remoteVersionStr}!\n\nNội dung cập nhật:\n{releaseNotes}\n\nBạn có muốn tự động tải & cập nhật ngay không?",
                        "⚡ Cập nhật ngay",
                        "Để sau"
                    );

                    if (updateNow)
                    {
                        try
                        {
                            await Launcher.Default.OpenAsync(new Uri(downloadUrl));
                        }
                        catch { }
                    }
                });
            }
            else if (!silentIfLatest)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await page.DisplayAlert("Phiên bản", $"Ứng dụng đang ở phiên bản mới nhất (v{currentVersionStr}).", "OK");
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OtaAutoUpdateService error: {ex.Message}");
        }
    }

    private bool IsNewerVersion(string remoteVer, string currentVer)
    {
        if (Version.TryParse(remoteVer, out var remote) && Version.TryParse(currentVer, out var current))
        {
            return remote > current;
        }
        return string.Compare(remoteVer, currentVer, StringComparison.OrdinalIgnoreCase) > 0;
    }
}
