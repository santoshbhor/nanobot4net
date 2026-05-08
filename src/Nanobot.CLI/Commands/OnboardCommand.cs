namespace Nanobot.CLI.Commands;

using Nanobot.Core.Services;
using Nanobot.Core.Models;

public static class OnboardCommand
{
    public static async Task ExecuteAsync(string[] args)
    {
        Console.WriteLine("🐈 Initializing nanobot...");
        
        var configService = new ConfigurationService();
        await configService.LoadAsync();

        Console.WriteLine($"Configuration will be saved to: {configService.ConfigPath}");
        Console.WriteLine();
        Console.WriteLine("Please configure your LLM provider:");
        Console.Write("Provider (openai/anthropic/openrouter/deepseek): ");
        var provider = Console.ReadLine()?.Trim().ToLowerInvariant() ?? "openai";

        Console.Write("API Key: ");
        var apiKey = Console.ReadLine()?.Trim() ?? "";

        Console.Write("Model (leave empty for default): ");
        var model = Console.ReadLine()?.Trim();

        var config = new NanobotConfig();
        config.Providers[provider] = new ProviderConfig
        {
            ApiKey = apiKey
        };

        config.Agents.Defaults = new AgentDefaults
        {
            Provider = provider,
            Model = string.IsNullOrEmpty(model) ? null : model
        };

        // Save config
        var configService2 = new ConfigurationService();
        await configService2.LoadAsync();
        
        Console.WriteLine();
        Console.WriteLine("✅ Configuration saved!");
        Console.WriteLine($"You can now start chatting with: nanobot agent");
    }
}
