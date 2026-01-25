namespace Claude.CodeSdk.ContentBlocks;

/// <summary>
/// Specifies the type of content block in a message.
/// </summary>
public enum ContentBlockType
{
    /// <summary>
    /// Plain text content.
    /// </summary>
    Text,

    /// <summary>
    /// A tool invocation request.
    /// </summary>
    ToolUse,

    /// <summary>
    /// The result of a tool invocation.
    /// </summary>
    ToolResult
}
