namespace Nanobot.Channels;

using System.Text.Json;
using Nanobot.Core.Interfaces;

public class DiscordChannel : IChannel
{
    public string Name => "discord";
    public bool IsEnabled => !string.IsNullOrEmpty(_botToken);

    private readonly string _botToken;
    private readonly HttpClient _httpClient;
    private readonly CancellationTokenSource _cts = new();

    public event Func<string, string, Task>? OnMessageReceived;
    public event Func<string, Task>? OnReady;

    public DiscordChannel(string? botToken = null)
    {
        _botToken = botToken ?? Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN") ?? "";
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bot {_botToken}");
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_botToken))
        {
            Console.WriteLine("⚠️ Discord bot token not configured. Channel disabled.");
            return;
        }

        Console.WriteLine("💬 Starting Discord channel...");
        
        // Verify token
        var response = await _httpClient.GetStringAsync("https://discord.com/api/v10/users/@me");
        using var doc = JsonDocument.Parse(response);
        var username = doc.RootElement.GetProperty("username").GetString();
        
        Console.WriteLine($"✅ Connected to Discord as: {username}");
        
        if (OnReady != null)
        {
            await OnReady.Invoke(Name);
        }

        // Note: Full Discord gateway implementation requires WebSocket connection
        // This is a simplified version
        Console.WriteLine("⚠️ Discord channel requires gateway WebSocket connection for full functionality");
    }

    public async Task SendMessageAsync(string channelId, string text)
    {
        var url = $"https://discord.com/api/v10/channels/{channelId}/messages";
        var content = new StringContent(
            JsonSerializer.Serialize(new { content = text }),
            System.Text.Encoding.UTF8,
            "application/json");
        
        await _httpClient.PostAsync(url, content);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _cts.Cancel();
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _httpClient.Dispose();
        await Task.CompletedTask;
    }
}
