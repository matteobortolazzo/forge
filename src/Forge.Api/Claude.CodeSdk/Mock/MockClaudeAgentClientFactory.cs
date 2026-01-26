namespace Claude.CodeSdk.Mock;

/// <summary>
/// Factory for creating <see cref="MockClaudeAgentClient"/> instances.
/// </summary>
public sealed class MockClaudeAgentClientFactory : IClaudeAgentClientFactory
{
    private readonly MockScenarioProvider _scenarioProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockClaudeAgentClientFactory"/> class.
    /// </summary>
    /// <param name="scenarioProvider">The scenario provider to use.</param>
    public MockClaudeAgentClientFactory(MockScenarioProvider scenarioProvider)
    {
        _scenarioProvider = scenarioProvider ?? throw new ArgumentNullException(nameof(scenarioProvider));
    }

    /// <inheritdoc />
    public IClaudeAgentClient Create(ClaudeAgentOptions? options = null)
    {
        return new MockClaudeAgentClient(_scenarioProvider, options);
    }
}
