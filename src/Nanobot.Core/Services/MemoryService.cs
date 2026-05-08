namespace Nanobot.Core.Services;

using System.Text.Json;
using Nanobot.Core.Models;

public interface IMemoryService : IAsyncDisposable
{
    Task StoreMemoryAsync(string key, string content, CancellationToken cancellationToken = default);
    Task<string?> RetrieveMemoryAsync(string key, CancellationToken cancellationToken = default);
    Task<List<string>> SearchMemoriesAsync(string query, int limit = 10, CancellationToken cancellationToken = default);
    Task DeleteMemoryAsync(string key, CancellationToken cancellationToken = default);
    Task<List<string>> ListMemoryKeysAsync(CancellationToken cancellationToken = default);
}

public class MemoryService : IMemoryService
{
    private readonly string _memoryPath;
    private readonly Dictionary<string, MemoryEntry> _memories = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public MemoryService(string? memoryPath = null)
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _memoryPath = memoryPath ?? Path.Combine(homeDir, ".nanobot", "memory.json");
        
        LoadMemories();
    }

    public async Task StoreMemoryAsync(string key, string content, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _memories[key] = new MemoryEntry
            {
                Key = key,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await SaveMemoriesAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string?> RetrieveMemoryAsync(string key, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _memories.TryGetValue(key, out var entry) ? entry.Content : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<string>> SearchMemoriesAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var results = _memories.Values
                .Where(m => m.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           m.Key.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m => m.UpdatedAt)
                .Take(limit)
                .Select(m => $"[{m.Key}]: {m.Content}")
                .ToList();

            return results;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteMemoryAsync(string key, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_memories.Remove(key))
            {
                await SaveMemoriesAsync(cancellationToken);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<string>> ListMemoryKeysAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _memories.Keys.ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    private void LoadMemories()
    {
        try
        {
            if (!File.Exists(_memoryPath))
            {
                return;
            }

            var json = File.ReadAllText(_memoryPath);
            var entries = JsonSerializer.Deserialize<Dictionary<string, MemoryEntry>>(json);
            if (entries != null)
            {
                foreach (var kvp in entries)
                {
                    _memories[kvp.Key] = kvp.Value;
                }
            }
        }
        catch
        {
            // Ignore load errors
        }
    }

    private async Task SaveMemoriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var directory = Path.GetDirectoryName(_memoryPath)!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_memories, _jsonOptions);
            await File.WriteAllTextAsync(_memoryPath, json, cancellationToken);
        }
        catch
        {
            // Ignore save errors
        }
    }

    public async ValueTask DisposeAsync()
    {
        await SaveMemoriesAsync(CancellationToken.None);
        _lock.Dispose();
    }

    private record MemoryEntry
    {
        public string Key { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
