using Nanobot.Core.Models;
using Nanobot.Core.Services;
using Nanobot.Core.Interfaces;
using Nanobot.Providers;
using NanobotAgent = Nanobot.Agent.Agent;

namespace Nanobot.WebUI.Services;

public class AgentService
{
    private readonly IConfigurationService _configService;
    private readonly Dictionary<string, NanobotAgent> _sessions = new();
    private readonly object _lock = new();

    public AgentService(IConfigurationService configService)
    {
        _configService = configService;
    }

    public async Task EnsureInitializedAsync()
    {
        await _configService.LoadAsync();
    }

    public IAgent GetOrCreateSession(string sessionId)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var agent))
            {
                var provider = ProviderFactory.CreateProviderFromConfig(_configService.Config);
                if (provider != null)
                {
                    agent = new NanobotAgent(provider, _configService.Config.Agents.Defaults);
                    _sessions[sessionId] = agent;
                }
            }
            return agent!;
        }
    }

    public async Task<string> SendMessageAsync(string sessionId, string message)
    {
        var agent = GetOrCreateSession(sessionId);
        var response = await agent.ProcessMessageAsync(message);
        return response.Content;
    }

    public List<Message> GetHistory(string sessionId)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(sessionId, out var agent))
            {
                return agent.ConversationHistory.ToList();
            }
        }
        return new List<Message>();
    }

    public void ClearSession(string sessionId)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(sessionId, out var agent))
            {
                agent.ClearHistory();
            }
        }
    }
}