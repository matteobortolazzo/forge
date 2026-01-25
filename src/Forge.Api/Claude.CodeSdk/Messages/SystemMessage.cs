using Claude.CodeSdk.Mcp;

namespace Claude.CodeSdk.Messages;

/// <summary>
/// Represents system-level metadata about the session.
/// </summary>
/// <param name="SessionId">The unique identifier for this session.</param>
/// <param name="McpServers">The status of connected MCP servers, if any.</param>
public sealed record SystemMessage(
    string? SessionId = null,
    IReadOnlyList<McpServerStatus>? McpServers = null) : IMessage
{
    /// <inheritdoc />
    public MessageType Type => MessageType.System;
}
