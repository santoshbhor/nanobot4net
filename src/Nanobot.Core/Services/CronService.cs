using System.Text.Json;
using Nanobot.Core.Models;

namespace Nanobot.Core.Services;

public interface ICronService : IAsyncDisposable
{
    Task AddJobAsync(string name, string schedule, string action, string? description = null, CancellationToken cancellationToken = default);
    Task RemoveJobAsync(string name, CancellationToken cancellationToken = default);
    Task UpdateJobAsync(string name, CronJob updatedJob, CancellationToken cancellationToken = default);
    Task<List<CronJob>> ListJobsAsync(CancellationToken cancellationToken = default);
    Task<CronJob?> GetJobAsync(string name, CancellationToken cancellationToken = default);
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task RunNowAsync(string name, CancellationToken cancellationToken = default);
}

public record CronJob
{
    public string Name { get; init; } = string.Empty;
    public string Schedule { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime? LastRun { get; init; }
    public DateTime? NextRun { get; init; }
    public bool Enabled { get; init; } = true;
    public int? SuccessCount { get; init; }
    public int? FailureCount { get; init; }
}

public record CronSchedule
{
    public string Expression { get; init; } = string.Empty;
    public string? HumanReadable { get; init; }
    public DateTime? NextRun { get; private set; }
    
    public static CronSchedule Parse(string expression)
    {
        var schedule = new CronSchedule 
        { 
            Expression = expression,
            NextRun = CalculateNextRun(expression),
            HumanReadable = GenerateHumanReadable(expression)
        };
        
        return schedule;
    }
    
    private static string GenerateHumanReadable(string expression)
    {
        // Handle common cron patterns
        if (expression.StartsWith("@hourly")) return "Every hour";
        if (expression.StartsWith("@daily") || expression.StartsWith("0 0 * * *")) return "Every day at midnight";
        if (expression.StartsWith("@weekly")) return "Every week";
        if (expression.StartsWith("@monthly")) return "Every month";
        
        // Parse standard cron: minute hour day month weekday
        var parts = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 5)
        {
            var minute = parts[0];
            var hour = parts[1];
            var day = parts[2];
            var month = parts[3];
            var weekday = parts[4];
            
            var sb = new System.Text.StringBuilder("Every ");
            
            // Day of month
            if (day != "*" && day != "?")
            {
                sb.Append($"day {day} ");
            }
            
            // Month
            if (month != "*" && month != "?")
            {
                sb.Append($"in month {month} ");
            }
            
            // Weekday
            if (weekday != "*" && weekday != "?")
            {
                var days = new[] {"Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"};
                if (int.TryParse(weekday, out int d) && d >= 0 && d < 7)
                    sb.Append($"on {days[d]} ");
            }
            
            // Time
            if (hour != "*" && minute != "*")
            {
                sb.Append($"at {hour.PadLeft(2, '0')}:{minute.PadLeft(2, '0')}");
            }
            
            return sb.ToString().Trim();
        }
        
        return expression;
    }
    
    private static DateTime? CalculateNextRun(string expression)
    {
        var now = DateTime.UtcNow;
        
        // Handle special patterns
        if (expression.StartsWith("@hourly"))
            return now.AddHours(1).AddMinutes(-now.Minute).AddSeconds(-now.Second);
        if (expression.StartsWith("@daily"))
            return now.AddDays(1).Date;
        if (expression.StartsWith("@weekly"))
            return now.AddDays(7 - (int)now.DayOfWeek).Date;
        if (expression.StartsWith("@monthly"))
            return new DateTime(now.Year, now.Month + 1, 1);
            
        // Parse standard cron expression (minute hour day month weekday)
        var parts = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5) return null;
        
        try
        {
            int minute = parts[0] == "*" ? now.Minute : int.Parse(parts[0]);
            int hour = parts[1] == "*" ? now.Hour : int.Parse(parts[1]);
            int day = parts[2] == "*" || parts[2] == "?" ? now.Day : int.Parse(parts[2]);
            int month = parts[3] == "*" || parts[3] == "?" ? now.Month : int.Parse(parts[3]);
            int weekday = parts[4] == "*" || parts[4] == "?" ? -1 : int.Parse(parts[4]);
            
            // Simple next run calculation - find next matching time
            for (int i = 0; i < 24 * 60 * 366; i++) // Max 1 year ahead
            {
                var candidate = now.AddMinutes(i);
                
                // Check minute
                if (parts[0] != "*" && candidate.Minute != minute) continue;
                // Check hour
                if (parts[1] != "*" && candidate.Hour != hour) continue;
                // Check day
                if (parts[2] != "*" && parts[2] != "?" && candidate.Day != day) continue;
                // Check month
                if (parts[3] != "*" && parts[3] != "?" && candidate.Month != month) continue;
                // Check weekday
                if (weekday >= 0 && (int)candidate.DayOfWeek != weekday) continue;
                
                return candidate;
            }
        }
        catch { }
        
        return now.AddHours(1); // Default to 1 hour
    }
}

public class CronService : ICronService
{
    private readonly string _storagePath;
    private readonly Dictionary<string, CronJob> _jobs = new();
    private readonly System.Timers.Timer _timer;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _running = false;
    private Func<string, Task>? _actionHandler;

