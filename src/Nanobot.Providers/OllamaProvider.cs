namespace Nanobot.Providers;

using Nanobot.Core.Models;

public class OllamaProvider : GenericHttpProvider
{
    public OllamaProvider(
        string? baseUrl = null,
        string? model = null,
        Dictionary<string, object>? additionalProperties = null)
        : base(
            name: "ollama",
            defaultModel: model ?? "llama3",
            apiKey: null,
            baseUrl: baseUrl ?? "http://localhost:11434",
            endpoint: "/api/chat",
            additionalProperties: additionalProperties)
    {
    }
}
