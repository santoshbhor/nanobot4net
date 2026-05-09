using Microsoft.AspNetCore.SignalR;
using Nanobot.WebUI.Services;

namespace Nanobot.WebUI.Hubs;

public class ChatHub : Hub
{
    private readonly AgentService _agentService;
    private static readonly Dictionary<string, string> _userConnections = new();

    public ChatHub(AgentService agentService)
    {
        _agentService = agentService;
    }

    public async Task SendMessage(string message)
    {
        var sessionId = Context.ConnectionId;
        
        try
        {
            var response = await _agentService.SendMessageAsync(sessionId, message);
            
            // Send user message
            await Clients.Caller.SendAsync("ReceiveMessage", new 
            {
                role = "user",
                content = message,
                timestamp = DateTime.UtcNow
            });
            
            // Send bot response
            await Clients.Caller.SendAsync("ReceiveMessage", new 
            {
                role = "assistant",
                content = response,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("ReceiveMessage", new 
            {
                role = "system",
                content = $"Error: {ex.Message}",
                timestamp = DateTime.UtcNow
            });
        }
    }

    public List<object> GetHistory()
    {
        var sessionId = Context.ConnectionId;
        return _agentService.GetHistory(sessionId).Select(m => new 
        {
            role = m.Role,
            content = m.Content
        }).ToList<object>();
    }

    public async Task ClearHistory()
    {
        var sessionId = Context.ConnectionId;
        _agentService.ClearSession(sessionId);
        await Clients.Caller.SendAsync("HistoryCleared");
    }

    public override async Task OnConnectedAsync()
    {
        await _agentService.EnsureInitializedAsync();
        await base.OnConnectedAsync();
        
        // Send welcome message
        await Clients.Caller.SendAsync("ReceiveMessage", new 
        {
            role = "system",
            content = "🐈 Connected to nanobot! Send me a message to start chatting.",
            timestamp = DateTime.UtcNow
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}