    public event Action<string>? OnJobExecuted;
    public event Action<string, Exception>? OnJobFailed;

    public CronService(string? storagePath = null, Func<string, Task>? actionHandler = null)
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _storagePath = storagePath ?? Path.Combine(homeDir, ".nanobot", "cron.json");
        _actionHandler = actionHandler;
        _timer = new System.Timers.Timer(60000); // Check every minute
        _timer.Elapsed += async (_, _) => await CheckAndRunJobsAsync();
        LoadJobs();
    }

    public void SetActionHandler(Func<string, Task> handler)
    {
        _actionHandler = handler;
    }

    public async Task AddJobAsync(string name, string schedule, string action, string? description = null, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var parsed = CronSchedule.Parse(schedule);
            _jobs[name] = new CronJob
            {
                Name = name,
                Schedule = schedule,
                Action = action,
                Description = description,
                NextRun = parsed.NextRun,
                Enabled = true
            };
            await SaveJobsAsync(cancellationToken);
            Console.WriteLine($"✅ Cron job '{name}' added: {parsed.HumanReadable}");
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
                Console.WriteLine($"✅ Cron job '{name}' removed");
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateJobAsync(string name, CronJob updatedJob, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_jobs.ContainsKey(name))
            {
                var parsed = CronSchedule.Parse(updatedJob.Schedule);
                _jobs[name] = updatedJob with { NextRun = parsed.NextRun };
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

    public async Task<CronJob?> GetJobAsync(string name, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _jobs.TryGetValue(name, out var job) ? job : null;
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
        Console.WriteLine($"   Config file: {_storagePath}");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _running = false;
        _timer.Stop();
        Console.WriteLine("⏰ Cron service stopped");
        return Task.CompletedTask;
    }

    public async Task RunNowAsync(string name, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_jobs.TryGetValue(name, out var job))
            {
                Console.WriteLine($"❌ Job '{name}' not found");
                return;
            }
            
            Console.WriteLine($"⏰ Running job '{name}' now...");
            await ExecuteJobAsync(job, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
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
                            await ExecuteJobAsync(job, CancellationToken.None);
                            
                            // Update job with new next run time
                            await _lock.WaitAsync();
                            try
                            {
                                if (_jobs.TryGetValue(job.Name, out var currentJob))
                                {
                                    var parsed = CronSchedule.Parse(currentJob.Schedule);
                                    _jobs[job.Name] = currentJob with
                                    {
                                        LastRun = now,
                                        NextRun = parsed.NextRun,
                                        SuccessCount = (currentJob.SuccessCount ?? 0) + 1
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
                            OnJobFailed?.Invoke(job.Name, ex);
                            
                            // Update failure count
                            await _lock.WaitAsync();
                            try
                            {
                                if (_jobs.TryGetValue(job.Name, out var currentJob))
                                {
                                    _jobs[job.Name] = currentJob with
                                    {
                                        LastRun = now,
                                        FailureCount = (currentJob.FailureCount ?? 0) + 1
                                    };
                                    await SaveJobsAsync(CancellationToken.None);
                                }
                            }
                            finally
                            {
                                _lock.Release();
                            }
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

    private async Task ExecuteJobAsync(CronJob job, CancellationToken cancellationToken)
    {
        Console.WriteLine($"⏰ Executing cron job: {job.Name}");
        
        try
        {
            if (_actionHandler != null)
            {
                await _actionHandler(job.Action);
            }
            else
            {
                // Default action handling
                await ExecuteDefaultActionAsync(job.Action, cancellationToken);
            }
            
            OnJobExecuted?.Invoke(job.Name);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error running cron job '{job.Name}': {ex.Message}");
            OnJobFailed?.Invoke(job.Name, ex);
            throw;
        }
    }

    private static async Task ExecuteDefaultActionAsync(string action, CancellationToken cancellationToken)
    {
        // Handle different action types
        if (action.StartsWith("http://") || action.StartsWith("https://"))
        {
            using var client = new HttpClient();
            await client.GetAsync(action, cancellationToken);
        }
        else if (action.StartsWith("shell:"))
        {
            var command = action[6..];
            await ExecuteShellCommandAsync(command, cancellationToken);
        }
        else if (action.StartsWith("memory:"))
        {
            var content = action[7..];
            // This would integrate with MemoryService
            Console.WriteLine($"   Memory note: {content}");
        }
    }

    private static async Task ExecuteShellCommandAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash",
                Arguments = OperatingSystem.IsWindows() ? $"/c {command}" : $"-c \"{command.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Shell error: {ex.Message}");
        }
    }

    private void LoadJobs()
    {
        try
        {
            if (!File.Exists(_storagePath)) return;
            var json = File.ReadAllText(_storagePath);
            var jobs = JsonSerializer.Deserialize<Dictionary<string, CronJob>>(json);
            if (jobs != null)
            {
                foreach (var kvp in jobs) _jobs[kvp.Key] = kvp.Value;
            }
        }
        catch { }
    }

    private async Task SaveJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var directory = Path.GetDirectoryName(_storagePath)!;
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            var json = JsonSerializer.Serialize(_jobs, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_storagePath, json, cancellationToken);
        }
        catch { }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _timer.Dispose();
        _lock.Dispose();
    }
}