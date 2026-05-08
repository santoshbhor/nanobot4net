namespace Nanobot.Providers;

using Nanobot.Core.Models;

public class DeepSeekProvider : GenericHttpProvider
{
    public DeepSeekProvider(
        string? apiKey = null,
        string? baseUrl = null,
        string? model = null,
        Dictionary<string, object>? additionalProperties = null)
        : base(
            name: "deepseek",
            defaultModel: model ?? "deepseek-chat",
            apiKey: apiKey,
            baseUrl: baseUrl ?? "https://api.deepseek.com",
            endpoint: "/v1/chat/completions",
            additionalProperties: additionalProperties)
    {
    }
}
