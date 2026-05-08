namespace Nanobot.Providers;

using Nanobot.Core.Models;

public class QwenProvider : GenericHttpProvider
{
    public QwenProvider(
        string? apiKey = null,
        string? model = null,
        Dictionary<string, object>? additionalProperties = null)
        : base(
            name: "qwen",
            defaultModel: model ?? "qwen-turbo",
            apiKey: apiKey,
            baseUrl: "https://dashscope.aliyuncs.com/compatible-mode/v1",
            endpoint: "/chat/completions",
            additionalProperties: additionalProperties)
    {
    }
}
