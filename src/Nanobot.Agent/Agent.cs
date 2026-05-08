namespace Nanobot.Agent;

using Nanobot.Core.Interfaces;
using Nanobot.Core.Models;
using Nanobot.Providers;

public class Agent : IAgent
{
    private readonly IProvider _provider;
    private readonly List<Message> _conversationHistory = new();
    private readonly AgentDefaults _defaults;

    public IReadOnlyList<Message> ConversationHistory => _conversationHistory.AsReadOnly();

    public Agent(IProvider provider, AgentDefaults? defaults = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _defaults = defaults ?? new AgentDefaults();
    }

    public async Task<Message> ProcessMessageAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        _conversationHistory.Add(new Message { Role = "user", Content = userMessage });

        var response = await _provider.CompleteAsync(
            _conversationHistory,
            null,
            _defaults.Model,
            _defaults.Temperature,
            _defaults.MaxTokens,
            cancellationToken);

        _conversationHistory.Add(response);
        return response;
    }

    public async Task<Message> ProcessToolResultAsync(ToolResult toolResult, CancellationToken cancellationToken = default)
    {
        var toolMessage = new Message
        {
            Role = "tool",
            Content = toolResult.Content,
            ToolCallId = toolResult.ToolCallId
        };

        _conversationHistory.Add(toolMessage);

        var response = await _provider.CompleteAsync(
            _conversationHistory,
            null,
            _defaults.Model,
            _defaults.Temperature,
            _defaults.MaxTokens,
            cancellationToken);

        _conversationHistory.Add(response);
        return response;
    }

    public void ClearHistory()
    {
        _conversationHistory.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        if (_provider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
    }
}
