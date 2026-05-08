namespace Nanobot.Providers;

using Nanobot.Core.Models;

public class StepFunProvider : GenericHttpProvider
{
    public StepFunProvider(
        string? apiKey = null,
        string? model = null,
        Dictionary<string, object>? additionalProperties = null)
        : base(
            name: "stepfun",
            defaultModel: model ?? "step-1-8k",
            apiKey: apiKey,
            baseUrl: "https://api.stepfun.com/v1",
            endpoint: "/chat/completions",
            additionalProperties: additionalProperties)
    {
    }
}
