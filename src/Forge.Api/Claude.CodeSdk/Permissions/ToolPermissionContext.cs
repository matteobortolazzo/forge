using System.Text.Json;

namespace Claude.CodeSdk.Permissions;

/// <summary>
/// Provides context information for a tool permission request.
/// </summary>
/// <param name="ToolName">The name of the tool being invoked.</param>
/// <param name="ToolUseId">The unique identifier for this tool use.</param>
/// <param name="Input">The input parameters for the tool as a JSON element.</param>
/// <param name="WorkingDirectory">The working directory for the CLI execution, if set.</param>
/// <param name="SessionId">The current session ID, if available.</param>
public sealed record ToolPermissionContext(
    string ToolName,
    string ToolUseId,
    JsonElement Input,
    string? WorkingDirectory,
    string? SessionId);
