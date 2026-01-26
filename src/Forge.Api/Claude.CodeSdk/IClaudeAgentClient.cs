using Claude.CodeSdk.Messages;

namespace Claude.CodeSdk;

/// <summary>
/// Interface for interacting with Claude Code CLI programmatically.
/// </summary>
public interface IClaudeAgentClient : IAsyncDisposable
{
    /// <summary>
    /// Sends a query and returns all messages.
    /// </summary>
    Task<IReadOnlyList<IMessage>> QueryAsync(string prompt, ClaudeAgentOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Sends a query and streams messages as they arrive.
    /// </summary>
    IAsyncEnumerable<IMessage> QueryStreamAsync(string prompt, ClaudeAgentOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Sends a query and returns only the text content from assistant messages.
    /// </summary>
    Task<string> QueryTextAsync(string prompt, ClaudeAgentOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Sends a query request and returns all messages.
    /// </summary>
    Task<IReadOnlyList<IMessage>> QueryAsync(QueryRequest request, CancellationToken ct = default);

    /// <summary>
    /// Sends a query request and streams messages as they arrive.
    /// </summary>
    IAsyncEnumerable<IMessage> QueryStreamAsync(QueryRequest request, CancellationToken ct = default);

    /// <summary>
    /// Sends a query request and returns only the text content.
    /// </summary>
    Task<string> QueryTextAsync(QueryRequest request, CancellationToken ct = default);
}
