namespace Nanobot.CLI.Commands;

using Nanobot.Channels;

public static class GatewayCommand
{
    public static async Task ExecuteAsync(string[] args)
    {
        Console.WriteLine("🐈 Starting nanobot gateway...");
        
        await using var webSocketChannel = new WebSocketChannel();
        await webSocketChannel.StartAsync();

        Console.WriteLine("✅ Gateway started. Press Ctrl+C to stop.");
        
        var tcs = new TaskCompletionSource<bool>();
        Console.CancelKeyPress += (_, _) =>
        {
            Console.WriteLine("Shutting down...");
            tcs.TrySetResult(true);
        };

        await tcs.Task;
        await webSocketChannel.StopAsync();
    }
}
