using System.Text.Json;

namespace Claude.CodeSdk.Messages;

/// <summary>
/// Represents a streaming event during real-time output.
/// </summary>
/// <param name="EventType">The type of streaming event (e.g., "content_block_delta", "message_start").</param>
/// <param name="Data">The raw JSON data associated with the event.</param>
public sealed record StreamEvent(string EventType, JsonElement Data) : IMessage
{
    /// <inheritdoc />
    public MessageType Type => MessageType.StreamEvent;
}
