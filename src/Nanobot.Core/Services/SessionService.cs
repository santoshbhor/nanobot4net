namespace Nanobot.Core.Services;

using System.Text.Json;
using Nanobot.Core.Models;

public class SessionService : ISessionService
{
    private readonly string _storagePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public SessionService(string? storagePath = null)
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _storagePath = storagePath ?? Path.Combine(homeDir, ".nanobot", "sessions");
        
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<Session?> LoadSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_storagePath, $"{sessionId}.json");
        
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return JsonSerializer.Deserialize<Session>(json);
    }

    public async Task SaveSessionAsync(Session session, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_storagePath, $"{session.Id}.json");
        var json = JsonSerializer.Serialize(session, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    public async Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_storagePath, $"{sessionId}.json");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        await Task.CompletedTask;
    }

    public async Task<List<string>> ListSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
            Directory.GetFiles(_storagePath, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => name!)
                .ToList(), cancellationToken);
    }
}

public interface ISessionService
{
    Task<Session?> LoadSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task SaveSessionAsync(Session session, CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<List<string>> ListSessionsAsync(CancellationToken cancellationToken = default);
}
