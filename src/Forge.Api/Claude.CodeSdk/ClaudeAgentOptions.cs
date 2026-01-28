using Claude.CodeSdk.Mcp;
using Claude.CodeSdk.Permissions;

namespace Claude.CodeSdk;

/// <summary>
/// Configuration options for the Claude Agent client and CLI execution.
/// </summary>
public sealed record ClaudeAgentOptions
{
    /// <summary>
    /// Gets or sets the custom path to the Claude Code CLI executable.
    /// If not set, the SDK will search common locations.
    /// </summary>
    public string? CliPath { get; init; }

    /// <summary>
    /// Gets or sets the working directory for CLI execution.
    /// Defaults to the current directory.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Gets or sets the output format for CLI responses.
    /// Defaults to StreamJson for streaming support.
    /// </summary>
    public OutputFormat OutputFormat { get; init; } = OutputFormat.StreamJson;

    /// <summary>
    /// Gets or sets the permission handling mode.
    /// </summary>
    public PermissionMode PermissionMode { get; init; } = PermissionMode.Default;

    /// <summary>
    /// Gets or sets the list of allowed tools when using <see cref="PermissionMode.Allowlist"/>.
    /// </summary>
    public IReadOnlyList<string>? AllowedTools { get; init; }

    /// <summary>
    /// Gets or sets the maximum number of conversational turns.
    /// Maps to CLI flag: --max-turns
    /// </summary>
    public int? MaxTurns { get; init; }

    /// <summary>
    /// Gets or sets the system prompt to use.
    /// Maps to CLI flag: --system-prompt
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// Gets or sets additional instructions to append to the system prompt.
    /// Maps to CLI flag: --append-system-prompt
    /// </summary>
    public string? AppendSystemPrompt { get; init; }

    /// <summary>
    /// Gets or sets the model to use (e.g., "claude-sonnet-4-20250514").
    /// Maps to CLI flag: --model
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Gets or sets whether to disable streaming output and wait for complete response.
    /// Maps to CLI flag: --print (when true with OutputFormat.Text)
    /// </summary>
    public bool Print { get; init; }

    /// <summary>
    /// Gets or sets the session ID to resume a previous conversation.
    /// Maps to CLI flag: --resume
    /// </summary>
    public string? ResumeSessionId { get; init; }

    /// <summary>
    /// Gets or sets whether to continue in conversation mode.
    /// Maps to CLI flag: --continue
    /// </summary>
    public bool Continue { get; init; }

    /// <summary>
    /// Gets or sets MCP server configurations.
    /// Maps to CLI flag: --mcp-config
    /// </summary>
    public IReadOnlyList<McpServerConfig>? McpServers { get; init; }

    /// <summary>
    /// Gets or sets environment variables to pass to the CLI process.
    /// </summary>
    public IReadOnlyDictionary<string, string>? EnvironmentVariables { get; init; }

    /// <summary>
    /// Gets or sets the timeout for CLI operations in milliseconds.
    /// Defaults to no timeout (null).
    /// </summary>
    public int? TimeoutMs { get; init; }

    /// <summary>
    /// Gets or sets whether to disable the permission prompt entirely.
    /// Maps to CLI flag: --dangerously-skip-permissions
    /// </summary>
    public bool DangerouslySkipPermissions { get; init; }

    /// <summary>
    /// Gets or sets whether to enable verbose output.
    /// Maps to CLI flag: --verbose
    /// </summary>
    public bool Verbose { get; init; }

    /// <summary>
    /// Gets or sets additional CLI arguments to pass.
    /// </summary>
    public IReadOnlyList<string>? AdditionalArgs { get; init; }

    /// <summary>
    /// Gets or sets the handler for tool permission requests.
    /// When set, this handler is invoked before each tool execution, allowing the consumer
    /// to approve, deny, or modify tool inputs.
    /// </summary>
    public ToolPermissionHandler? ToolPermissionHandler { get; init; }

    /// <summary>
    /// Gets or sets the timeout in milliseconds for tool permission handler responses.
    /// Defaults to 60000 (60 seconds).
    /// </summary>
    public int ToolPermissionTimeoutMs { get; init; } = 60000;
}
