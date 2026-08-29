using System;
using System.IO;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Xiaozhi.Core.Constants;
using Xiaozhi.Core.Interfaces;

namespace Xiaozhi.Audio.Services;

public class NAudioAudioService : IAudioService
{
    // Thu và phát là hai pipeline độc lập: IWaveIn cho micro, BufferedWaveProvider cho loa.
    private IWaveIn? _waveIn;
    private WaveOutEvent? _waveOut;
    private BufferedWaveProvider? _bufferedWaveProvider;
    private float _volume = 0.9f;

    public event Action<byte[]>? OnAudioRecorded;
    public bool IsRecording { get; private set; }
    public bool IsPlaying { get; private set; }

    /// <summary>Tạo service và chuẩn bị pipeline loa ngay để sẵn sàng nhận TTS.</summary>
    public NAudioAudioService()
    {
        InitializePlayback();
    }

    /// <summary>Tạo buffer PCM 24 kHz và WaveOut chạy nền để phát TTS liên tục.</summary>
    private void InitializePlayback()
    {
        // Server TTS output is 24kHz, 16-bit, Mono
        var waveFormat = new WaveFormat(24000, 16, 1);
        _bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
        {
            BufferLength = 24000 * 2 * 10, // 10 seconds buffer
            DiscardOnBufferOverflow = true
        };

        _waveOut = new WaveOutEvent();
        _waveOut.Init(_bufferedWaveProvider);
        _waveOut.Volume = _volume;
        _waveOut.Play();
    }

    /// <summary>Mở micro mặc định bằng WASAPI, fallback WaveIn, rồi phát từng buffer PCM.</summary>
    public void StartRecording()
    {
        if (IsRecording) return;

        try
        {
            var targetFormat = new WaveFormat(SystemConstants.SampleRate, 16, SystemConstants.Channels);

            // 1. Ưu tiên dùng WasapiCapture để tự động lấy Micro mặc định đang hoạt động trong Windows
            try
            {
                var wasapi = new WasapiCapture();
                wasapi.DataAvailable += (sender, args) =>
                {
                    if (args.BytesRecorded > 0)
                    {
                        byte[] pcm16k = ResampleToPcm16k(args.Buffer, args.BytesRecorded, wasapi.WaveFormat, targetFormat);
                        if (pcm16k.Length > 0)
                        {
                            OnAudioRecorded?.Invoke(pcm16k);
                        }
                    }
                };

                _waveIn = wasapi;
                _waveIn.StartRecording();
                IsRecording = true;
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WASAPI capture error: {ex.Message}");
            }

            // 2. Fallback: Dùng WaveInEvent tìm thiết bị đầu vào khả dụng đầu tiên
            int selectedDevice = 0;
            int count = WaveInEvent.DeviceCount;
            for (int i = 0; i < count; i++)
            {
                var caps = WaveInEvent.GetCapabilities(i);
                if (caps.Channels > 0)
                {
                    selectedDevice = i;
                    break;
                }
            }

            var waveIn = new WaveInEvent
            {
                DeviceNumber = selectedDevice,
                WaveFormat = targetFormat,
                BufferMilliseconds = SystemConstants.FrameDurationMs
            };

            waveIn.DataAvailable += (sender, args) =>
            {
                if (args.BytesRecorded > 0)
                {
                    var buffer = new byte[args.BytesRecorded];
                    Array.Copy(args.Buffer, buffer, args.BytesRecorded);
                    OnAudioRecorded?.Invoke(buffer);
                }
            };

            _waveIn = waveIn;
            _waveIn.StartRecording();
            IsRecording = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StartRecording error: {ex.Message}");
        }
    }

    /// <summary>Chuẩn hóa audio thiết bị về PCM16 mono 16 kHz.</summary>
    private byte[] ResampleToPcm16k(byte[] inBuffer, int bytesRecorded, WaveFormat inFormat, WaveFormat targetFormat)
    {
        // WASAPI thường trả định dạng thiết bị (hay float/stereo/48 kHz); server cần PCM16 mono 16 kHz.
        try
        {
            if (inFormat.SampleRate == targetFormat.SampleRate &&
                inFormat.Channels == targetFormat.Channels &&
                inFormat.BitsPerSample == 16 &&
                inFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                var copy = new byte[bytesRecorded];
                Array.Copy(inBuffer, copy, bytesRecorded);
                return copy;
            }

            using var inMs = new MemoryStream(inBuffer, 0, bytesRecorded);
            IWaveProvider provider;

            if (inFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                provider = new RawSourceWaveStream(inMs, inFormat).ToSampleProvider().ToWaveProvider16();
            }
            else
            {
                provider = new RawSourceWaveStream(inMs, inFormat);
            }

            using var resampler = new MediaFoundationResampler(provider, targetFormat);
            using var outMs = new MemoryStream();
            var buf = new byte[4096];
            int read;
            while ((read = resampler.Read(buf, 0, buf.Length)) > 0)
            {
                outMs.Write(buf, 0, read);
            }
            return outMs.ToArray();
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    /// <summary>Dừng, dispose và nhả thiết bị micro hiện tại.</summary>
    public void StopRecording()
    {
        // Dispose thiết bị để Windows nhả micro và lần thu sau có thể khởi tạo sạch.
        if (!IsRecording || _waveIn == null) return;
        try
        {
            _waveIn.StopRecording();
            _waveIn.Dispose();
        }
        catch { }
        finally
        {
            _waveIn = null;
            IsRecording = false;
        }
    }

    /// <summary>Đưa PCM TTS vào buffer mà WaveOut đang phát nền.</summary>
    public void PlayAudio(byte[] pcmData)
    {
        // Chỉ thêm dữ liệu vào buffer; WaveOutEvent đã chạy nền và tự lấy mẫu để phát.
        if (_bufferedWaveProvider != null && pcmData.Length > 0)
        {
            _bufferedWaveProvider.AddSamples(pcmData, 0, pcmData.Length);
            IsPlaying = true;
        }
    }

    /// <summary>Xóa audio chưa phát để ngắt câu trả lời ngay.</summary>
    public void StopPlayback()
    {
        _bufferedWaveProvider?.ClearBuffer();
        IsPlaying = false;
    }

    /// <summary>Đổi phần trăm âm lượng sang khoảng 0..1 của NAudio.</summary>
    public void SetVolume(int volumePercent)
    {
        _volume = Math.Clamp(volumePercent / 100f, 0f, 1f);
        if (_waveOut != null)
        {
            _waveOut.Volume = _volume;
        }
    }

    /// <summary>Đóng cả pipeline thu và phát khi service hết vòng đời.</summary>
    public void Dispose()
    {
        StopRecording();
        _waveOut?.Stop();
        _waveOut?.Dispose();
    }
}
