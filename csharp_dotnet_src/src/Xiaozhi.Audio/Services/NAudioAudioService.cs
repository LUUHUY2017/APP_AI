using System;
using NAudio.Wave;
using Xiaozhi.Core.Constants;
using Xiaozhi.Core.Interfaces;

namespace Xiaozhi.Audio.Services;

public class NAudioAudioService : IAudioService
{
    private WaveInEvent? _waveIn;
    private WaveOutEvent? _waveOut;
    private BufferedWaveProvider? _bufferedWaveProvider;
    private float _volume = 0.9f;

    public event Action<byte[]>? OnAudioRecorded;
    public bool IsRecording { get; private set; }
    public bool IsPlaying { get; private set; }

    public NAudioAudioService()
    {
        InitializePlayback();
    }

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

    public void StartRecording()
    {
        if (IsRecording) return;

        try
        {
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(SystemConstants.SampleRate, 16, SystemConstants.Channels),
                BufferMilliseconds = SystemConstants.FrameDurationMs
            };

            _waveIn.DataAvailable += (sender, args) =>
            {
                if (args.BytesRecorded > 0)
                {
                    var buffer = new byte[args.BytesRecorded];
                    Array.Copy(args.Buffer, buffer, args.BytesRecorded);
                    OnAudioRecorded?.Invoke(buffer);
                }
            };

            _waveIn.StartRecording();
            IsRecording = true;
        }
        catch { }
    }

    public void StopRecording()
    {
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

    public void PlayAudio(byte[] pcmData)
    {
        if (_bufferedWaveProvider != null && pcmData.Length > 0)
        {
            _bufferedWaveProvider.AddSamples(pcmData, 0, pcmData.Length);
            IsPlaying = true;
        }
    }

    public void StopPlayback()
    {
        _bufferedWaveProvider?.ClearBuffer();
        IsPlaying = false;
    }

    public void SetVolume(int volumePercent)
    {
        _volume = Math.Clamp(volumePercent / 100f, 0f, 1f);
        if (_waveOut != null)
        {
            _waveOut.Volume = _volume;
        }
    }

    public void Dispose()
    {
        StopRecording();
        _waveOut?.Stop();
        _waveOut?.Dispose();
    }
}
