namespace Nanobot.Core.Services;

using System.Text.Json;
using Nanobot.Core.Models;

public interface ICronService : IAsyncDisposable
{
    Task AddJobAsync(string name, string schedule, string command, CancellationToken cancellationToken = default);
    Task RemoveJobAsync(string name, CancellationToken cancellationToken = default);
    Task<List<CronJob>> ListJobsAsync(CancellationToken cancellationToken = default);
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

public record CronJob
{
    public string Name { get; init; } = string.Empty;
    public string Schedule { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;
    public DateTime? LastRun { get; init; }
    public DateTime? NextRun { get; init; }
    public bool Enabled { get; init; } = true;
}

public class CronService : ICronService
{
    private readonly string _storagePath;
    private readonly Dictionary<string, CronJob> _jobs = new();
    private readonly System.Timers.Timer _timer;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _running = false;

    public CronService(string? storagePath = null)
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _storagePath = storagePath ?? Path.Combine(homeDir, ".nanobot", "cron.json");
        _timer = new System.Timers.Timer(60000); // Check every minute
        _timer.Elapsed += async (_, _) => await CheckAndRunJobsAsync();
        LoadJobs();
    }

    public async Task AddJobAsync(string name, string schedule, string command, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _jobs[name] = new CronJob
            {
                Name = name,
                Schedule = schedule,
                Command = command,
                NextRun = CalculateNextRun(schedule)
            };
            await SaveJobsAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveJobAsync(string name, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_jobs.Remove(name))
            {
                await SaveJobsAsync(cancellationToken);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<CronJob>> ListJobsAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _jobs.Values.ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _running = true;
        _timer.Start();
        Console.WriteLine("⏰ Cron service started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _running = false;
        _timer.Stop();
        Console.WriteLine("⏰ Cron service stopped");
        return Task.CompletedTask;
    }

    private async Task CheckAndRunJobsAsync()
    {
        if (!_running) return;

        await _lock.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _jobs.ToList())
            {
                var job = kvp.Value;
                if (!job.Enabled || job.NextRun == null) continue;

                if (job.NextRun <= now)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            Console.WriteLine($"⏰ Running cron job: {job.Name}");
                            // Execute command (simplified - would call agent in real implementation)
                            await ExecuteCommandAsync(job.Command);
                            
                            // Update last run and calculate next run
                            await _lock.WaitAsync();
                            try
                            {
                                if (_jobs.TryGetValue(job.Name, out var currentJob))
                                {
                                    _jobs[job.Name] = currentJob with
                                    {
                                        LastRun = now,
                                        NextRun = CalculateNextRun(currentJob.Schedule)
                                    };
                                    await SaveJobsAsync(CancellationToken.None);
                                }
                            }
                            finally
                            {
                                _lock.Release();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Error running cron job '{job.Name}': {ex.Message}");
                        }
                    });
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private static async Task ExecuteCommandAsync(string command)
    {
        try
        {
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash",
                Arguments = OperatingSystem.IsWindows() ? $"/c {command}" : $"-c \"{command.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processStartInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing cron command: {ex.Message}");
        }
    }

    private static DateTime? CalculateNextRun(string schedule)
    {
        // Simplified - in real implementation, use croniter library or similar
        // For now, just return 1 hour from now as placeholder
        return DateTime.UtcNow.AddHours(1);
    }

    private void LoadJobs()
    {
        try
        {
            if (!File.Exists(_storagePath))
            {
                return;
            }

            var json = File.ReadAllText(_storagePath);
            var jobs = JsonSerializer.Deserialize<Dictionary<string, CronJob>>(json);
            if (jobs != null)
            {
                foreach (var kvp in jobs)
                {
                    _jobs[kvp.Key] = kvp.Value;
                }
            }
        }
        catch
        {
            // Ignore load errors
        }
    }

    private async Task SaveJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var directory = Path.GetDirectoryName(_storagePath)!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_jobs, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_storagePath, json, cancellationToken);
        }
        catch
        {
            // Ignore save errors
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _timer.Dispose();
        _lock.Dispose();
    }
}
