namespace Claude.CodeSdk.Exceptions;

/// <summary>
/// Thrown when the CLI process exits with a non-zero exit code.
/// </summary>
public sealed class ProcessException : ClaudeAgentException
{
    /// <summary>
    /// Gets the exit code of the process.
    /// </summary>
    public int ExitCode { get; }

    /// <summary>
    /// Gets the standard error output from the process.
    /// </summary>
    public string Stderr { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessException"/> class.
    /// </summary>
    /// <param name="exitCode">The exit code of the process.</param>
    /// <param name="stderr">The standard error output.</param>
    public ProcessException(int exitCode, string stderr)
        : base($"CLI process exited with code {exitCode}: {stderr}")
    {
        ExitCode = exitCode;
        Stderr = stderr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="exitCode">The exit code of the process.</param>
    /// <param name="stderr">The standard error output.</param>
    public ProcessException(string message, int exitCode, string stderr) : base(message)
    {
        ExitCode = exitCode;
        Stderr = stderr;
    }
}
