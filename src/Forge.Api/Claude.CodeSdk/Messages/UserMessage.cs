using Claude.CodeSdk.ContentBlocks;

namespace Claude.CodeSdk.Messages;

/// <summary>
/// Represents a user input message.
/// </summary>
/// <param name="Content">The content blocks in this message.</param>
public sealed record UserMessage(IReadOnlyList<IContentBlock> Content) : IMessage
{
    /// <inheritdoc />
    public MessageType Type => MessageType.User;

    /// <summary>
    /// Gets the concatenated text content from all text blocks.
    /// </summary>
    public string Text => string.Join("", Content.OfType<TextBlock>().Select(b => b.Text));
}
