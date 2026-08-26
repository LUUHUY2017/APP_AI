using System;
using System.Diagnostics;

namespace Xiaozhi.Audio.Services;

/// <summary>
/// Bộ phát hiện hoạt động giọng nói (Voice Activity Detector - VAD).
/// Tự động nhận diện khi người dùng bắt đầu nói và tự động gửi khi người dùng ngừng nói (im lặng > 1.2s).
/// </summary>
public class VoiceActivityDetector
{
    private double _energyThreshold = 600.0;
    private readonly Stopwatch _silenceTimer = new();
    private readonly Stopwatch _speechTimer = new();
    private bool _hasSpoken = false;

    public event Action? OnSpeechStarted;
    public event Action? OnSpeechEnded;

    public bool IsSpeechActive => _hasSpoken;
    public double SilenceTimeoutMs { get; set; } = 1200;

    public void Reset()
    {
        _silenceTimer.Reset();
        _speechTimer.Reset();
        _hasSpoken = false;
    }

    public void ProcessPcm(byte[] pcmData)
    {
        if (pcmData.Length < 2) return;

        double sum = 0;
        int sampleCount = pcmData.Length / 2;
        for (int i = 0; i < pcmData.Length; i += 2)
        {
            short sample = BitConverter.ToInt16(pcmData, i);
            sum += (double)sample * sample;
        }

        double rms = Math.Sqrt(sum / sampleCount);

        if (rms > _energyThreshold)
        {
            _silenceTimer.Reset();

            if (!_hasSpoken)
            {
                if (!_speechTimer.IsRunning)
                    _speechTimer.Restart();

                if (_speechTimer.ElapsedMilliseconds > 150)
                {
                    _hasSpoken = true;
                    OnSpeechStarted?.Invoke();
                }
            }
        }
        else
        {
            _speechTimer.Reset();

            if (_hasSpoken)
            {
                if (!_silenceTimer.IsRunning)
                    _silenceTimer.Restart();

                if (_silenceTimer.ElapsedMilliseconds >= SilenceTimeoutMs)
                {
                    _hasSpoken = false;
                    _silenceTimer.Reset();
                    OnSpeechEnded?.Invoke();
                }
            }
        }
    }
}
