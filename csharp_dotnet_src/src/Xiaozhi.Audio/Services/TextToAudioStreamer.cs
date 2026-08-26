using System;
using System.IO;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using Xiaozhi.Audio.Codecs;
using Xiaozhi.Protocols.WebSocket;

namespace Xiaozhi.Audio.Services;

/// <summary>
/// Chuyển đổi câu hỏi dạng văn bản dài thành luồng Opus Audio để gửi lên server Tenclass
/// Giải quyết triệt để lỗi: "Detect is only for wake words, do not send long texts."
/// </summary>
public class TextToAudioStreamer
{
    private readonly OpusCodec _opusCodec = new();

    public async Task StreamTextAsAudioAsync(XiaozhiWebSocketClient client, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || !client.IsConnected) return;

        // 1. Gửi lệnh bắt đầu nghe
        await client.StartListeningAsync(mode: "manual");

        // 2. Chuyển văn bản thành PCM 16kHz Mono 16-bit
        byte[] pcmData = SynthesizeToPcm(text);

        if (pcmData.Length > 44) // Bỏ qua header WAV 44 bytes nếu có
        {
            int pcmOffset = 44;
            int bytesPerFrame = 960 * 2; // 60ms @ 16kHz (1920 bytes)
            var pcmShorts = new short[960];

            for (int i = pcmOffset; i < pcmData.Length; i += bytesPerFrame)
            {
                int chunkSize = Math.Min(bytesPerFrame, pcmData.Length - i);
                if (chunkSize < bytesPerFrame)
                {
                    Array.Clear(pcmShorts, 0, 960);
                    Buffer.BlockCopy(pcmData, i, pcmShorts, 0, chunkSize);
                }
                else
                {
                    Buffer.BlockCopy(pcmData, i, pcmShorts, 0, bytesPerFrame);
                }

                var opusFrame = _opusCodec.Encode(pcmShorts);
                await client.SendAudioAsync(opusFrame);
                await Task.Delay(20); // Stream nhịp nhàng
            }
        }

        // 3. Gửi lệnh kết thúc nghe để server tiến hành xử lý
        await Task.Delay(100);
        await client.StopListeningAsync();
    }

    private byte[] SynthesizeToPcm(string text)
    {
        try
        {
            using var synth = new SpeechSynthesizer();
            using var stream = new MemoryStream();
            
            // Format 16kHz 16-bit Mono PCM
            var format = new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, AudioChannel.Mono);
            synth.SetOutputToAudioStream(stream, format);
            synth.Rate = 1;
            synth.Speak(text);
            synth.SetOutputToNull();

            return stream.ToArray();
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }
}
