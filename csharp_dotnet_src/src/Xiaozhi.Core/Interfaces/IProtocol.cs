using System;
using System.Threading;
using System.Threading.Tasks;

namespace Xiaozhi.Core.Interfaces;

public interface IProtocol : IAsyncDisposable
{
    event Func<byte[], Task>? OnIncomingAudio;
    event Func<string, Task>? OnIncomingText;
    event Func<Task>? OnConnected;
    event Func<string, Task>? OnDisconnected;
    event Func<Exception, Task>? OnError;

    bool IsConnected { get; }
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    Task SendAudioAsync(byte[] opusData);
    Task SendTextAsync(string text);
    Task SendJsonAsync(object data);
}
