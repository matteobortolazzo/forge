namespace Claude.CodeSdk.ContentBlocks;

/// <summary>
/// Represents the result of a tool invocation content block.
/// </summary>
/// <param name="ToolUseId">The identifier of the tool use this result corresponds to.</param>
/// <param name="Content">The content/output from the tool execution.</param>
/// <param name="IsError">Indicates whether the tool execution resulted in an error.</param>
public sealed record ToolResultBlock(string ToolUseId, string Content, bool IsError = false) : IContentBlock
{
    /// <inheritdoc />
    public ContentBlockType Type => ContentBlockType.ToolResult;
}
