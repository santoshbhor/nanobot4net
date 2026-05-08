namespace Nanobot.Channels;

using System.Net;
using System.Net.WebSockets;
using Nanobot.Core.Interfaces;

public class WebSocketChannel : IChannel
{
    public string Name => "websocket";
    public bool IsEnabled => true;

    private HttpListener? _listener;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly List<WebSocket> _sockets = new();

    public event Func<string, string, Task>? OnMessageReceived;
    public event Func<string, Task>? OnReady;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://localhost:8765/ws/");
        _listener.Start();

        Console.WriteLine("WebSocket server started at ws://localhost:8765/ws/");

        if (OnReady != null)
        {
            await OnReady.Invoke("websocket");
        }

        _ = Task.Run(() => AcceptLoopAsync(_cancellationTokenSource.Token));
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var context = await _listener!.GetContextAsync();
                
                if (context.Request.IsWebSocketRequest)
                {
                    var wsContext = await context.AcceptWebSocketAsync(null);
                    _sockets.Add(wsContext.WebSocket);
                    _ = Task.Run(() => HandleWebSocketAsync(wsContext.WebSocket, cancellationToken));
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"Error accepting connection: {ex.Message}");
            }
        }
    }

    private async Task HandleWebSocketAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                    
                    if (OnMessageReceived != null)
                    {
                        await OnMessageReceived.Invoke("client", message);
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken);
                }
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"WebSocket error: {ex.Message}");
        }
        finally
        {
            _sockets.Remove(socket);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _cancellationTokenSource.Cancel();
        
        foreach (var socket in _sockets.ToList())
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None);
            }
        }

        _listener?.Stop();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cancellationTokenSource.Dispose();
    }
}
