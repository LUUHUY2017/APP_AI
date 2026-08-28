using System.Text.Json.Serialization;

namespace Xiaozhi.Core.Models;

public class EfuseConfig
{
    [JsonPropertyName("mac_address")]
    public string MacAddress { get; set; } = "cc:30:80:20:64:7c";

    [JsonPropertyName("serial_number")]
    public string SerialNumber { get; set; } = "SN-F396BDD6-cc308020647c";

    [JsonPropertyName("hmac_key")]
    public string HmacKey { get; set; } = string.Empty;

    [JsonPropertyName("activation_status")]
    public bool ActivationStatus { get; set; } = false;
}
