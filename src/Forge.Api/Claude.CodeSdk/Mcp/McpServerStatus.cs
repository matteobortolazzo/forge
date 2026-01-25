namespace Claude.CodeSdk.Mcp;

/// <summary>
/// Represents the status of an MCP (Model Context Protocol) server.
/// </summary>
/// <param name="Name">The name identifier of the MCP server.</param>
/// <param name="Status">The current connection status of the server.</param>
public sealed record McpServerStatus(string Name, string Status);
