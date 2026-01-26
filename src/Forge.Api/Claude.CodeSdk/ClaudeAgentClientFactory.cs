namespace Claude.CodeSdk;

/// <summary>
/// Default factory for creating <see cref="ClaudeAgentClient"/> instances.
/// </summary>
public sealed class ClaudeAgentClientFactory : IClaudeAgentClientFactory
{
    /// <inheritdoc />
    public IClaudeAgentClient Create(ClaudeAgentOptions? options = null)
        => new ClaudeAgentClient(options);
}
