namespace Nanobot.Skills;

using System.Text.Json;
using Nanobot.Core;
using Nanobot.Core.Services;

public class BuiltInSkills
{
    private readonly ToolRegistry _toolRegistry;

    public BuiltInSkills(ToolRegistry toolRegistry)
    {
        _toolRegistry = toolRegistry;
        RegisterSkills();
    }

    private readonly IWebSearchService? _searchService;

    public BuiltInSkills(ToolRegistry toolRegistry, IWebSearchService? searchService = null)
    {
        _toolRegistry = toolRegistry;
        _searchService = searchService;
        RegisterSkills();
    }

    private void RegisterSkills()
    {
        _toolRegistry.RegisterTool(
            "execute_shell",
            "Execute a shell command",
            ExecuteShellAsync,
            new
            {
                type = "object",
                properties = new
                {
                    command = new { type = "string", description = "The shell command to execute" }
                },
                required = new[] { "command" }
            });

        _toolRegistry.RegisterTool(
            "read_file",
            "Read contents of a file",
            ReadFileAsync,
            new
            {
                type = "object",
                properties = new
                {
                    path = new { type = "string", description = "Path to the file" }
                },
                required = new[] { "path" }
            });

        _toolRegistry.RegisterTool(
            "write_file",
            "Write content to a file",
            WriteFileAsync,
            new
            {
                type = "object",
                properties = new
                {
                    path = new { type = "string", description = "Path to the file" },
                    content = new { type = "string", description = "Content to write" }
                },
                required = new[] { "path", "content" }
            });

        _toolRegistry.RegisterTool(
            "list_files",
            "List files in a directory",
            ListFilesAsync,
            new
            {
                type = "object",
                properties = new
                {
                    path = new { type = "string", description = "Directory path", @default = "." }
                }
            });

        if (_searchService != null)
        {
            _toolRegistry.RegisterTool(
                "web_search",
                "Search the web for information",
                WebSearchAsync,
                new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "The search query" },
                        count = new { type = "integer", description = "Number of results", @default = 5 }
                    },
                    required = new[] { "query" }
                });
        }
    }

    private static async Task<string> ExecuteShellAsync(Dictionary<string, object> args)
    {
        if (!args.TryGetValue("command", out var cmdObj) || cmdObj is not string cmd)
        {
            return "Error: command parameter is required";
        }

        if (string.IsNullOrWhiteSpace(cmd))
        {
            return "Error: command cannot be empty";
        }

        try
        {
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash",
                Arguments = OperatingSystem.IsWindows() ? $"/c {cmd}" : $"-c \"{cmd.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            using var process = System.Diagnostics.Process.Start(processStartInfo);
            if (process == null)
            {
                return "Error: Failed to start process";
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            
            var output = await outputTask;
            var error = await errorTask;

            if (!string.IsNullOrEmpty(error))
            {
                return $"Error: {error}";
            }

            return string.IsNullOrEmpty(output) ? "(command executed successfully with no output)" : output;
        }
        catch (Exception ex)
        {
            return $"Error executing command '{cmd}': {ex.GetType().Name}: {ex.Message}";
        }
    }

    private static async Task<string> ReadFileAsync(Dictionary<string, object> args)
    {
        if (!args.TryGetValue("path", out var pathObj) || pathObj is not string path)
        {
            return "Error: path parameter is required";
        }

        try
        {
            if (!File.Exists(path))
            {
                return $"Error: File not found: {path}";
            }

            var content = await File.ReadAllTextAsync(path);
            return content;
        }
        catch (Exception ex)
        {
            return $"Error reading file: {ex.Message}";
        }
    }

    private static async Task<string> WriteFileAsync(Dictionary<string, object> args)
    {
        if (!args.TryGetValue("path", out var pathObj) || pathObj is not string path)
        {
            return "Error: path parameter is required";
        }

        if (!args.TryGetValue("content", out var contentObj) || contentObj is not string content)
        {
            return "Error: content parameter is required";
        }

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(path, content);
            return $"File written successfully: {path}";
        }
        catch (Exception ex)
        {
            return $"Error writing file: {ex.Message}";
        }
    }

    private static Task<string> ListFilesAsync(Dictionary<string, object> args)
    {
        var path = args.TryGetValue("path", out var pathObj) && pathObj is string p ? p : ".";
        
        try
        {
            if (!Directory.Exists(path))
            {
                return Task.FromResult($"Error: Directory not found: {path}");
            }

            var files = Directory.GetFileSystemEntries(path);
            return Task.FromResult(string.Join("\n", files));
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error listing files: {ex.Message}");
        }
    }

    private async Task<string> WebSearchAsync(Dictionary<string, object> args)
    {
        if (_searchService == null)
        {
            return "Error: Web search service not configured";
        }

        if (!args.TryGetValue("query", out var queryObj) || queryObj is not string query)
        {
            return "Error: query parameter is required";
        }

        var count = 5;
        if (args.TryGetValue("count", out var countObj))
        {
            if (countObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
            {
                count = jsonElement.GetInt32();
            }
            else if (countObj is int i)
            {
                count = i;
            }
        }

        try
        {
            var results = await _searchService.SearchAsync(query, count);
            
            if (results.Count == 0)
            {
                return "No search results found.";
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Search results for: {query}");
            sb.AppendLine();
            
            for (var i = 0; i < results.Count; i++)
            {
                var result = results[i];
                sb.AppendLine($"{i + 1}. {result.Title}");
                sb.AppendLine($"   URL: {result.Url}");
                sb.AppendLine($"   {result.Snippet}");
                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error during web search: {ex.Message}";
        }
    }
}
