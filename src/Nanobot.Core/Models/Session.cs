namespace Nanobot.Core.Models;

public record Session
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = "default";
    public List<Message> Messages { get; init; } = new();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; init; } = new();
}
