namespace Claude.CodeSdk.Messages;

/// <summary>
/// Specifies the type of message in the CLI output stream.
/// </summary>
public enum MessageType
{
    /// <summary>
    /// A message from the assistant (Claude).
    /// </summary>
    Assistant,

    /// <summary>
    /// A message representing user input.
    /// </summary>
    User,

    /// <summary>
    /// System-level metadata about the session.
    /// </summary>
    System,

    /// <summary>
    /// The final result message containing usage statistics.
    /// </summary>
    Result,

    /// <summary>
    /// A streaming event during real-time output.
    /// </summary>
    StreamEvent
}
