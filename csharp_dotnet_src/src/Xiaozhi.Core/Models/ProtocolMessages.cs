using System.Text.Json.Serialization;

namespace Xiaozhi.Core.Models;

public class HelloMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "hello";

    [JsonPropertyName("version")]
    public int Version { get; set; } = 1; // WS uses version 1

    [JsonPropertyName("transport")]
    public string Transport { get; set; } = "websocket";

    [JsonPropertyName("features")]
    public HelloFeatures Features { get; set; } = new();

    [JsonPropertyName("audio_params")]
    public HelloAudioParams AudioParams { get; set; } = new();
}

public class HelloFeatures
{
    [JsonPropertyName("mcp")]
    public bool Mcp { get; set; } = false;

    [JsonPropertyName("aec")]
    public bool Aec { get; set; } = false;
}

public class HelloAudioParams
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = "opus";

    [JsonPropertyName("sample_rate")]
    public int SampleRate { get; set; } = 16000;

    [JsonPropertyName("channels")]
    public int Channels { get; set; } = 1;

    [JsonPropertyName("frame_duration")]
    public int FrameDuration { get; set; } = 60;
}

public class ListenMessage
{
    [JsonPropertyName("session_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SessionId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "listen";

    [JsonPropertyName("state")]
    public string State { get; set; } = "start"; // start, stop, detect

    [JsonPropertyName("mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Mode { get; set; } // manual, auto, realtime

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }
}

public class AbortMessage
{
    [JsonPropertyName("session_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SessionId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "abort";

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "wake_word_detected";
}
