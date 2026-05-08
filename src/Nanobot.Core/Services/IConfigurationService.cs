namespace Nanobot.Core.Services;

using Nanobot.Core.Models;

public interface IConfigurationService
{
    NanobotConfig Config { get; }
    string ConfigPath { get; }
    Task LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    T? GetSection<T>(string sectionName) where T : class;
}
