namespace Nanobot.Core.Services;

using System.Text.Json;
using Nanobot.Core.Models;

public class ConfigurationService : IConfigurationService
{
    private NanobotConfig _config = new();
    public NanobotConfig Config => _config;
    public string ConfigPath { get; }

    public ConfigurationService(string? configPath = null)
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        ConfigPath = configPath ?? Path.Combine(homeDir, ".nanobot", "config.json");
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(ConfigPath))
        {
            _config = new NanobotConfig();
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(ConfigPath, cancellationToken);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _config = JsonSerializer.Deserialize<NanobotConfig>(json, options) ?? new NanobotConfig();
        }
        catch (Exception)
        {
            _config = new NanobotConfig();
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(ConfigPath)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(_config, options);
        await File.WriteAllTextAsync(ConfigPath, json, cancellationToken);
    }

    public T? GetSection<T>(string sectionName) where T : class
    {
        var json = JsonSerializer.Serialize(_config);
        var doc = JsonDocument.Parse(json);
        
        if (doc.RootElement.TryGetProperty(sectionName, out var element))
        {
            return JsonSerializer.Deserialize<T>(element.GetRawText());
        }

        return null;
    }
}
