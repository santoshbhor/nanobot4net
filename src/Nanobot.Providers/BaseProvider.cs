namespace Nanobot.Providers;

using Nanobot.Core.Interfaces;
using Nanobot.Core.Models;

public abstract class BaseProvider : IProvider
{
    public abstract string Name { get; }
    public abstract string DefaultModel { get; }
    
    protected readonly string? ApiKey;
    protected readonly string? BaseUrl;
    protected readonly Dictionary<string, object>? AdditionalProperties;

    protected BaseProvider(string? apiKey = null, string? baseUrl = null, Dictionary<string, object>? additionalProperties = null)
    {
        ApiKey = apiKey;
        BaseUrl = baseUrl;
        AdditionalProperties = additionalProperties;
    }

    public abstract Task<Message> CompleteAsync(
        IReadOnlyList<Message> messages,
        IEnumerable<ToolDefinition>? tools = null,
        string? model = null,
        double? temperature = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);

    public virtual Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(!string.IsNullOrEmpty(ApiKey));
    }

    public abstract ValueTask DisposeAsync();
}
