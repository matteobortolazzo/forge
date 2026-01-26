using System.Runtime.CompilerServices;
using Claude.CodeSdk.Messages;

namespace Claude.CodeSdk.Mock;

/// <summary>
/// Mock implementation of <see cref="IClaudeAgentClient"/> for testing.
/// Yields messages from <see cref="MockScenarioProvider"/> with configurable delays.
/// </summary>
public sealed class MockClaudeAgentClient : IClaudeAgentClient
{
    private readonly MockScenarioProvider _scenarioProvider;
    private readonly ClaudeAgentOptions? _defaultOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockClaudeAgentClient"/> class.
    /// </summary>
    /// <param name="scenarioProvider">The scenario provider to use.</param>
    /// <param name="defaultOptions">Optional default options.</param>
    public MockClaudeAgentClient(MockScenarioProvider scenarioProvider, ClaudeAgentOptions? defaultOptions = null)
    {
        _scenarioProvider = scenarioProvider ?? throw new ArgumentNullException(nameof(scenarioProvider));
        _defaultOptions = defaultOptions;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IMessage>> QueryAsync(string prompt, ClaudeAgentOptions? options = null, CancellationToken ct = default)
    {
        var messages = new List<IMessage>();
        await foreach (var message in QueryStreamAsync(prompt, options, ct))
        {
            messages.Add(message);
        }

        return messages.AsReadOnly();
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IMessage> QueryStreamAsync(string prompt, ClaudeAgentOptions? options = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var scenario = _scenarioProvider.GetScenarioForPrompt(prompt);

        foreach (var message in scenario.Messages)
        {
            ct.ThrowIfCancellationRequested();

            // Simulate delay between messages
            if (scenario.DelayBetweenMessagesMs > 0)
            {
                await Task.Delay(scenario.DelayBetweenMessagesMs, ct);
            }

            yield return message;
        }
    }

    /// <inheritdoc />
    public async Task<string> QueryTextAsync(string prompt, ClaudeAgentOptions? options = null, CancellationToken ct = default)
    {
        var messages = await QueryAsync(prompt, options, ct);
        return string.Join("\n", messages.OfType<AssistantMessage>().Select(m => m.Text));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IMessage>> QueryAsync(QueryRequest request, CancellationToken ct = default)
    {
        var mergedOptions = MergeOptions(request.Options);
        return QueryAsync(request.Prompt, mergedOptions, ct);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IMessage> QueryStreamAsync(QueryRequest request, CancellationToken ct = default)
    {
        var mergedOptions = MergeOptions(request.Options);
        return QueryStreamAsync(request.Prompt, mergedOptions, ct);
    }

    /// <inheritdoc />
    public Task<string> QueryTextAsync(QueryRequest request, CancellationToken ct = default)
    {
        var mergedOptions = MergeOptions(request.Options);
        return QueryTextAsync(request.Prompt, mergedOptions, ct);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        // No resources to dispose
        return ValueTask.CompletedTask;
    }

    private ClaudeAgentOptions? MergeOptions(ClaudeAgentOptions? requestOptions)
    {
        if (requestOptions == null)
        {
            return _defaultOptions;
        }

        if (_defaultOptions == null)
        {
            return requestOptions;
        }

        // Request options take precedence
        return requestOptions with
        {
            WorkingDirectory = requestOptions.WorkingDirectory ?? _defaultOptions.WorkingDirectory,
            McpServers = requestOptions.McpServers ?? _defaultOptions.McpServers,
            AllowedTools = requestOptions.AllowedTools ?? _defaultOptions.AllowedTools,
            SystemPrompt = requestOptions.SystemPrompt ?? _defaultOptions.SystemPrompt,
            AppendSystemPrompt = requestOptions.AppendSystemPrompt ?? _defaultOptions.AppendSystemPrompt,
            Model = requestOptions.Model ?? _defaultOptions.Model,
            CliPath = requestOptions.CliPath ?? _defaultOptions.CliPath
        };
    }
}
