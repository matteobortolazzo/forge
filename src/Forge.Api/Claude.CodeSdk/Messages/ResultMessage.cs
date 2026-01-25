namespace Claude.CodeSdk.Messages;

/// <summary>
/// Represents token usage statistics.
/// </summary>
/// <param name="InputTokens">The number of input tokens consumed.</param>
/// <param name="OutputTokens">The number of output tokens generated.</param>
/// <param name="CacheReadInputTokens">The number of tokens read from cache.</param>
/// <param name="CacheCreationInputTokens">The number of tokens used for cache creation.</param>
public sealed record Usage(
    int InputTokens,
    int OutputTokens,
    int CacheReadInputTokens = 0,
    int CacheCreationInputTokens = 0)
{
    /// <summary>
    /// Gets the total tokens consumed (input + output).
    /// </summary>
    public int TotalTokens => InputTokens + OutputTokens;
}

/// <summary>
/// Represents the final result message with usage statistics.
/// </summary>
/// <param name="Usage">The token usage statistics.</param>
/// <param name="SessionId">The session identifier.</param>
/// <param name="CostUsd">The cost in USD, if available.</param>
/// <param name="DurationMs">The duration in milliseconds.</param>
/// <param name="NumTurns">The number of conversational turns.</param>
public sealed record ResultMessage(
    Usage Usage,
    string? SessionId = null,
    decimal? CostUsd = null,
    long? DurationMs = null,
    int? NumTurns = null) : IMessage
{
    /// <inheritdoc />
    public MessageType Type => MessageType.Result;
}
