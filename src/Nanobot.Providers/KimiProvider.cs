namespace Nanobot.Providers;

using Nanobot.Core.Models;

public class KimiProvider : GenericHttpProvider
{
    public KimiProvider(
        string? apiKey = null,
        string? model = null,
        Dictionary<string, object>? additionalProperties = null)
        : base(
            name: "kimi",
            defaultModel: model ?? "moonshot-v1-8k",
            apiKey: apiKey,
            baseUrl: "https://api.moonshot.cn/v1",
            endpoint: "/chat/completions",
            additionalProperties: additionalProperties)
    {
    }
}
