using Nanobot.Core.Interfaces;
using Nanobot.Core.Models;
using Nanobot.Core.Services;
using Nanobot.Providers;
using Nanobot.Api.Models;

namespace Nanobot.Api.Services;

public class ApiService
{
    private readonly IConfigurationService _configService;
    private readonly IProvider? _provider;
    private readonly Dictionary<string, IAgent> _sessions = new();
    private readonly object _lock = new();
    private readonly Dictionary<string, ToolDefinition> _tools = new();

    public ApiService(IConfigurationService configService)
    {
        _configService = configService;
        _provider = ProviderFactory.CreateProviderFromConfig(_configService.Config);
        
        // Load tools
        LoadTools();
    }

    private void LoadTools()
    {
        // Add built-in tools
        _tools["execute_shell"] = new ToolDefinition
        {
            Name = "execute_shell",
            Description = "Execute a shell command and return the output",
            InputSchema = new
            {
                type = "object",
                properties = new { command = new { type = "string", description = "The command to execute" } },
                required = new[] { "command" }
            }
        };
        
        _tools["read_file"] = new ToolDefinition
        {
            Name = "read_file",
            Description = "Read contents of a file",
            InputSchema = new
            {
                type = "object",
                properties = new { path = new { type = "string", description = "Path to the file" } },
                required = new[] { "path" }
            }
        };
        
        _tools["write_file"] = new ToolDefinition
        {
            Name = "write_file",
            Description = "Write content to a file",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    path = new { type = "string", description = "Path to the file" },
                    content = new { type = "string", description = "Content to write" }
                },
                required = new[] { "path", "content" }
            }
        };
        
        _tools["list_files"] = new ToolDefinition
        {
            Name = "list_files",
            Description = "List files in a directory",
            InputSchema = new
            {
                type = "object",
                properties = new { path = new { type = "string", description = "Directory path" } }
            }
        };
        
        _tools["web_search"] = new ToolDefinition
        {
            Name = "web_search",
            Description = "Search the web for information",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    query = new { type = "string", description = "Search query" },
                    count = new { type = "integer", description = "Number of results" }
                },
                required = new[] { "query" }
            }
        };
    }

    public async Task EnsureInitializedAsync()
    {
        await _configService.LoadAsync();
    }

    public IAgent GetOrCreateSession(string sessionId)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var agent) && _provider != null)
            {
                var defaults = _configService.Config.Agents.Defaults;
                agent = new Nanobot.Agent.Agent(_provider, defaults);
                _sessions[sessionId] = agent;
            }
            return agent!;
        }
    }

    public async Task<ChatCompletionResponse> CreateCompletionAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
    {
        var sessionId = request.User ?? "default";
        var agent = GetOrCreateSession(sessionId);
        
        // Convert OpenAI messages to nanobot messages
        var messages = request.Messages.Select(m => new Message
        {
            Role = m.Role,
            Content = m.Content
        }).ToList();
        
        // Get tools if provided
        IEnumerable<ToolDefinition>? tools = null;
        if (request.Tools?.Count > 0)
        {
            tools = request.Tools.Select(t => new ToolDefinition
            {
                Name = t.Function.Name,
                Description = t.Function.Description,
                InputSchema = t.Function.Parameters
            });
        }
        
        // Make the call
        var response = await agent.ProcessMessageAsync(
            request.Messages.LastOrDefault()?.Content ?? "",
            cancellationToken
        );
        
        return new ChatCompletionResponse
        {
            Id = $"chatcmpl-{Guid.NewGuid():N}".Substring(0, 24),
            Object = "chat.completion",
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = request.Model,
            Choices = new List<ChatChoice>
            {
                new ChatChoice
                {
                    Index = 0,
                    Message = new ChatMessage
                    {
                        Role = "assistant",
                        Content = response.Content
                    },
                    FinishReason = "stop"
                }
            },
            Usage = new Usage
            {
                PromptTokens = messages.Sum(m => m.Content.Length / 4),
                CompletionTokens = response.Content.Length / 4,
                TotalTokens = (messages.Sum(m => m.Content.Length) + response.Content.Length) / 4
            }
        };
    }

    public async IAsyncEnumerable<ChatCompletionStreamResponse> CreateCompletionStreamAsync(
        ChatCompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sessionId = request.User ?? "default";
        var agent = GetOrCreateSession(sessionId);
        
        // Get the last user message
        var userMessage = request.Messages.LastOrDefault(m => m.Role == "user")?.Content ?? "";
        
        // First chunk - role
        yield return CreateStreamChunk(request.Model, new ChatMessageDelta { Role = "assistant" }, 0);
        
        // Simulate streaming by yielding chunks of the response
        var response = await agent.ProcessMessageAsync(userMessage, cancellationToken);
        
        // Yield content chunks
        var words = response.Content.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            yield return CreateStreamChunk(request.Model, new ChatMessageDelta 
            { 
                Content = words[i] + (i < words.Length - 1 ? " " : "") 
            }, 0);
            
            await Task.Delay(20, cancellationToken); // Simulate typing delay
        }
        
        // Final chunk - finish reason
        yield return CreateStreamChunk(request.Model, null, 0, "stop");
    }

    private static ChatCompletionStreamResponse CreateStreamChunk(
        string model, 
        ChatMessageDelta? delta, 
        int index,
        string? finishReason = null)
    {
        return new ChatCompletionStreamResponse
        {
            Id = $"chatcmpl-{Guid.NewGuid():N}".Substring(0, 24),
            Object = "chat.completion.chunk",
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = model,
            Choices = new List<ChatStreamChoice>
            {
                new ChatStreamChoice
                {
                    Index = index,
                    Delta = delta,
                    FinishReason = finishReason ?? ""
                }
            }
        };
    }

    public ModelsResponse GetModels()
    {
        var defaults = _configService.Config.Agents.Defaults;
        
        return new ModelsResponse
        {
            Object = "list",
            Data = new List<ModelData>
            {
                new ModelData
                {
                    Id = defaults.Model ?? "gpt-3.5-turbo",
                    Object = "model",
                    Created = 1677610602,
                    OwnedBy = "nanobot"
                },
                new ModelData
                {
                    Id = "gpt-4",
                    Object = "model",
                    Created = 1687882411,
                    OwnedBy = "openai"
                },
                new ModelData
                {
                    Id = "claude-opus-4",
                    Object = "model",
                    Created = 1709596800,
                    OwnedBy = "anthropic"
                },
                new ModelData
                {
                    Id = "llama3",
                    Object = "model",
                    Created = 1712332800,
                    OwnedBy = "meta"
                }
            }
        };
    }

    public async Task<EmbeddingsResponse> CreateEmbeddingAsync(EmbeddingsRequest request, CancellationToken cancellationToken = default)
    {
        // For now, return a simple embedding
        // In production, you'd integrate with a model that supports embeddings
        var text = request.Input ?? string.Join("\n", request.InputArray ?? new List<string>());
        var embedding = GenerateSimpleEmbedding(text);
        
        return new EmbeddingsResponse
        {
            Object = "list",
            Model = request.Model,
            Data = new List<EmbeddingData>
            {
                new EmbeddingData
                {
                    Index = 0,
                    Object = "embedding",
                    Embedding = embedding
                }
            },
            Usage = new Usage
            {
                PromptTokens = text.Length / 4,
                CompletionTokens = 0,
                TotalTokens = text.Length / 4
            }
        };
    }

    private static List<float> GenerateSimpleEmbedding(string text)
    {
        // Simple hash-based embedding for demo purposes
        // In production, use actual embedding models
        var random = new Random(text.GetHashCode());
        var embedding = new List<float>();
        for (int i = 0; i < 1536; i++)
        {
            embedding.Add((float)(random.NextDouble() * 2 - 1));
        }
        
        // Normalize
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        return embedding.Select(x => (float)(x / magnitude)).ToList();
    }
}