namespace Nanobot.Channels;

using System.Text.Json;
using Nanobot.Core.Interfaces;

public class TelegramChannel : IChannel
{
    public string Name => "telegram";
    public bool IsEnabled => !string.IsNullOrEmpty(_botToken);

    private readonly string _botToken;
    private readonly HttpClient _httpClient;
    private readonly CancellationTokenSource _cts = new();
    private string _baseUrl => $"https://api.telegram.org/bot{_botToken}";

    public event Func<string, string, Task>? OnMessageReceived;
    public event Func<string, Task>? OnReady;

    public TelegramChannel(string? botToken = null)
    {
        _botToken = botToken ?? Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN") ?? "";
        _httpClient = new HttpClient();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_botToken))
        {
            Console.WriteLine("⚠️ Telegram bot token not configured. Channel disabled.");
            return;
        }

        Console.WriteLine("📱 Starting Telegram channel...");
        
        if (OnReady != null)
        {
            await OnReady.Invoke(Name);
        }

        _ = Task.Run(() => PollUpdatesAsync(_cts.Token));
    }

    private async Task PollUpdatesAsync(CancellationToken cancellationToken)
    {
        var offset = 0L;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var url = $"{_baseUrl}/getUpdates?offset={offset}&timeout=30";
                var response = await _httpClient.GetStringAsync(url, cancellationToken);
                
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;
                
                if (root.GetProperty("ok").GetBoolean())
                {
                    var updates = root.GetProperty("result").EnumerateArray();
                    
                    foreach (var update in updates)
                    {
                        offset = update.GetProperty("update_id").GetInt64() + 1;
                        
                        if (update.TryGetProperty("message", out var messageElement))
                        {
                            var chatId = messageElement.GetProperty("chat").GetProperty("id").GetInt64();
                            var text = messageElement.GetProperty("text").GetString() ?? "";
                            
                            if (OnMessageReceived != null)
                            {
                                await OnMessageReceived.Invoke(Name, $"{chatId}:{text}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"Telegram polling error: {ex.Message}");
                await Task.Delay(5000, cancellationToken);
            }
        }
    }

    public async Task SendMessageAsync(long chatId, string text)
    {
        var url = $"{_baseUrl}/sendMessage";
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("chat_id", chatId.ToString()),
            new KeyValuePair<string, string>("text", text),
            new KeyValuePair<string, string>("parse_mode", "Markdown")
        });
        
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
