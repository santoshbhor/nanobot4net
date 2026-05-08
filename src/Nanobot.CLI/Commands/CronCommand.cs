namespace Nanobot.CLI.Commands;

using Nanobot.Core.Services;

public static class CronCommand
{
    public static async Task ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return;
        }

        var subcommand = args[0].ToLowerInvariant();
        var remaining = args.Skip(1).ToArray();

        await using var cronService = new CronService();
        await cronService.LoadAsync(); // Wait for services to initialize

        switch (subcommand)
        {
            case "list":
                await ListJobsAsync(cronService);
                break;
            case "add":
                await AddJobAsync(cronService, remaining);
                break;
            case "remove":
            case "delete":
                await RemoveJobAsync(cronService, remaining);
                break;
            case "run":
                await RunJobAsync(cronService, remaining);
                break;
            case "start":
                await cronService.StartAsync();
                Console.WriteLine("✅ Cron service started");
                break;
            case "stop":
                await cronService.StopAsync();
                Console.WriteLine("✅ Cron service stopped");
                break;
            default:
                Console.WriteLine($"Unknown subcommand: {subcommand}");
                ShowHelp();
                break;
        }
    }

    private static async Task ListJobsAsync(CronService cronService)
    {
        var jobs = await cronService.ListJobsAsync();
        
        if (jobs.Count == 0)
        {
            Console.WriteLine("No cron jobs configured.");
            return;
        }

        Console.WriteLine("📅 Cron Jobs:");
        Console.WriteLine(new string('-', 80));
        
        foreach (var job in jobs)
        {
            var status = job.Enabled ? "✅" : "❌";
            var nextRun = job.NextRun?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "N/A";
            var lastRun = job.LastRun?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "Never";
            
            Console.WriteLine($"{status} {job.Name}");
            Console.WriteLine($"   Schedule: {job.Schedule}");
            Console.WriteLine($"   Action: {job.Action}");
            Console.WriteLine($"   Next run: {nextRun}");
            Console.WriteLine($"   Last run: {lastRun}");
            if (!string.IsNullOrEmpty(job.Description))
                Console.WriteLine($"   Description: {job.Description}");
            Console.WriteLine();
        }
    }

    private static async Task AddJobAsync(CronService cronService, string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: cron add <name> <schedule> <action> [description]");
            Console.WriteLine("Example: cron add daily-report \"0 9 * * *\" \"shell:python report.py\" \"Daily report generation\"");
            Console.WriteLine();
            Console.WriteLine("Schedule formats:");
            Console.WriteLine("  @hourly     - Every hour");
            Console.WriteLine("  @daily      - Every day at midnight");
            Console.WriteLine("  @weekly     - Every week");
            Console.WriteLine("  @monthly    - Every month");
            Console.WriteLine("  * * * * *   - minute hour day month weekday");
            return;
        }

        var name = args[0];
        var schedule = args[1];
        var action = args[2];
        var description = args.Length > 3 ? string.Join(" ", args.Skip(3)) : null;

        await cronService.AddJobAsync(name, schedule, action, description);
    }

    private static async Task RemoveJobAsync(CronService cronService, string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: cron remove <name>");
            return;
        }

        var name = args[0];
        await cronService.RemoveJobAsync(name);
    }

    private static async Task RunJobAsync(CronService cronService, string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: cron run <name>");
            return;
        }

        var name = args[0];
        await cronService.RunNowAsync(name);
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Cron Job Management");
        Console.WriteLine();
        Console.WriteLine("Usage: nanobot cron <subcommand> [options]");
        Console.WriteLine();
        Console.WriteLine("Subcommands:");
        Console.WriteLine("  list                List all cron jobs");
        Console.WriteLine("  add <name> <schedule> <action> [description]  Add a new cron job");
        Console.WriteLine("  remove <name>       Remove a cron job");
        Console.WriteLine("  run <name>          Run a cron job immediately");
        Console.WriteLine("  start               Start the cron service");
        Console.WriteLine("  stop                Stop the cron service");
        Console.WriteLine();
        Console.WriteLine("Schedule formats:");
        Console.WriteLine("  @hourly    - Every hour");
        Console.WriteLine("  @daily     - Every day at midnight");
        Console.WriteLine("  @weekly    - Every week on Sunday");
        Console.WriteLine("  @monthly   - First day of each month");
        Console.WriteLine("  * * * * *  - minute hour day month weekday");
    }

    private static async Task LoadAsync(this CronService service)
    {
        // Just a placeholder - cron service loads automatically
        await Task.CompletedTask;
    }
}