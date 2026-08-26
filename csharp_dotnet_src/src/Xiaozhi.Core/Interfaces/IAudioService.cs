using System;
using System.Threading.Tasks;

namespace Xiaozhi.Core.Interfaces;

public interface IAudioService : IDisposable
{
    event Action<byte[]>? OnAudioRecorded; // Raw PCM 16kHz
    void StartRecording();
    void StopRecording();
    bool IsRecording { get; }

    void PlayAudio(byte[] pcmData);
    void StopPlayback();
    bool IsPlaying { get; }
    
    void SetVolume(int volumePercent);
}
