using System.Text.Json;

namespace Claude.CodeSdk.Permissions;

/// <summary>
/// Represents the result of a tool permission request.
/// Use factory methods <see cref="Allow"/> and <see cref="Deny"/> to create instances.
/// </summary>
public abstract record PermissionResult
{
    private PermissionResult() { }

    /// <summary>
    /// Creates a result that allows the tool to execute.
    /// </summary>
    /// <param name="updatedInput">Optional modified input to pass to the tool.</param>
    /// <returns>An allow result.</returns>
    public static PermissionResult Allow(JsonElement? updatedInput = null)
        => new AllowResult(updatedInput);

    /// <summary>
    /// Creates a result that denies the tool execution.
    /// </summary>
    /// <param name="message">Message explaining why the tool was denied.</param>
    /// <param name="interrupt">If true, throws <see cref="Exceptions.ToolDeniedException"/> and aborts the session.</param>
    /// <returns>A deny result.</returns>
    public static PermissionResult Deny(string message, bool interrupt = false)
        => new DenyResult(message, interrupt);

    /// <summary>
    /// Represents a result that allows tool execution.
    /// </summary>
    /// <param name="UpdatedInput">Optional modified input to use instead of the original.</param>
    public sealed record AllowResult(JsonElement? UpdatedInput) : PermissionResult;

    /// <summary>
    /// Represents a result that denies tool execution.
    /// </summary>
    /// <param name="Message">Message explaining why the tool was denied.</param>
    /// <param name="Interrupt">If true, the session should be aborted.</param>
    public sealed record DenyResult(string Message, bool Interrupt) : PermissionResult;
}
