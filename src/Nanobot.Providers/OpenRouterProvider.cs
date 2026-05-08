namespace Nanobot.Providers;

using Nanobot.Core.Models;

public class OpenRouterProvider : GenericHttpProvider
{
    public OpenRouterProvider(
        string? apiKey = null,
        string? model = null,
        Dictionary<string, object>? additionalProperties = null)
        : base(
            name: "openrouter",
            defaultModel: model ?? "anthropic/claude-opus-4",
            apiKey: apiKey,
            baseUrl: "https://openrouter.ai/api/v1",
            endpoint: "/chat/completions",
            additionalProperties: additionalProperties)
    {
    }
}
