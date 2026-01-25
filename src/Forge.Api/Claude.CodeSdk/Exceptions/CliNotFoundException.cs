namespace Claude.CodeSdk.Exceptions;

/// <summary>
/// Thrown when the Claude Code CLI executable cannot be found.
/// </summary>
public sealed class CliNotFoundException : ClaudeAgentException
{
    /// <summary>
    /// Gets the paths that were searched for the CLI.
    /// </summary>
    public IReadOnlyList<string> SearchedPaths { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliNotFoundException"/> class.
    /// </summary>
    /// <param name="searchedPaths">The paths that were searched.</param>
    public CliNotFoundException(IReadOnlyList<string> searchedPaths)
        : base($"Claude Code CLI not found. Searched paths: {string.Join(", ", searchedPaths)}")
    {
        SearchedPaths = searchedPaths;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliNotFoundException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="searchedPaths">The paths that were searched.</param>
    public CliNotFoundException(string message, IReadOnlyList<string> searchedPaths)
        : base(message)
    {
        SearchedPaths = searchedPaths;
    }
}
