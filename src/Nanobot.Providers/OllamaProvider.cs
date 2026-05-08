using System.Text;
using System.Text.Json;
using Nanobot.Core.Interfaces;
using Nanobot.Core.Models;

namespace Nanobot.Providers;

public class OllamaProvider : BaseProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private string _currentModel;
    private List<OllamaModel>? _cachedModels;

    public override string Name => "ollama";
    public override string DefaultModel => "llama3";

    public OllamaProvider(
        string? baseUrl = null,
        string? model = null,
        Dictionary<string, object>? additionalProperties = null)
        : base(null, baseUrl, additionalProperties)
    {
        _baseUrl = baseUrl ?? "http://localhost:11434";
        _currentModel = model ?? DefaultModel;
        _httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };
        _httpClient.Timeout = TimeSpan.FromSeconds(120); // Longer timeout for local models
    }

    public async Task<List<OllamaModel>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            
            var models = new List<OllamaModel>();
            if (doc.RootElement.TryGetProperty("models", out var modelsArray))
            {
                foreach (var model in modelsArray.EnumerateArray())
                {
                    models.Add(new OllamaModel
                    {
                        Name = model.GetProperty("name").GetString() ?? "",
                        Size = model.TryGetProperty("size", out var size) ? size.GetInt64() : 0,
                        ModifiedAt = model.TryGetProperty("modified_at", out var modified) 
                            ? modified.GetString() ?? "" : "",
                    });
                }
            }
            _cachedModels = models;
            return models;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error listing Ollama models: {ex.Message}");
            return new List<OllamaModel>();
        }
    }

    public async Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void SetModel(string modelName)
    {
        _currentModel = modelName;
    }

    public override async Task<Message> CompleteAsync(
        IReadOnlyList<Message> messages,
        IEnumerable<ToolDefinition>? tools = null,
        string? model = null,
        double? temperature = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        var useModel = model ?? _currentModel;
        
        var requestBody = new
        {
            model = useModel,
            messages = messages.Select(m => new
            {
                role = m.Role,
                content = m.Content
            }).ToArray(),
            stream = false,
            options = new
            {
                temperature = temperature ?? 0.7,
                num_predict = maxTokens ?? 4096
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/chat", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        var contentText = "";
        if (doc.RootElement.TryGetProperty("message", out var message) &&
            message.TryGetProperty("content", out var messageContent))
        {
            contentText = messageContent.GetString() ?? "";
        }

        // Check for tool calls in the response
        List<ToolCall>? toolCalls = null;
        if (doc.RootElement.TryGetProperty("tool_calls", out var toolCallsArray))
        {
            toolCalls = new List<ToolCall>();
            foreach (var tc in toolCallsArray.EnumerateArray())
            {
                string fnName = "";
                string fnArgs = "{}";
                
                if (tc.TryGetProperty("function", out var fnElement))
                {
                    if (fnElement.TryGetProperty("name", out var nameProp))
                        fnName = nameProp.GetString() ?? "";
                    if (fnElement.TryGetProperty("arguments", out var argsProp))
                        fnArgs = argsProp.GetRawText();
                }
                
                var toolId = tc.TryGetProperty("id", out var id) ? id.GetString() ?? "" : Guid.NewGuid().ToString();
                
                toolCalls.Add(new ToolCall
                {
                    Id = toolId,
                    Type = "function",
                    Function = new FunctionCall
                    {
                        Name = fnName,
                        Arguments = fnArgs
                    }
                });
            }
        }

        return new Message
        {
            Role = "assistant",
            Content = contentText,
            ToolCalls = toolCalls
        };
    }

    // Streaming support for real-time responses
    public async IAsyncEnumerable<Message> StreamCompleteAsync(
        IReadOnlyList<Message> messages,
        string? model = null,
        double? temperature = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var useModel = model ?? _currentModel;
        
        var requestBody = new
        {
            model = useModel,
            messages = messages.Select(m => new
            {
                role = m.Role,
                content = m.Content
            }).ToArray(),
            stream = true
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/chat", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;
            if (string.IsNullOrEmpty(line)) continue;
            
            Message? yieldMessage = null;
            
            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var messageContent))
                {
                    var chunk = messageContent.GetString() ?? "";
                    
                    yieldMessage = new Message
                    {
                        Role = "assistant",
                        Content = chunk
                    };
                }
                
                // Check for done
                if (doc.RootElement.TryGetProperty("done", out var done) && done.GetBoolean())
                {
                    break;
                }
            }
            catch
            {
                // Skip malformed lines
            }

            if (yieldMessage != null)
            {
                yield return yieldMessage;
            }
        }
    }

    // Generate embeddings for vector storage
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = _currentModel,
            prompt = text
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/embeddings", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        if (doc.RootElement.TryGetProperty("embedding", out var embeddingArray))
        {
            return embeddingArray.EnumerateArray()
                .Select(x => (float)x.GetDouble())
                .ToArray();
        }

        return Array.Empty<float>();
    }

    public override ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        return ValueTask.CompletedTask;
    }
}

public record OllamaModel
{
    public string Name { get; init; } = string.Empty;
    public long Size { get; init; }
    public string ModifiedAt { get; init; } = string.Empty;
    
    public string SizeFormatted => Size switch
    {
        < 1024 => $"{Size} B",
        < 1024 * 1024 => $"{Size / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{Size / (1024.0 * 1024):F1} MB",
        _ => $"{Size / (1024.0 * 1024 * 1024):F1} GB"
    };
}

public static class OllamaExtensions
{
    public static OllamaProvider WithDefaultModel(this OllamaProvider provider, string model)
    {
        provider.SetModel(model);
        return provider;
    }
}