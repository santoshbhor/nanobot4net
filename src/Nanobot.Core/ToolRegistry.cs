namespace Nanobot.Core;

using Nanobot.Core.Interfaces;
using Nanobot.Core.Models;
using System.Text.Json;

public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ToolEntry> _tools = new();

    public void RegisterTool(string name, string description, Func<Dictionary<string, object>, Task<string>> handler, object? inputSchema = null)
    {
        _tools[name] = new ToolEntry
        {
            Name = name,
            Description = description,
            Handler = handler,
            InputSchema = inputSchema ?? new { type = "object", properties = new { } }
        };
    }

    public IReadOnlyList<ToolDefinition> GetTools()
    {
        return _tools.Values.Select(t => new ToolDefinition
        {
            Name = t.Name,
            Description = t.Description,
            InputSchema = t.InputSchema
        }).ToList();
    }

    public async Task<string> ExecuteToolAsync(string toolName, string arguments)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return "Error: Tool name cannot be empty";
        }

        if (!_tools.TryGetValue(toolName, out var tool))
        {
            return $"Error: Tool '{toolName}' not found. Available tools: {string.Join(", ", _tools.Keys)}";
        }

        try
        {
            var args = string.IsNullOrWhiteSpace(arguments) 
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(arguments) ?? new Dictionary<string, object>();
            
            var result = await tool.Handler(args);
            return result ?? "Tool execution completed with no output";
        }
        catch (JsonException jsonEx)
        {
            return $"Error: Invalid JSON arguments for tool '{toolName}': {jsonEx.Message}";
        }
        catch (Exception ex)
        {
            return $"Error executing tool '{toolName}': {ex.GetType().Name}: {ex.Message}";
        }
    }

    private class ToolEntry
    {
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public Func<Dictionary<string, object>, Task<string>> Handler { get; init; } = _ => Task.FromResult(string.Empty);
        public object InputSchema { get; init; } = new();
    }
}
