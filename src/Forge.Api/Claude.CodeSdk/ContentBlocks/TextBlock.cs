namespace Claude.CodeSdk.ContentBlocks;

/// <summary>
/// Represents a plain text content block.
/// </summary>
/// <param name="Text">The text content.</param>
public sealed record TextBlock(string Text) : IContentBlock
{
    /// <inheritdoc />
    public ContentBlockType Type => ContentBlockType.Text;
}
