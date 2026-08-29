using System;
using Concentus.Enums;
using Concentus.Structs;
using Xiaozhi.Core.Constants;

namespace Xiaozhi.Audio.Codecs;

public class OpusCodec : IDisposable
{
    private readonly OpusEncoder _encoder;
    private readonly OpusDecoder _decoder24k;
    private readonly OpusDecoder _decoder16k;
    private readonly int _frameSize;

    /// <summary>Khởi tạo encoder cho audio gửi lên và decoder cho audio TTS nhận về.</summary>
    public OpusCodec(int inputSampleRate = SystemConstants.SampleRate, int channels = SystemConstants.Channels)
    {
        _frameSize = SystemConstants.FrameSize; // 960 samples for 60ms @ 16kHz
        _encoder = new OpusEncoder(inputSampleRate, channels, OpusApplication.OPUS_APPLICATION_VOIP);
        _encoder.Bitrate = 32000;
        
        // 24kHz decoder for server TTS responses (server returns 24kHz)
        _decoder24k = new OpusDecoder(24000, 1);
        _decoder16k = new OpusDecoder(16000, 1);
    }

    /// <summary>Nén một frame PCM 16-bit thành gói Opus có kích thước thực tế.</summary>
    public byte[] Encode(short[] pcmSamples)
    {
        var outputBuffer = new byte[1000];
        int encodedBytes = _encoder.Encode(pcmSamples, 0, pcmSamples.Length, outputBuffer, 0, outputBuffer.Length);
        var result = new byte[encodedBytes];
        Array.Copy(outputBuffer, result, encodedBytes);
        return result;
    }

    /// <summary>Giải mã một gói Opus TTS 24 kHz; trả mảng rỗng khi gói lỗi.</summary>
    public short[] Decode24k(byte[] opusData)
    {
        var outputPcm = new short[2880]; // Max 120ms @ 24kHz
        try
        {
            int decodedSamples = _decoder24k.Decode(opusData, 0, opusData.Length, outputPcm, 0, outputPcm.Length, false);
            if (decodedSamples > 0)
            {
                var result = new short[decodedSamples];
                Array.Copy(outputPcm, result, decodedSamples);
                return result;
            }
        }
        catch { }
        return Array.Empty<short>();
    }

    /// <summary>Reset trạng thái nội bộ của encoder/decoder khi codec được giải phóng.</summary>
    public void Dispose()
    {
        _encoder.ResetState();
        _decoder24k.ResetState();
        _decoder16k.ResetState();
    }
}
