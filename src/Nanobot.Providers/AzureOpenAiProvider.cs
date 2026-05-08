namespace Nanobot.Providers;

using System.Text;
using System.Text.Json;
using Nanobot.Core.Interfaces;
using Nanobot.Core.Models;

public class AzureOpenAiProvider : BaseProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly string _endpoint;

    public override string Name => "azure";
    public override string DefaultModel => "gpt-4";

    public AzureOpenAiProvider(
        string? apiKey = null,
        string? endpoint = null,
        string? model = null,
        Dictionary<string, object>? additionalProperties = null)
        : base(apiKey, endpoint, additionalProperties)
    {
        _model = model ?? DefaultModel;
        _endpoint = endpoint ?? "";
        _httpClient = new HttpClient();
        
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("api-key", apiKey);
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
            messages = messages.Select(m => new
            {
                role = m.Role,
                content = m.Content
            }).ToArray(),
            temperature = temperature ?? 0.7,
            max_tokens = maxTokens ?? 4096
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"{_endpoint?.TrimEnd('/')}/openai/deployments/{model ?? _model}/chat/completions?api-version=2024-02-15-preview";
        
        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        var contentText = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

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
