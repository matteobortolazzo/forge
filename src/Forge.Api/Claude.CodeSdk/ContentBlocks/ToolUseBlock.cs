using System.Text.Json;

namespace Claude.CodeSdk.ContentBlocks;

/// <summary>
/// Represents a tool invocation request content block.
/// </summary>
/// <param name="Id">The unique identifier for this tool use.</param>
/// <param name="Name">The name of the tool being invoked.</param>
/// <param name="Input">The input parameters for the tool as a JSON element.</param>
public sealed record ToolUseBlock(string Id, string Name, JsonElement Input) : IContentBlock
{
    /// <inheritdoc />
    public ContentBlockType Type => ContentBlockType.ToolUse;
}
