namespace Claude.CodeSdk;

/// <summary>
/// Specifies how the Claude Code CLI handles permission requests.
/// </summary>
public enum PermissionMode
{
    /// <summary>
    /// Default behavior - prompts for permission when needed.
    /// </summary>
    Default,

    /// <summary>
    /// Accept all permission requests without prompting.
    /// Maps to CLI flag: --dangerously-skip-permissions
    /// </summary>
    AcceptAll,

    /// <summary>
    /// Use an allowlist file to determine permissions.
    /// Maps to CLI flag: --allowedTools
    /// </summary>
    Allowlist
}
