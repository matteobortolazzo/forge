using System.Text.Json;
using Claude.CodeSdk.ContentBlocks;
using Claude.CodeSdk.Exceptions;
using Claude.CodeSdk.Mcp;
using Claude.CodeSdk.Messages;

namespace Claude.CodeSdk.Internal;

/// <summary>
/// Parses JSON messages from the CLI output stream.
/// </summary>
internal static class MessageParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Parses a JSON line into an IMessage.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>The parsed message.</returns>
    /// <exception cref="JsonDecodeException">Thrown when parsing fails.</exception>
    public static IMessage Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var type = GetStringProperty(root, "type")?.ToLowerInvariant();

            return type switch
            {
                "assistant" => ParseAssistantMessage(root),
                "user" => ParseUserMessage(root),
                "system" => ParseSystemMessage(root),
                "result" => ParseResultMessage(root),
                _ => ParseStreamEvent(root, type ?? "unknown")
            };
        }
        catch (JsonException ex)
        {
            throw new JsonDecodeException(json, ex);
        }
    }

    /// <summary>
    /// Tries to parse a JSON line into an IMessage.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <param name="message">The parsed message if successful.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    public static bool TryParse(string json, out IMessage? message)
    {
        message = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            message = Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static AssistantMessage ParseAssistantMessage(JsonElement root)
    {
        var content = ParseContentBlocks(root);
        var model = GetStringProperty(root, "model");
        var stopReason = GetStringProperty(root, "stop_reason");

        return new AssistantMessage(content, model, stopReason);
    }

    private static UserMessage ParseUserMessage(JsonElement root)
    {
        var content = ParseContentBlocks(root);
        return new UserMessage(content);
    }

    private static SystemMessage ParseSystemMessage(JsonElement root)
    {
        var sessionId = GetStringProperty(root, "session_id");
        var mcpServers = ParseMcpServers(root);

        return new SystemMessage(sessionId, mcpServers);
    }

    private static ResultMessage ParseResultMessage(JsonElement root)
    {
        var usage = ParseUsage(root);
        var sessionId = GetStringProperty(root, "session_id");
        // Try both "total_cost_usd" (actual CLI output) and "cost_usd" (for compatibility)
        var costUsd = GetDecimalProperty(root, "total_cost_usd") ?? GetDecimalProperty(root, "cost_usd");
        var durationMs = GetLongProperty(root, "duration_ms");
        var numTurns = GetIntProperty(root, "num_turns");

        return new ResultMessage(usage, sessionId, costUsd, durationMs, numTurns);
    }

    private static StreamEvent ParseStreamEvent(JsonElement root, string eventType)
    {
        // Clone the element so it survives after the JsonDocument is disposed
        return new StreamEvent(eventType, root.Clone());
    }

    private static IReadOnlyList<IContentBlock> ParseContentBlocks(JsonElement root)
    {
        var blocks = new List<IContentBlock>();

        if (!root.TryGetProperty("content", out var contentArray))
        {
            // Try parsing "message" property which may contain content
            if (root.TryGetProperty("message", out var messageElement) &&
                messageElement.TryGetProperty("content", out contentArray))
            {
                // Found content in nested message
            }
            else
            {
                return blocks;
            }
        }

        if (contentArray.ValueKind != JsonValueKind.Array)
        {
            return blocks;
        }

        foreach (var item in contentArray.EnumerateArray())
        {
            var block = ParseContentBlock(item);
            if (block is not null)
            {
                blocks.Add(block);
            }
        }

        return blocks;
    }

    private static IContentBlock? ParseContentBlock(JsonElement element)
    {
        var type = GetStringProperty(element, "type")?.ToLowerInvariant();

        return type switch
        {
            "text" => ParseTextBlock(element),
            "tool_use" => ParseToolUseBlock(element),
            "tool_result" => ParseToolResultBlock(element),
            _ => null
        };
    }

    private static TextBlock ParseTextBlock(JsonElement element)
    {
        var text = GetStringProperty(element, "text") ?? string.Empty;
        return new TextBlock(text);
    }

    private static ToolUseBlock ParseToolUseBlock(JsonElement element)
    {
        var id = GetStringProperty(element, "id") ?? string.Empty;
        var name = GetStringProperty(element, "name") ?? string.Empty;

        JsonElement input = default;
        if (element.TryGetProperty("input", out var inputElement))
        {
            input = inputElement.Clone();
        }

        return new ToolUseBlock(id, name, input);
    }

    private static ToolResultBlock ParseToolResultBlock(JsonElement element)
    {
        var toolUseId = GetStringProperty(element, "tool_use_id") ?? string.Empty;
        var content = GetStringProperty(element, "content") ?? string.Empty;
        var isError = GetBoolProperty(element, "is_error") ?? false;

        return new ToolResultBlock(toolUseId, content, isError);
    }

    private static IReadOnlyList<McpServerStatus>? ParseMcpServers(JsonElement root)
    {
        if (!root.TryGetProperty("mcp_servers", out var serversArray))
        {
            return null;
        }

        if (serversArray.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var servers = new List<McpServerStatus>();

        foreach (var item in serversArray.EnumerateArray())
        {
            var name = GetStringProperty(item, "name") ?? string.Empty;
            var status = GetStringProperty(item, "status") ?? string.Empty;
            servers.Add(new McpServerStatus(name, status));
        }

        return servers;
    }

    private static Usage ParseUsage(JsonElement root)
    {
        if (!root.TryGetProperty("usage", out var usageElement))
        {
            return new Usage(0, 0);
        }

        var inputTokens = GetIntProperty(usageElement, "input_tokens") ?? 0;
        var outputTokens = GetIntProperty(usageElement, "output_tokens") ?? 0;
        var cacheRead = GetIntProperty(usageElement, "cache_read_input_tokens") ?? 0;
        var cacheCreation = GetIntProperty(usageElement, "cache_creation_input_tokens") ?? 0;

        return new Usage(inputTokens, outputTokens, cacheRead, cacheCreation);
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }

    private static int? GetIntProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
        {
            return prop.GetInt32();
        }
        return null;
    }

    private static long? GetLongProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
        {
            return prop.GetInt64();
        }
        return null;
    }

    private static decimal? GetDecimalProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
        {
            return prop.GetDecimal();
        }
        return null;
    }

    private static bool? GetBoolProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            return prop.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null
            };
        }
        return null;
    }
}
