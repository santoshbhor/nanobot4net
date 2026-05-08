namespace Nanobot.Core.Interfaces;

using Nanobot.Core.Models;

public interface IAgent : IAsyncDisposable
{
    Task<Message> ProcessMessageAsync(string userMessage, CancellationToken cancellationToken = default);
    Task<Message> ProcessToolResultAsync(ToolResult toolResult, CancellationToken cancellationToken = default);
    IReadOnlyList<Message> ConversationHistory { get; }
    void ClearHistory();
}
