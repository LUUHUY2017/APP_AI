using System.Text.Json.Serialization;

namespace Xiaozhi.Core.Models;

public class EfuseConfig
{
    [JsonPropertyName("mac_address")]
    public string MacAddress { get; set; } = "38:60:77:dc:90:11";

    [JsonPropertyName("serial_number")]
    public string SerialNumber { get; set; } = "SN-B18EE69C-386077dc9011";

    [JsonPropertyName("hmac_key")]
    public string HmacKey { get; set; } = "4a8b1dac46b2f64dfefe39ee1279ac6f6ec7f197bcbcfdf01a373d7e516a976b";

    [JsonPropertyName("activation_status")]
    public bool ActivationStatus { get; set; } = true;
}
