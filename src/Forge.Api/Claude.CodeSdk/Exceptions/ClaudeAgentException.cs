namespace Claude.CodeSdk.Exceptions;

/// <summary>
/// Base exception for all Claude Agent SDK errors.
/// </summary>
public class ClaudeAgentException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClaudeAgentException"/> class.
    /// </summary>
    public ClaudeAgentException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaudeAgentException"/> class with a message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ClaudeAgentException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaudeAgentException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ClaudeAgentException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
