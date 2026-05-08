namespace Nanobot.Core.Interfaces;

public interface IChannel : IAsyncDisposable
{
    string Name { get; }
    bool IsEnabled { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    event Func<string, string, Task>? OnMessageReceived;
    event Func<string, Task>? OnReady;
}
