namespace Claude.CodeSdk.Messages;

/// <summary>
/// Base interface for all messages in the CLI output stream.
/// </summary>
public interface IMessage
{
    /// <summary>
    /// Gets the type of message.
    /// </summary>
    MessageType Type { get; }
}
