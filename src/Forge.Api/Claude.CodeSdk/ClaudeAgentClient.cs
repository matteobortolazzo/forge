using System.Runtime.CompilerServices;
using Claude.CodeSdk.ContentBlocks;
using Claude.CodeSdk.Internal;
using Claude.CodeSdk.Messages;

namespace Claude.CodeSdk;

/// <summary>
/// Client for interacting with Claude Code CLI programmatically.
/// </summary>
public sealed class ClaudeAgentClient : IClaudeAgentClient
{
    private readonly ClaudeAgentOptions _defaultOptions;
    private readonly string _cliPath;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaudeAgentClient"/> class.
    /// </summary>
    /// <param name="options">Default options for all queries. Can be overridden per-query.</param>
    public ClaudeAgentClient(ClaudeAgentOptions? options = null)
    {
        _defaultOptions = options ?? new ClaudeAgentOptions();
        _cliPath = CliLocator.FindCli(_defaultOptions.CliPath);
    }

    /// <summary>
    /// Sends a query and returns all messages.
    /// </summary>
    /// <param name="prompt">The prompt to send to Claude.</param>
    /// <param name="options">Optional execution options that override defaults.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of all messages from the conversation.</returns>
    public async Task<IReadOnlyList<IMessage>> QueryAsync(
        string prompt,
        ClaudeAgentOptions? options = null,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var messages = new List<IMessage>();

        await foreach (var message in QueryStreamAsync(prompt, options, ct))
        {
            messages.Add(message);
        }

        return messages;
    }

    /// <summary>
    /// Sends a query and streams messages as they arrive.
    /// </summary>
    /// <param name="prompt">The prompt to send to Claude.</param>
    /// <param name="options">Optional execution options that override defaults.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async enumerable of messages.</returns>
    public async IAsyncEnumerable<IMessage> QueryStreamAsync(
        string prompt,
        ClaudeAgentOptions? options = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var effectiveOptions = MergeOptions(_defaultOptions, options);
        var args = CommandBuilder.BuildArguments(prompt, effectiveOptions);

        await using var process = CliProcess.Start(
            _cliPath,
            args,
            effectiveOptions.WorkingDirectory,
            effectiveOptions.EnvironmentVariables,
            ct);

        await foreach (var line in process.ReadLinesAsync(ct))
        {
            if (MessageParser.TryParse(line, out var message) && message is not null)
            {
                yield return message;
            }
        }

        await process.WaitForExitAsync(ct);
    }

    /// <summary>
    /// Sends a query and returns only the text content from assistant messages.
    /// </summary>
    /// <param name="prompt">The prompt to send to Claude.</param>
    /// <param name="options">Optional execution options that override defaults.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The concatenated text response from the assistant.</returns>
    public async Task<string> QueryTextAsync(
        string prompt,
        ClaudeAgentOptions? options = null,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var messages = await QueryAsync(prompt, options, ct);

        var textBlocks = messages
            .OfType<AssistantMessage>()
            .SelectMany(m => m.Content)
            .OfType<TextBlock>()
            .Select(b => b.Text);

        return string.Join("", textBlocks);
    }

    /// <summary>
    /// Sends a query request and returns all messages.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of all messages from the conversation.</returns>
    public Task<IReadOnlyList<IMessage>> QueryAsync(QueryRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return QueryAsync(request.Prompt, request.Options, ct);
    }

    /// <summary>
    /// Sends a query request and streams messages as they arrive.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async enumerable of messages.</returns>
    public IAsyncEnumerable<IMessage> QueryStreamAsync(QueryRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return QueryStreamAsync(request.Prompt, request.Options, ct);
    }

    /// <summary>
    /// Sends a query request and returns only the text content.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The concatenated text response from the assistant.</returns>
    public Task<string> QueryTextAsync(QueryRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return QueryTextAsync(request.Prompt, request.Options, ct);
    }

    private static ClaudeAgentOptions MergeOptions(ClaudeAgentOptions defaults, ClaudeAgentOptions? overrides)
    {
        if (overrides is null)
        {
            return defaults;
        }

        return new ClaudeAgentOptions
        {
            CliPath = overrides.CliPath ?? defaults.CliPath,
            WorkingDirectory = overrides.WorkingDirectory ?? defaults.WorkingDirectory,
            OutputFormat = overrides.OutputFormat != OutputFormat.StreamJson ? overrides.OutputFormat : defaults.OutputFormat,
            PermissionMode = overrides.PermissionMode != PermissionMode.Default ? overrides.PermissionMode : defaults.PermissionMode,
            AllowedTools = overrides.AllowedTools ?? defaults.AllowedTools,
            MaxTurns = overrides.MaxTurns ?? defaults.MaxTurns,
            SystemPrompt = overrides.SystemPrompt ?? defaults.SystemPrompt,
            AppendSystemPrompt = overrides.AppendSystemPrompt ?? defaults.AppendSystemPrompt,
            Model = overrides.Model ?? defaults.Model,
            Print = overrides.Print || defaults.Print,
            ResumeSessionId = overrides.ResumeSessionId ?? defaults.ResumeSessionId,
            Continue = overrides.Continue || defaults.Continue,
            McpServers = overrides.McpServers ?? defaults.McpServers,
            EnvironmentVariables = MergeEnvironmentVariables(defaults.EnvironmentVariables, overrides.EnvironmentVariables),
            TimeoutMs = overrides.TimeoutMs ?? defaults.TimeoutMs,
            DangerouslySkipPermissions = overrides.DangerouslySkipPermissions || defaults.DangerouslySkipPermissions,
            Verbose = overrides.Verbose || defaults.Verbose,
            AdditionalArgs = MergeAdditionalArgs(defaults.AdditionalArgs, overrides.AdditionalArgs)
        };
    }

    private static IReadOnlyDictionary<string, string>? MergeEnvironmentVariables(
        IReadOnlyDictionary<string, string>? defaults,
        IReadOnlyDictionary<string, string>? overrides)
    {
        if (defaults is null) return overrides;
        if (overrides is null) return defaults;

        var merged = new Dictionary<string, string>(defaults);
        foreach (var (key, value) in overrides)
        {
            merged[key] = value;
        }
        return merged;
    }

    private static IReadOnlyList<string>? MergeAdditionalArgs(
        IReadOnlyList<string>? defaults,
        IReadOnlyList<string>? overrides)
    {
        if (defaults is null) return overrides;
        if (overrides is null) return defaults;

        return [..defaults, ..overrides];
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _disposed = true;
        return ValueTask.CompletedTask;
    }
}
