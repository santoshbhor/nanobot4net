namespace Nanobot.Providers;

using Nanobot.Core.Interfaces;
using Nanobot.Core.Models;

public static class ProviderFactory
{
    public static IProvider CreateProvider(string name, ProviderConfig config)
    {
        var apiKey = config.ApiKey;
        var baseUrl = config.BaseUrl;
        var additionalProps = config.AdditionalProperties;

        return name.ToLowerInvariant() switch
        {
            "openai" => new OpenAiProvider(apiKey, baseUrl, null, additionalProps),
            "anthropic" => new AnthropicProvider(apiKey, baseUrl, null, additionalProps),
            "openrouter" => new OpenRouterProvider(apiKey, null, additionalProps),
            "deepseek" => new DeepSeekProvider(apiKey, baseUrl, null, additionalProps),
            "azure" => new AzureOpenAiProvider(apiKey, baseUrl, null, additionalProps),
            "qwen" or "dashscope" => new QwenProvider(apiKey, null, additionalProps),
            "kimi" or "moonshot" => new KimiProvider(apiKey, null, additionalProps),
            "mistral" => new MistralProvider(apiKey, null, additionalProps),
            "ollama" => new OllamaProvider(baseUrl, null, additionalProps),
            "volcengine" or "volc" => new VolcEngineProvider(apiKey, null, additionalProps),
            "stepfun" => new StepFunProvider(apiKey, null, additionalProps),
            _ => new GenericHttpProvider(name, "default-model", apiKey, baseUrl, "/v1/chat/completions", additionalProps)
        };
    }

    public static IProvider? CreateProviderFromConfig(
        NanobotConfig config, 
        string? providerName = null)
    {
        var providers = config.Providers;
        if (providers.Count == 0) return null;

        var name = providerName ?? config.Agents.Defaults.Provider ?? providers.Keys.First();
        
        if (!providers.TryGetValue(name, out var providerConfig))
        {
            return null;
        }

        return CreateProvider(name, providerConfig);
    }
}
