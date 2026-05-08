namespace Nanobot.Providers;

using Nanobot.Core.Models;

public class MistralProvider : GenericHttpProvider
{
    public MistralProvider(
        string? apiKey = null,
        string? model = null,
        Dictionary<string, object>? additionalProperties = null)
        : base(
            name: "mistral",
            defaultModel: model ?? "mistral-large-latest",
            apiKey: apiKey,
            baseUrl: "https://api.mistral.ai/v1",
            endpoint: "/chat/completions",
            additionalProperties: additionalProperties)
    {
    }
}
