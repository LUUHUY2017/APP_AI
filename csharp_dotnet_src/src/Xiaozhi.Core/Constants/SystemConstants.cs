namespace Xiaozhi.Core.Constants;

public static class SystemConstants
{
    public const string AppName = "py-xiaozhi";
    public const string AppVersion = "2.0.0";
    public const string ActivationVersion = "2";
    public const string BoardType = "bread-compact-wifi";
    public const int DefaultTimeoutSeconds = 10;
    public const int ActivationMaxRetries = 60;
    public const int ActivationRetryIntervalSeconds = 5;
    
    // Audio constants
    public const int SampleRate = 16000;
    public const int Channels = 1;
    public const int FrameDurationMs = 60;
    public const int FrameSize = SampleRate * FrameDurationMs / 1000; // 960 samples
}
