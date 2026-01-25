using System.Text;
using System.Text.Json;

namespace Claude.CodeSdk.Internal;

/// <summary>
/// Builds CLI command arguments from options.
/// </summary>
internal static class CommandBuilder
{
    /// <summary>
    /// Builds the arguments string for the CLI.
    /// </summary>
    /// <param name="prompt">The prompt to send.</param>
    /// <param name="options">The execution options.</param>
    /// <returns>The arguments list.</returns>
    public static IReadOnlyList<string> BuildArguments(string prompt, ClaudeAgentOptions options)
    {
        var args = new List<string>();

        // Add print flag for non-interactive mode
        if (options.Print || options.OutputFormat != OutputFormat.Text)
        {
            args.Add("--print");
        }

        // Output format
        if (options.OutputFormat != OutputFormat.Text)
        {
            args.Add("--output-format");
            args.Add(options.OutputFormat switch
            {
                OutputFormat.Json => "json",
                OutputFormat.StreamJson => "stream-json",
                _ => "text"
            });
        }

        // Permission handling
        if (options.DangerouslySkipPermissions || options.PermissionMode == PermissionMode.AcceptAll)
        {
            args.Add("--dangerously-skip-permissions");
        }

        // Allowed tools
        if (options.AllowedTools is { Count: > 0 })
        {
            foreach (var tool in options.AllowedTools)
            {
                args.Add("--allowedTools");
                args.Add(tool);
            }
        }

        // Max turns
        if (options.MaxTurns.HasValue)
        {
            args.Add("--max-turns");
            args.Add(options.MaxTurns.Value.ToString());
        }

        // System prompt
        if (!string.IsNullOrWhiteSpace(options.SystemPrompt))
        {
            args.Add("--system-prompt");
            args.Add(options.SystemPrompt);
        }

        // Append system prompt
        if (!string.IsNullOrWhiteSpace(options.AppendSystemPrompt))
        {
            args.Add("--append-system-prompt");
            args.Add(options.AppendSystemPrompt);
        }

        // Model
        if (!string.IsNullOrWhiteSpace(options.Model))
        {
            args.Add("--model");
            args.Add(options.Model);
        }

        // Resume session
        if (!string.IsNullOrWhiteSpace(options.ResumeSessionId))
        {
            args.Add("--resume");
            args.Add(options.ResumeSessionId);
        }

        // Continue flag
        if (options.Continue)
        {
            args.Add("--continue");
        }

        // Verbose
        if (options.Verbose)
        {
            args.Add("--verbose");
        }

        // MCP servers configuration
        if (options.McpServers is { Count: > 0 })
        {
            var mcpConfig = BuildMcpConfig(options.McpServers);
            args.Add("--mcp-config");
            args.Add(mcpConfig);
        }

        // Additional arguments
        if (options.AdditionalArgs is { Count: > 0 })
        {
            args.AddRange(options.AdditionalArgs);
        }

        // The prompt must be the last argument
        args.Add("--prompt");
        args.Add(prompt);

        return args;
    }

    private static string BuildMcpConfig(IReadOnlyList<Mcp.McpServerConfig> servers)
    {
        var config = new Dictionary<string, object>();
        var mcpServers = new Dictionary<string, object>();

        foreach (var server in servers)
        {
            var serverConfig = new Dictionary<string, object>
            {
                ["command"] = server.Command
            };

            if (server.Args is { Count: > 0 })
            {
                serverConfig["args"] = server.Args;
            }

            if (server.Env is { Count: > 0 })
            {
                serverConfig["env"] = server.Env;
            }

            mcpServers[server.Name] = serverConfig;
        }

        config["mcpServers"] = mcpServers;

        return JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }
}
