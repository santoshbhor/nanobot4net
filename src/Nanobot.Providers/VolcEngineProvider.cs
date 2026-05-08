namespace Nanobot.Providers;

using Nanobot.Core.Models;

public class VolcEngineProvider : GenericHttpProvider
{
    public VolcEngineProvider(
        string? apiKey = null,
        string? model = null,
        Dictionary<string, object>? additionalProperties = null)
        : base(
            name: "volcengine",
            defaultModel: model ?? "doubao-lite-4k",
            apiKey: apiKey,
            baseUrl: "https://ark.cn-beijing.volces.com/api/v3",
            endpoint: "/chat/completions",
            additionalProperties: additionalProperties)
    {
    }
}
