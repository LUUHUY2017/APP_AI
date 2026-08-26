using System;
using System.Diagnostics;

namespace Xiaozhi.Audio.Services;

/// <summary>
/// Bộ phát hiện hoạt động giọng nói (Voice Activity Detector - VAD).
/// Tự động nhận diện khi người dùng bắt đầu nói và tự động gửi khi người dùng ngừng nói (im lặng > 1.2s).
/// </summary>
public class VoiceActivityDetector
{
    private double _energyThreshold = 300.0;
    private double _noiseFloor = 100.0;
    private readonly Stopwatch _silenceTimer = new();
    private readonly Stopwatch _speechTimer = new();
    private readonly Stopwatch _totalListeningTimer = new();
    private bool _hasSpoken = false;

    public event Action? OnSpeechStarted;
    public event Action? OnSpeechEnded;

    public bool IsSpeechActive => _hasSpoken;
    public double SilenceTimeoutMs { get; set; } = 800; // 800ms im lặng -> tự ngắt & gửi câu hỏi
    public double MaxListeningTimeoutMs { get; set; } = 8000; // 8s không nói gì -> tự ngắt

    public void Reset()
    {
        _silenceTimer.Reset();
        _speechTimer.Reset();
        _totalListeningTimer.Restart();
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

        // Tự động cập nhật noise floor khi không có giọng nói
        if (!_hasSpoken && rms < _energyThreshold)
        {
            _noiseFloor = _noiseFloor * 0.9 + rms * 0.1;
            _energyThreshold = Math.Max(250.0, _noiseFloor * 2.0);
        }

        if (rms > _energyThreshold)
        {
            _silenceTimer.Reset();

            if (!_hasSpoken)
            {
                if (!_speechTimer.IsRunning)
                    _speechTimer.Restart();

                if (_speechTimer.ElapsedMilliseconds > 60)
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

                // Ngừng nói > 800ms -> Tự động kích hoạt OnSpeechEnded để ngắt & gửi
                if (_silenceTimer.ElapsedMilliseconds >= SilenceTimeoutMs)
                {
                    _hasSpoken = false;
                    _silenceTimer.Reset();
                    OnSpeechEnded?.Invoke();
                }
            }
            else if (_totalListeningTimer.ElapsedMilliseconds >= MaxListeningTimeoutMs)
            {
                // Nếu sau 8s người dùng không nói gì -> Tự động ngắt
                _totalListeningTimer.Reset();
                OnSpeechEnded?.Invoke();
            }
        }
    }
}
