using Claude.CodeSdk.ContentBlocks;

namespace Claude.CodeSdk.Messages;

/// <summary>
/// Represents a message from the assistant (Claude).
/// </summary>
/// <param name="Content">The content blocks in this message.</param>
/// <param name="Model">The model that generated this message.</param>
/// <param name="StopReason">The reason the assistant stopped generating, if applicable.</param>
public sealed record AssistantMessage(
    IReadOnlyList<IContentBlock> Content,
    string? Model = null,
    string? StopReason = null) : IMessage
{
    /// <inheritdoc />
    public MessageType Type => MessageType.Assistant;

    /// <summary>
    /// Gets the concatenated text content from all text blocks.
    /// </summary>
    public string Text => string.Join("", Content.OfType<TextBlock>().Select(b => b.Text));
}
