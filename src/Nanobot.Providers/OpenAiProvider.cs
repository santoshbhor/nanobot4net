namespace Nanobot.Providers;

using System.Text;
using System.Text.Json;
using Nanobot.Core.Interfaces;
using Nanobot.Core.Models;

public class OpenAiProvider : GenericHttpProvider
{
    public override string Name => "openai";
    public override string DefaultModel => "gpt-4-turbo-preview";

    public OpenAiProvider(
        string? apiKey = null,
        string? baseUrl = null,
        string? model = null,
        Dictionary<string, object>? additionalProperties = null)
        : base(
            name: "openai",
            defaultModel: model ?? "gpt-4-turbo-preview",
            apiKey: apiKey,
            baseUrl: baseUrl ?? "https://api.openai.com/v1",
            endpoint: "/chat/completions",
            additionalProperties: additionalProperties)
    {
    }
}
