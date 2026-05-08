namespace Nanobot.Core.Models;

public record McpServerConfig
{
    public string Name { get; init; } = string.Empty;
    public string Transport { get; init; } = "stdio"; // stdio or sse
    public string? Command { get; init; }
    public List<string>? Args { get; init; }
    public Dictionary<string, string>? Env { get; init; }
    public string? Url { get; init; }
    public bool Enabled { get; init; } = true;
}

public record McpTool
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public object InputSchema { get; init; } = new();
}

public record McpResource
{
    public string Uri { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string MimeType { get; init; } = "text/plain";
}
