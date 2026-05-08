namespace Nanobot.CLI.Commands;

using Nanobot.Core.Services;
using Nanobot.Core.Interfaces;
using Nanobot.Providers;
using Nanobot.Agent;

public static class AgentCommand
{
    public static async Task ExecuteAsync(string[] args)
    {
        Console.WriteLine("🐈 Starting nanobot agent...");
        Console.WriteLine("Type 'exit' or 'quit' to stop.");
        Console.WriteLine();

        var configService = new ConfigurationService();
        await configService.LoadAsync();

        var provider = ProviderFactory.CreateProviderFromConfig(configService.Config);
        if (provider == null)
        {
            Console.WriteLine("❌ No provider configured. Please run 'nanobot onboard' first.");
            return;
        }

        await using var agent = new Agent(provider, configService.Config.Agents.Defaults);

        while (true)
        {
            Console.Write("You: ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) || 
                input.Equals("exit", StringComparison.OrdinalIgnoreCase) || 
                input.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            try
            {
                var response = await agent.ProcessMessageAsync(input);
                Console.WriteLine($"🐈: {response.Content}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        Console.WriteLine("Goodbye!");
    }
}
