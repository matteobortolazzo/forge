namespace Claude.CodeSdk;

/// <summary>
/// Represents a query request to the Claude Agent.
/// This wrapper provides an API-compatible request structure.
/// </summary>
public sealed record QueryRequest
{
    /// <summary>
    /// Gets or sets the prompt to send to Claude.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Gets or sets optional execution options that override client defaults.
    /// </summary>
    public ClaudeAgentOptions? Options { get; init; }
}
