namespace Claude.CodeSdk.Permissions;

/// <summary>
/// Delegate for handling tool permission requests.
/// Invoked when a tool is about to be executed, allowing the consumer to approve, deny, or modify the request.
/// </summary>
/// <param name="context">Context information about the tool being invoked.</param>
/// <param name="ct">Cancellation token.</param>
/// <returns>A <see cref="PermissionResult"/> indicating whether to allow or deny the tool execution.</returns>
public delegate ValueTask<PermissionResult> ToolPermissionHandler(
    ToolPermissionContext context,
    CancellationToken ct);
