namespace Claude.CodeSdk.Mcp;

/// <summary>
/// Configuration for an MCP (Model Context Protocol) server.
/// </summary>
/// <param name="Name">The name identifier for the MCP server.</param>
/// <param name="Command">The command to execute the MCP server.</param>
/// <param name="Args">Optional arguments to pass to the MCP server command.</param>
/// <param name="Env">Optional environment variables for the MCP server process.</param>
public sealed record McpServerConfig(
    string Name,
    string Command,
    IReadOnlyList<string>? Args = null,
    IReadOnlyDictionary<string, string>? Env = null);
