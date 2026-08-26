using System.Text.Json.Serialization;

namespace Xiaozhi.Core.Models;

// Mirror config.json of original Lily Python app
public class AppConfig
{
    [JsonPropertyName("SYSTEM_OPTIONS")]
    public SystemOptions SystemOptions { get; set; } = new();

    [JsonPropertyName("WAKE_WORD_OPTIONS")]
    public WakeWordOptions WakeWordOptions { get; set; } = new();
}

public class SystemOptions
{
    [JsonPropertyName("CLIENT_ID")]
    public string ClientId { get; set; } = System.Guid.NewGuid().ToString();

    [JsonPropertyName("DEVICE_ID")]
    public string DeviceId { get; set; } = string.Empty; // MAC address

    [JsonPropertyName("NETWORK")]
    public NetworkOptions Network { get; set; } = new();
}

public class NetworkOptions
{
    [JsonPropertyName("OTA_VERSION_URL")]
    public string OtaVersionUrl { get; set; } = "https://api.tenclass.net/xiaozhi/ota/";

    [JsonPropertyName("WEBSOCKET_URL")]
    public string WebSocketUrl { get; set; } = "wss://api.tenclass.net/xiaozhi/v1/";

    [JsonPropertyName("WEBSOCKET_ACCESS_TOKEN")]
    public string WebSocketAccessToken { get; set; } = string.Empty;

    [JsonPropertyName("AUTHORIZATION_URL")]
    public string AuthorizationUrl { get; set; } = "https://xiaozhi.me/";
}

public class WakeWordOptions
{
    [JsonPropertyName("USE_WAKE_WORD")]
    public bool UseWakeWord { get; set; } = true;

    [JsonPropertyName("MODEL_PATH")]
    public string ModelPath { get; set; } = "models";

    [JsonPropertyName("NUM_THREADS")]
    public int NumThreads { get; set; } = 4;

    [JsonPropertyName("KEYWORDS_SCORE")]
    public float KeywordsScore { get; set; } = 1.8f;

    [JsonPropertyName("KEYWORDS_THRESHOLD")]
    public float KeywordsThreshold { get; set; } = 0.2f;
}
