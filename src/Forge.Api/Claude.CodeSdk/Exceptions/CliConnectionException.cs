namespace Claude.CodeSdk.Exceptions;

/// <summary>
/// Thrown when the SDK fails to establish a connection with the CLI process.
/// </summary>
public sealed class CliConnectionException : ClaudeAgentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CliConnectionException"/> class.
    /// </summary>
    public CliConnectionException()
        : base("Failed to establish connection with Claude Code CLI.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliConnectionException"/> class with a message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public CliConnectionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliConnectionException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public CliConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
