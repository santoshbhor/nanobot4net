namespace Nanobot.Core.Services;

using System.Text.Json;
using Nanobot.Core.Models;

public interface IMcpClient : IAsyncDisposable
{
    string ServerName { get; }
    bool IsConnected { get; }
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task<List<McpTool>> ListToolsAsync(CancellationToken cancellationToken = default);
    Task<string> CallToolAsync(string toolName, Dictionary<string, object> arguments, CancellationToken cancellationToken = default);
}

public class StdioMcpClient : IMcpClient
{
    private readonly McpServerConfig _config;
    private System.Diagnostics.Process? _process;
    private StreamWriter? _stdin;
    private StreamReader? _stdout;
    private int _requestId = 0;
    private readonly Dictionary<int, TaskCompletionSource<JsonElement>> _pendingRequests = new();

    public string ServerName => _config.Name;
    public bool IsConnected => _process != null && !_process.HasExited;

    public StdioMcpClient(McpServerConfig config)
    {
        _config = config;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_config.Command))
        {
            throw new InvalidOperationException("Command is required for stdio transport");
        }

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = _config.Command,
            Arguments = _config.Args != null ? string.Join(" ", _config.Args) : "",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (_config.Env != null)
        {
            foreach (var env in _config.Env)
            {
                startInfo.Environment[env.Key] = env.Value;
            }
        }

        _process = System.Diagnostics.Process.Start(startInfo);
        if (_process == null)
        {
            throw new InvalidOperationException("Failed to start MCP server process");
        }

        _stdin = _process.StandardInput;
        _stdout = _process.StandardOutput;

        // Initialize protocol
        var initRequest = new
        {
            jsonrpc = "2.0",
            id = ++_requestId,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new { name = "nanobot-csharp", version = "0.1.5" }
            }
        };

        var response = await SendRequestAsync(initRequest, cancellationToken);
        
        // Send initialized notification
        var notify = new
        {
            jsonrpc = "2.0",
            method = "notifications/initialized"
        };
        await SendNotificationAsync(notify, cancellationToken);
    }

    public async Task<List<McpTool>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        var request = new
        {
            jsonrpc = "2.0",
            id = ++_requestId,
            method = "tools/list"
        };

        var response = await SendRequestAsync(request, cancellationToken);
        var tools = new List<McpTool>();

        if (response.TryGetProperty("result", out var result) &&
            result.TryGetProperty("tools", out var toolsArray))
        {
            foreach (var toolElement in toolsArray.EnumerateArray())
            {
                tools.Add(new McpTool
                {
                    Name = toolElement.GetProperty("name").GetString() ?? "",
                    Description = toolElement.TryGetProperty("description", out var desc) 
                        ? desc.GetString() ?? "" : "",
                    InputSchema = toolElement.TryGetProperty("inputSchema", out var schema)
                        ? JsonSerializer.Deserialize<object>(schema.GetRawText()) ?? new { }
                        : new { }
                });
            }
        }

        return tools;
    }

    public async Task<string> CallToolAsync(string toolName, Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            jsonrpc = "2.0",
            id = ++_requestId,
            method = "tools/call",
            @params = new
            {
                name = toolName,
                arguments = arguments
            }
        };

        var response = await SendRequestAsync(request, cancellationToken);
        
        if (response.TryGetProperty("result", out var result))
        {
            if (result.TryGetProperty("content", out var contentArray))
            {
                var textParts = new List<string>();
                foreach (var item in contentArray.EnumerateArray())
                {
                    if (item.TryGetProperty("type", out var type) && type.GetString() == "text")
                    {
                        if (item.TryGetProperty("text", out var text))
                        {
                            textParts.Add(text.GetString() ?? "");
                        }
                    }
                }
                return string.Join("\n", textParts);
            }
        }
        else if (response.TryGetProperty("error", out var error))
        {
            var errorMsg = error.TryGetProperty("message", out var msg) 
                ? msg.GetString() ?? "Unknown error" 
                : "Unknown error";
            return $"Error calling tool '{toolName}': {errorMsg}";
        }

        return "Tool executed successfully";
    }

    private async Task<JsonElement> SendRequestAsync(object request, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request);
        var tcs = new TaskCompletionSource<JsonElement>();
        _pendingRequests[_requestId] = tcs;

        await _stdin!.WriteLineAsync(json.AsMemory(), cancellationToken);
        await _stdin.FlushAsync(cancellationToken);

        // Simple response reading - in production, use a proper message reader
        var responseLine = await _stdout!.ReadLineAsync(cancellationToken);
        if (string.IsNullOrEmpty(responseLine))
        {
            throw new InvalidOperationException("No response from MCP server");
        }

        var response = JsonDocument.Parse(responseLine).RootElement;
        return response;
    }

    private async Task SendNotificationAsync(object notification, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(notification);
        await _stdin!.WriteLineAsync(json.AsMemory(), cancellationToken);
        await _stdin.FlushAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_process != null && !_process.HasExited)
        {
            _process.Kill();
            await _process.WaitForExitAsync();
            _process.Dispose();
        }
    }
}
