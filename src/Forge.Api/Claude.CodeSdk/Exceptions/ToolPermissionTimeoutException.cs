namespace Claude.CodeSdk.Exceptions;

/// <summary>
/// Thrown when a tool permission handler exceeds the configured timeout.
/// </summary>
public sealed class ToolPermissionTimeoutException : ClaudeAgentException
{
    /// <summary>
    /// Gets the name of the tool that timed out.
    /// </summary>
    public string ToolName { get; }

    /// <summary>
    /// Gets the ID of the tool use that timed out.
    /// </summary>
    public string ToolUseId { get; }

    /// <summary>
    /// Gets the timeout duration in milliseconds.
    /// </summary>
    public int TimeoutMs { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolPermissionTimeoutException"/> class.
    /// </summary>
    /// <param name="toolName">The name of the tool.</param>
    /// <param name="toolUseId">The ID of the tool use.</param>
    /// <param name="timeoutMs">The timeout duration in milliseconds.</param>
    public ToolPermissionTimeoutException(string toolName, string toolUseId, int timeoutMs)
        : base($"Tool permission handler for '{toolName}' timed out after {timeoutMs}ms")
    {
        ToolName = toolName;
        ToolUseId = toolUseId;
        TimeoutMs = timeoutMs;
    }
}
