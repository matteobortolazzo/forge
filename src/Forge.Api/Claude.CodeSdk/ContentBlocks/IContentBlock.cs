namespace Claude.CodeSdk.ContentBlocks;

/// <summary>
/// Base interface for all content blocks within a message.
/// </summary>
public interface IContentBlock
{
    /// <summary>
    /// Gets the type of content block.
    /// </summary>
    ContentBlockType Type { get; }
}
