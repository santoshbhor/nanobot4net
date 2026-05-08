namespace Nanobot.Core.Models;

public record Message
{
    public string Role { get; init; } = "user";
    public string Content { get; init; } = string.Empty;
    public List<ToolCall>? ToolCalls { get; init; }
    public string? ToolCallId { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

public record ToolCall
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = "function";
    public FunctionCall Function { get; init; } = new();
}

public record FunctionCall
{
    public string Name { get; init; } = string.Empty;
    public string Arguments { get; init; } = string.Empty;
}

public record ToolResult
{
    public string ToolCallId { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public bool IsError { get; init; }
}
