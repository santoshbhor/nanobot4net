namespace Nanobot.Providers;

using System.Text;
using System.Text.Json;
using Nanobot.Core.Interfaces;
using Nanobot.Core.Models;

public class AnthropicProvider : BaseProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly string _apiUrl;

    public override string Name => "anthropic";
    public override string DefaultModel => "claude-opus-4-20240513";

    public AnthropicProvider(
        string? apiKey = null,
        string? baseUrl = null,
        string? model = null,
        Dictionary<string, object>? additionalProperties = null)
        : base(apiKey, baseUrl, additionalProperties)
    {
        _model = model ?? DefaultModel;
        _apiUrl = baseUrl ?? "https://api.anthropic.com/v1/messages";
        _httpClient = new HttpClient();
        
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }
    }

    public override async Task<Message> CompleteAsync(
        IReadOnlyList<Message> messages,
        IEnumerable<ToolDefinition>? tools = null,
        string? model = null,
        double? temperature = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = model ?? _model,
            max_tokens = maxTokens ?? 4096,
            temperature = temperature ?? 0.7,
            messages = messages.Where(m => m.Role != "system").Select(m => new
            {
                role = m.Role == "assistant" ? "assistant" : "user",
                content = m.Content
            }).ToArray(),
            system = messages.FirstOrDefault(m => m.Role == "system")?.Content
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_apiUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        var contentText = "";
        if (doc.RootElement.TryGetProperty("content", out var contentArray))
        {
            foreach (var item in contentArray.EnumerateArray())
            {
                if (item.TryGetProperty("type", out var type) && 
                    type.GetString() == "text" &&
                    item.TryGetProperty("text", out var text))
                {
                    contentText += text.GetString();
                }
            }
        }

        return new Message
        {
            Role = "assistant",
            Content = contentText
        };
    }

    public override ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        return ValueTask.CompletedTask;
    }
}
