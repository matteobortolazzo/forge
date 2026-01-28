namespace Claude.CodeSdk.Exceptions;

/// <summary>
/// Thrown when a tool permission request is denied with the interrupt flag set.
/// </summary>
public sealed class ToolDeniedException : ClaudeAgentException
{
    /// <summary>
    /// Gets the name of the tool that was denied.
    /// </summary>
    public string ToolName { get; }

    /// <summary>
    /// Gets the ID of the tool use that was denied.
    /// </summary>
    public string ToolUseId { get; }

    /// <summary>
    /// Gets the reason provided for denying the tool.
    /// </summary>
    public string DenyReason { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolDeniedException"/> class.
    /// </summary>
    /// <param name="toolName">The name of the tool that was denied.</param>
    /// <param name="toolUseId">The ID of the tool use.</param>
    /// <param name="denyReason">The reason for denying the tool.</param>
    public ToolDeniedException(string toolName, string toolUseId, string denyReason)
        : base($"Tool '{toolName}' was denied: {denyReason}")
    {
        ToolName = toolName;
        ToolUseId = toolUseId;
        DenyReason = denyReason;
    }
}
