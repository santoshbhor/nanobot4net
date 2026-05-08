namespace Nanobot.Providers;

using System.Text;
using System.Text.Json;
using Nanobot.Core.Interfaces;
using Nanobot.Core.Models;

public class GenericHttpProvider : BaseProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly string _endpoint;

    public override string Name { get; }
    public override string DefaultModel { get; }

    public GenericHttpProvider(
        string name,
        string defaultModel,
        string? apiKey = null,
        string? baseUrl = null,
        string? endpoint = null,
        Dictionary<string, object>? additionalProperties = null,
        HttpClient? httpClient = null)
        : base(apiKey, baseUrl, additionalProperties)
    {
        Name = name;
        DefaultModel = defaultModel;
        _model = defaultModel;
        _endpoint = endpoint ?? "/v1/chat/completions";
        _httpClient = httpClient ?? new HttpClient();
        
        if (!string.IsNullOrEmpty(baseUrl))
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
        }
        
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
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
            messages = messages.Select(m => new
            {
                role = m.Role,
                content = m.Content
            }).ToArray(),
            temperature = temperature ?? 0.7,
            max_tokens = maxTokens ?? 4096,
            tools = tools?.Select(t => new
            {
                type = "function",
                function = new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = t.InputSchema
                }
            }).ToArray()
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_endpoint, content, cancellationToken);
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
