namespace Nanobot.Core.Interfaces;

using Nanobot.Core.Models;

public interface IToolRegistry
{
    void RegisterTool(string name, string description, Func<Dictionary<string, object>, Task<string>> handler, object? inputSchema = null);
    IReadOnlyList<ToolDefinition> GetTools();
    Task<string> ExecuteToolAsync(string toolName, string arguments);
}
