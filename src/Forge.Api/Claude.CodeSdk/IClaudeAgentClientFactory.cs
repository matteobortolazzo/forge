namespace Claude.CodeSdk;

/// <summary>
/// Factory for creating <see cref="IClaudeAgentClient"/> instances.
/// </summary>
public interface IClaudeAgentClientFactory
{
    /// <summary>
    /// Creates a new <see cref="IClaudeAgentClient"/> instance.
    /// </summary>
    /// <param name="options">Optional configuration options for the client.</param>
    /// <returns>A new client instance.</returns>
    IClaudeAgentClient Create(ClaudeAgentOptions? options = null);
}
