namespace Nanobot.Core.Interfaces;

using Nanobot.Core.Models;

public interface IProvider : IAsyncDisposable
{
    string Name { get; }
    string DefaultModel { get; }
    Task<Message> CompleteAsync(
        IReadOnlyList<Message> messages,
        IEnumerable<ToolDefinition>? tools = null,
        string? model = null,
        double? temperature = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

public record ToolDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public object InputSchema { get; init; } = new();
}
