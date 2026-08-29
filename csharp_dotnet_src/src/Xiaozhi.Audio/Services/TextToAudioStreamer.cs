using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using NAudio.Wave;
using Xiaozhi.Audio.Codecs;
using Xiaozhi.Protocols.WebSocket;

namespace Xiaozhi.Audio.Services;

/// <summary>
/// Chuyển đổi câu hỏi dạng văn bản dài thành luồng Opus Audio tiếng Việt chất lượng cao để gửi lên server Tenclass.
/// Giải quyết triệt để lỗi từ server: "Detect is only for wake words, do not send long texts."
/// </summary>
public class TextToAudioStreamer
{
    private readonly OpusCodec _opusCodec = new();
    private static readonly HttpClient _httpClient = new();

    /// <summary>
    /// Đọc câu text thành tiếng Việt, đóng gói PCM thành Opus và giả lập một lượt nói qua WebSocket.
    /// </summary>
    public async Task StreamTextAsAudioAsync(XiaozhiWebSocketClient client, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || !client.IsConnected) return;

        try
        {
            // 1. Tải và tổng hợp PCM 16kHz từ Google TTS (tự động chia nhỏ văn bản dài thành các đoạn <= 150 ký tự)
            byte[] pcm16k = await FetchVietnameseTtsPcmAsync(text);

            if (pcm16k.Length > 0)
            {
                // 2. Gửi lệnh bắt đầu ghi âm lên server
                await client.StartListeningAsync(mode: "manual");
                await Task.Delay(200);

                int bytesPerFrame = 960 * 2;
                var pcmShorts = new short[960];

                // Gửi 4 khung im lặng (lead-in silence ~240ms) để server mở audio pipeline hoàn toàn
                var silentOpus = _opusCodec.Encode(pcmShorts);
                for (int s = 0; s < 4; s++)
                {
                    if (!client.IsConnected) break;
                    await client.SendAudioAsync(silentOpus);
                    await Task.Delay(35);
                }

                // 3. Đóng gói PCM 16kHz Mono thành các frame Opus 60ms và truyền dữ liệu giọng đọc tiếng Việt
                for (int i = 0; i < pcm16k.Length; i += bytesPerFrame)
                {
                    if (!client.IsConnected) break;

                    int chunkSize = Math.Min(bytesPerFrame, pcm16k.Length - i);
                    Array.Clear(pcmShorts, 0, 960);
                    Buffer.BlockCopy(pcm16k, i, pcmShorts, 0, chunkSize);

                    var opusFrame = _opusCodec.Encode(pcmShorts);
                    await client.SendAudioAsync(opusFrame);
                    await Task.Delay(35); // Giữ nhịp phát 35ms cho frame 60ms
                }

                // Gửi 3 khung im lặng (lead-out silence ~180ms) trước khi kết thúc
                Array.Clear(pcmShorts, 0, 960);
                var trailingSilentOpus = _opusCodec.Encode(pcmShorts);
                for (int s = 0; s < 3; s++)
                {
                    if (!client.IsConnected) break;
                    await client.SendAudioAsync(trailingSilentOpus);
                    await Task.Delay(35);
                }

                // 4. Gửi lệnh kết thúc ghi âm để server tiến hành STT & xử lý LLM
                await Task.Delay(100);
                await client.StopListeningAsync();
                return;
            }
        }
        catch (Exception ex)
        {
            XiaozhiWebSocketClient.Log($"StreamTextAsAudio Exception: {ex.Message}");
        }
    }

    /// <summary>Tải từng đoạn MP3 từ dịch vụ TTS, đổi sang PCM 16 kHz và ghép theo đúng thứ tự.</summary>
    private async Task<byte[]> FetchVietnameseTtsPcmAsync(string text)
    {
        var chunks = SplitTextIntoChunks(text, 140);
        using var combinedPcmMs = new MemoryStream();

        foreach (var chunk in chunks)
        {
            if (string.IsNullOrWhiteSpace(chunk)) continue;
            try
            {
                var url = $"https://translate.google.com/translate_tts?ie=UTF-8&q={Uri.EscapeDataString(chunk)}&tl=vi&client=tw-ob";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                var resp = await _httpClient.SendAsync(req);
                if (resp.IsSuccessStatusCode)
                {
                    var mp3Bytes = await resp.Content.ReadAsByteArrayAsync();
                    byte[] pcm16k = ConvertMp3ToPcm16k(mp3Bytes);
                    if (pcm16k.Length > 0)
                    {
                        combinedPcmMs.Write(pcm16k, 0, pcm16k.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                XiaozhiWebSocketClient.Log($"FetchTTS Chunk Exception: {ex.Message}");
            }
        }

        return combinedPcmMs.ToArray();
    }

    /// <summary>Chia văn bản theo dấu câu và từ, bảo đảm mỗi đoạn không vượt quá giới hạn TTS.</summary>
    private List<string> SplitTextIntoChunks(string text, int maxChunkSize)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return result;

        var sentences = text.Split(new[] { '.', '!', '?', '\n', ',', ';', ':', ']', '[' }, StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = "";

        foreach (var sentence in sentences)
        {
            var trimmed = sentence.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            if ((currentChunk + " " + trimmed).Length > maxChunkSize)
            {
                if (!string.IsNullOrEmpty(currentChunk))
                {
                    result.Add(currentChunk.Trim());
                    currentChunk = "";
                }

                if (trimmed.Length > maxChunkSize)
                {
                    var words = trimmed.Split(' ');
                    foreach (var word in words)
                    {
                        if ((currentChunk + " " + word).Length > maxChunkSize)
                        {
                            if (!string.IsNullOrEmpty(currentChunk))
                                result.Add(currentChunk.Trim());
                            currentChunk = word;
                        }
                        else
                        {
                            currentChunk += (string.IsNullOrEmpty(currentChunk) ? "" : " ") + word;
                        }
                    }
                }
                else
                {
                    currentChunk = trimmed;
                }
            }
            else
            {
                currentChunk += (string.IsNullOrEmpty(currentChunk) ? "" : " ") + trimmed;
            }
        }

        if (!string.IsNullOrWhiteSpace(currentChunk))
        {
            result.Add(currentChunk.Trim());
        }

        return result;
    }

    /// <summary>Giải mã byte MP3 và resample thành PCM 16-bit, mono, 16 kHz mà server yêu cầu.</summary>
    private byte[] ConvertMp3ToPcm16k(byte[] mp3Bytes)
    {
        try
        {
            using var ms = new MemoryStream(mp3Bytes);
            using var mp3Reader = new Mp3FileReader(ms);
            var targetFormat = new WaveFormat(16000, 16, 1);
            using var resampler = new MediaFoundationResampler(mp3Reader, targetFormat);

            using var outMs = new MemoryStream();
            var buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
            {
                outMs.Write(buffer, 0, bytesRead);
            }
            return outMs.ToArray();
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }
}

