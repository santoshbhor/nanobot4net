using Nanobot.CLI.Commands;

Console.WriteLine("🐈 nanobot - The Ultra-Lightweight Personal AI Agent");
Console.WriteLine();

if (args.Length == 0)
{
    ShowHelp();
    return;
}

var command = args[0].ToLowerInvariant();
var remainingArgs = args.Skip(1).ToArray();

switch (command)
{
    case "onboard":
        await OnboardCommand.ExecuteAsync(remainingArgs);
        break;
    case "agent":
        await AgentCommand.ExecuteAsync(remainingArgs);
        break;
    case "gateway":
        await GatewayCommand.ExecuteAsync(remainingArgs);
        break;
    case "version":
        VersionCommand.Execute();
        break;
    case "cron":
        await CronCommand.ExecuteAsync(remainingArgs);
        break;
    case "help" or "--help" or "-h":
        ShowHelp();
        break;
    default:
        Console.WriteLine($"Unknown command: {command}");
        ShowHelp();
        break;
}

static void ShowHelp()
{
    Console.WriteLine("Usage: nanobot <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  onboard    Initialize nanobot configuration");
    Console.WriteLine("  agent      Start interactive agent chat");
    Console.WriteLine("  gateway    Start the nanobot gateway");
    Console.WriteLine("  cron       Manage scheduled jobs");
    Console.WriteLine("  version    Show version information");
    Console.WriteLine("  help       Show this help message");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  nanobot onboard");
    Console.WriteLine("  nanobot agent");
    Console.WriteLine("  nanobot cron list");
    Console.WriteLine("  nanobot cron add myjob \"0 9 * * *\" \"shell:echo hello\"");
}