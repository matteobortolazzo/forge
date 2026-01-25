# Claude.CodeSdk - C# SDK Documentation

This document provides comprehensive documentation for the Claude.CodeSdk library, a C# wrapper for the Claude Code CLI.

## Overview

Claude.CodeSdk enables programmatic interaction with Claude Code CLI from .NET applications. It spawns the CLI as a subprocess, communicates via stdin/stdout with NDJSON streaming, and parses messages into strongly-typed objects.

**Target Framework:** .NET 10
**Dependencies:** None (uses System.Text.Json)
**Namespace:** `Claude.CodeSdk`

## Quick Start

```csharp
using Claude.CodeSdk;

// Basic usage
await using var client = new ClaudeAgentClient();
var response = await client.QueryTextAsync("What is 2+2?");
Console.WriteLine(response);

// With options
await using var client = new ClaudeAgentClient(new ClaudeAgentOptions
{
    MaxTurns = 5,
    DangerouslySkipPermissions = true,
    WorkingDirectory = "/path/to/project"
});

// Streaming
await foreach (var message in client.QueryStreamAsync("Explain async/await"))
{
    if (message is AssistantMessage assistant)
    {
        Console.Write(assistant.Text);
    }
}
```

## Project Structure

```
Claude.CodeSdk/
├── ClaudeAgentClient.cs          # Main public API
├── ClaudeAgentOptions.cs         # Configuration options
├── QueryRequest.cs               # Request wrapper
├── PermissionMode.cs             # Permission enum
├── OutputFormat.cs               # Output format enum
├── ContentBlocks/
│   ├── IContentBlock.cs          # Base interface
│   ├── ContentBlockType.cs       # Block type enum
│   ├── TextBlock.cs              # Text content
│   ├── ToolUseBlock.cs           # Tool invocation
│   └── ToolResultBlock.cs        # Tool result
├── Messages/
│   ├── IMessage.cs               # Base interface
│   ├── MessageType.cs            # Message type enum
│   ├── AssistantMessage.cs       # Claude responses
│   ├── UserMessage.cs            # User inputs
│   ├── SystemMessage.cs          # Session metadata
│   ├── ResultMessage.cs          # Final stats
│   └── StreamEvent.cs            # Streaming events
├── Mcp/
│   ├── McpServerStatus.cs        # Server status
│   └── McpServerConfig.cs        # Server config
├── Exceptions/
│   ├── ClaudeAgentException.cs   # Base exception
│   ├── CliNotFoundException.cs   # CLI not found
│   ├── CliConnectionException.cs # Connection failure
│   ├── ProcessException.cs       # Non-zero exit
│   └── JsonDecodeException.cs    # Parse failure
└── Internal/
    ├── CliLocator.cs             # Find CLI executable
    ├── CommandBuilder.cs         # Build CLI args
    ├── MessageParser.cs          # JSON parsing
    └── CliProcess.cs             # Process management
```

## Public API Reference

### ClaudeAgentClient

The main entry point for interacting with Claude Code CLI.

```csharp
public sealed class ClaudeAgentClient : IAsyncDisposable
{
    // Constructor
    public ClaudeAgentClient(ClaudeAgentOptions? options = null);

    // Query methods - return all messages
    public Task<IReadOnlyList<IMessage>> QueryAsync(
        string prompt,
        ClaudeAgentOptions? options = null,
        CancellationToken ct = default);

    public Task<IReadOnlyList<IMessage>> QueryAsync(
        QueryRequest request,
        CancellationToken ct = default);

    // Streaming methods - yield messages as they arrive
    public IAsyncEnumerable<IMessage> QueryStreamAsync(
        string prompt,
        ClaudeAgentOptions? options = null,
        CancellationToken ct = default);

    public IAsyncEnumerable<IMessage> QueryStreamAsync(
        QueryRequest request,
        CancellationToken ct = default);

    // Text-only methods - return concatenated text from AssistantMessages
    public Task<string> QueryTextAsync(
        string prompt,
        ClaudeAgentOptions? options = null,
        CancellationToken ct = default);

    public Task<string> QueryTextAsync(
        QueryRequest request,
        CancellationToken ct = default);

    // Disposal
    public ValueTask DisposeAsync();
}
```

**Usage Patterns:**

```csharp
// Pattern 1: Simple text query
await using var client = new ClaudeAgentClient();
string answer = await client.QueryTextAsync("What is the capital of France?");

// Pattern 2: Full message access
var messages = await client.QueryAsync("Analyze this code");
foreach (var msg in messages)
{
    switch (msg)
    {
        case AssistantMessage assistant:
            Console.WriteLine($"Assistant: {assistant.Text}");
            break;
        case ResultMessage result:
            Console.WriteLine($"Tokens: {result.Usage.TotalTokens}");
            break;
    }
}

// Pattern 3: Streaming with tool use detection
await foreach (var msg in client.QueryStreamAsync("Create a file"))
{
    if (msg is AssistantMessage assistant)
    {
        foreach (var block in assistant.Content)
        {
            if (block is TextBlock text)
                Console.Write(text.Text);
            else if (block is ToolUseBlock tool)
                Console.WriteLine($"[Tool: {tool.Name}]");
        }
    }
}

// Pattern 4: With per-query options override
var result = await client.QueryTextAsync("Fix the bug", new ClaudeAgentOptions
{
    MaxTurns = 10,
    WorkingDirectory = "/project/src"
});
```

### ClaudeAgentOptions

Configuration for client and per-query execution.

```csharp
public sealed record ClaudeAgentOptions
{
    // CLI location
    string? CliPath { get; init; }              // Custom CLI path
    string? WorkingDirectory { get; init; }     // Execution directory

    // Output control
    OutputFormat OutputFormat { get; init; }    // Default: StreamJson
    bool Print { get; init; }                   // Non-interactive mode
    bool Verbose { get; init; }                 // Verbose output

    // Permission handling
    PermissionMode PermissionMode { get; init; } // Default: Default
    bool DangerouslySkipPermissions { get; init; } // Skip all prompts
    IReadOnlyList<string>? AllowedTools { get; init; } // Tool allowlist

    // Model configuration
    string? Model { get; init; }                // Model ID
    int? MaxTurns { get; init; }                // Max conversation turns
    int? TimeoutMs { get; init; }               // Operation timeout

    // Prompt customization
    string? SystemPrompt { get; init; }         // Custom system prompt
    string? AppendSystemPrompt { get; init; }   // Append to system prompt

    // Session management
    string? ResumeSessionId { get; init; }      // Resume session
    bool Continue { get; init; }                // Continue conversation

    // MCP servers
    IReadOnlyList<McpServerConfig>? McpServers { get; init; }

    // Environment
    IReadOnlyDictionary<string, string>? EnvironmentVariables { get; init; }
    IReadOnlyList<string>? AdditionalArgs { get; init; }
}
```

**CLI Flag Mapping:**

| Property | CLI Flag |
|----------|----------|
| `DangerouslySkipPermissions` | `--dangerously-skip-permissions` |
| `MaxTurns` | `--max-turns` |
| `SystemPrompt` | `--system-prompt` |
| `AppendSystemPrompt` | `--append-system-prompt` |
| `Model` | `--model` |
| `ResumeSessionId` | `--resume` |
| `Continue` | `--continue` |
| `Verbose` | `--verbose` |
| `AllowedTools` | `--allowedTools` (multiple) |
| `OutputFormat` | `--output-format` |
| `Print` | `--print` |

### QueryRequest

API-compatible request wrapper.

```csharp
public sealed record QueryRequest
{
    required string Prompt { get; init; }
    ClaudeAgentOptions? Options { get; init; }
}
```

## Message Types

### IMessage Interface

Base interface for all message types.

```csharp
public interface IMessage
{
    MessageType Type { get; }
}

public enum MessageType
{
    Assistant,    // Claude response
    User,         // User input
    System,       // Session metadata
    Result,       // Final statistics
    StreamEvent   // Streaming event
}
```

### AssistantMessage

Messages from Claude containing content blocks.

```csharp
public sealed record AssistantMessage(
    IReadOnlyList<IContentBlock> Content,
    string? Model = null,
    string? StopReason = null) : IMessage
{
    MessageType Type => MessageType.Assistant;
    string Text { get; } // Concatenated text from all TextBlocks
}
```

**Example Processing:**

```csharp
if (message is AssistantMessage assistant)
{
    // Get all text
    Console.WriteLine(assistant.Text);

    // Or process individual blocks
    foreach (var block in assistant.Content)
    {
        switch (block)
        {
            case TextBlock text:
                Console.Write(text.Text);
                break;
            case ToolUseBlock tool:
                Console.WriteLine($"Tool: {tool.Name}, Input: {tool.Input}");
                break;
            case ToolResultBlock result:
                Console.WriteLine($"Result: {result.Content}, Error: {result.IsError}");
                break;
        }
    }
}
```

### UserMessage

Represents user input in the conversation.

```csharp
public sealed record UserMessage(IReadOnlyList<IContentBlock> Content) : IMessage
{
    MessageType Type => MessageType.User;
    string Text { get; } // Concatenated text
}
```

### SystemMessage

Session metadata and MCP server status.

```csharp
public sealed record SystemMessage(
    string? SessionId = null,
    IReadOnlyList<McpServerStatus>? McpServers = null) : IMessage
{
    MessageType Type => MessageType.System;
}
```

### ResultMessage

Final message with usage statistics.

```csharp
public sealed record ResultMessage(
    Usage Usage,
    string? SessionId = null,
    decimal? CostUsd = null,
    long? DurationMs = null,
    int? NumTurns = null) : IMessage
{
    MessageType Type => MessageType.Result;
}

public sealed record Usage(
    int InputTokens,
    int OutputTokens,
    int CacheReadInputTokens = 0,
    int CacheCreationInputTokens = 0)
{
    int TotalTokens { get; } // InputTokens + OutputTokens
}
```

**Example:**

```csharp
var messages = await client.QueryAsync("Hello");
var result = messages.OfType<ResultMessage>().FirstOrDefault();
if (result is not null)
{
    Console.WriteLine($"Input tokens: {result.Usage.InputTokens}");
    Console.WriteLine($"Output tokens: {result.Usage.OutputTokens}");
    Console.WriteLine($"Cost: ${result.CostUsd}");
    Console.WriteLine($"Duration: {result.DurationMs}ms");
}
```

### StreamEvent

Raw streaming events during real-time output.

```csharp
public sealed record StreamEvent(string EventType, JsonElement Data) : IMessage
{
    MessageType Type => MessageType.StreamEvent;
}
```

## Content Blocks

### IContentBlock Interface

```csharp
public interface IContentBlock
{
    ContentBlockType Type { get; }
}

public enum ContentBlockType
{
    Text,
    ToolUse,
    ToolResult
}
```

### TextBlock

Plain text content.

```csharp
public sealed record TextBlock(string Text) : IContentBlock
{
    ContentBlockType Type => ContentBlockType.Text;
}
```

### ToolUseBlock

Tool invocation request.

```csharp
public sealed record ToolUseBlock(
    string Id,
    string Name,
    JsonElement Input) : IContentBlock
{
    ContentBlockType Type => ContentBlockType.ToolUse;
}
```

**Example - Processing Tool Calls:**

```csharp
foreach (var block in assistant.Content.OfType<ToolUseBlock>())
{
    Console.WriteLine($"Tool ID: {block.Id}");
    Console.WriteLine($"Tool Name: {block.Name}");

    // Access input parameters
    if (block.Input.TryGetProperty("file_path", out var path))
    {
        Console.WriteLine($"File: {path.GetString()}");
    }
}
```

### ToolResultBlock

Result from tool execution.

```csharp
public sealed record ToolResultBlock(
    string ToolUseId,
    string Content,
    bool IsError = false) : IContentBlock
{
    ContentBlockType Type => ContentBlockType.ToolResult;
}
```

## MCP Configuration

### McpServerConfig

Configure MCP servers for the CLI.

```csharp
public sealed record McpServerConfig(
    string Name,
    string Command,
    IReadOnlyList<string>? Args = null,
    IReadOnlyDictionary<string, string>? Env = null);
```

### McpServerStatus

Status of connected MCP servers.

```csharp
public sealed record McpServerStatus(string Name, string Status);
```

**Example - Configuring MCP Servers:**

```csharp
var options = new ClaudeAgentOptions
{
    McpServers =
    [
        new McpServerConfig(
            Name: "filesystem",
            Command: "npx",
            Args: ["-y", "@anthropic/mcp-server-filesystem", "/allowed/path"]
        ),
        new McpServerConfig(
            Name: "github",
            Command: "npx",
            Args: ["-y", "@anthropic/mcp-server-github"],
            Env: new Dictionary<string, string>
            {
                ["GITHUB_TOKEN"] = Environment.GetEnvironmentVariable("GITHUB_TOKEN")!
            }
        )
    ]
};

await using var client = new ClaudeAgentClient(options);
```

## Exceptions

### Exception Hierarchy

```
ClaudeAgentException (base)
├── CliNotFoundException      # CLI executable not found
├── CliConnectionException    # Failed to start/connect to CLI
├── ProcessException          # CLI exited with non-zero code
└── JsonDecodeException       # Failed to parse CLI output
```

### CliNotFoundException

Thrown when the CLI cannot be located.

```csharp
public sealed class CliNotFoundException : ClaudeAgentException
{
    IReadOnlyList<string> SearchedPaths { get; }
}
```

### ProcessException

Thrown when CLI exits with error.

```csharp
public sealed class ProcessException : ClaudeAgentException
{
    int ExitCode { get; }
    string Stderr { get; }
}
```

### JsonDecodeException

Thrown when JSON parsing fails.

```csharp
public sealed class JsonDecodeException : ClaudeAgentException
{
    string RawData { get; }
}
```

**Example - Error Handling:**

```csharp
try
{
    await using var client = new ClaudeAgentClient();
    var result = await client.QueryTextAsync("Hello");
}
catch (CliNotFoundException ex)
{
    Console.WriteLine($"CLI not found. Searched: {string.Join(", ", ex.SearchedPaths)}");
}
catch (ProcessException ex)
{
    Console.WriteLine($"CLI failed with exit code {ex.ExitCode}: {ex.Stderr}");
}
catch (JsonDecodeException ex)
{
    Console.WriteLine($"Failed to parse: {ex.RawData}");
}
catch (ClaudeAgentException ex)
{
    Console.WriteLine($"SDK error: {ex.Message}");
}
```

## Enums

### PermissionMode

```csharp
public enum PermissionMode
{
    Default,    // Normal permission prompts
    AcceptAll,  // Skip all prompts (--dangerously-skip-permissions)
    Allowlist   // Use allowed tools list
}
```

### OutputFormat

```csharp
public enum OutputFormat
{
    Text,       // Plain text output
    Json,       // Single JSON object
    StreamJson  // NDJSON streaming (default)
}
```

> **Note:** When using `StreamJson` with `--print` (non-interactive mode), the CLI also requires `--verbose` to produce the streaming JSON output. The SDK handles this automatically.

## Internal Components

These classes are internal but documented for maintenance purposes.

### CliLocator

Finds the Claude Code CLI executable.

**Search Order:**
1. Custom path (if provided via `ClaudeAgentOptions.CliPath`)
2. PATH environment variable (`claude` on Unix, `claude.cmd`/`claude.exe` on Windows)
3. Common installation locations:
   - **Windows:** `%APPDATA%\npm\claude.cmd`, `%LOCALAPPDATA%\npm\claude.cmd`
   - **Unix:** `/usr/local/bin/claude`, `~/.npm-global/bin/claude`, `~/.local/bin/claude`

### CommandBuilder

Builds CLI arguments from `ClaudeAgentOptions`.

**Argument Order:**
1. `--print` (if needed)
2. `--output-format` (if not Text)
3. Permission flags
4. Other options
5. `<prompt>` (positional argument, always last)

### MessageParser

Parses NDJSON lines into `IMessage` objects.

**JSON Property Mapping (snake_case to PascalCase):**
- `session_id` → `SessionId`
- `tool_use_id` → `ToolUseId`
- `is_error` → `IsError`
- `stop_reason` → `StopReason`
- `input_tokens` → `InputTokens`
- `output_tokens` → `OutputTokens`
- `cache_read_input_tokens` → `CacheReadInputTokens`
- `cache_creation_input_tokens` → `CacheCreationInputTokens`
- `total_cost_usd` → `CostUsd`
- `duration_ms` → `DurationMs`
- `num_turns` → `NumTurns`
- `mcp_servers` → `McpServers`

### CliProcess

Manages CLI process lifecycle.

**Key Features:**
- Redirects stdin/stdout/stderr
- Reads stdout as `IAsyncEnumerable<string>`
- Captures stderr for error reporting
- Kills entire process tree on cancellation: `process.Kill(entireProcessTree: true)`
- Implements `IAsyncDisposable` for cleanup

## Common Usage Patterns

### Pattern 1: Simple Query

```csharp
await using var client = new ClaudeAgentClient(new ClaudeAgentOptions
{
    DangerouslySkipPermissions = true,
    MaxTurns = 1
});

string answer = await client.QueryTextAsync("What is 2+2?");
```

### Pattern 2: Code Generation with Working Directory

```csharp
await using var client = new ClaudeAgentClient(new ClaudeAgentOptions
{
    WorkingDirectory = "/path/to/project",
    DangerouslySkipPermissions = true,
    MaxTurns = 10
});

var messages = await client.QueryAsync("Create a new React component called UserProfile");

foreach (var msg in messages.OfType<AssistantMessage>())
{
    foreach (var tool in msg.Content.OfType<ToolUseBlock>())
    {
        if (tool.Name == "Write")
        {
            var filePath = tool.Input.GetProperty("file_path").GetString();
            Console.WriteLine($"Created file: {filePath}");
        }
    }
}
```

### Pattern 3: Streaming with Progress Reporting

```csharp
await using var client = new ClaudeAgentClient();

await foreach (var msg in client.QueryStreamAsync("Explain quantum computing"))
{
    switch (msg)
    {
        case SystemMessage system:
            Console.WriteLine($"Session: {system.SessionId}");
            break;

        case AssistantMessage assistant:
            Console.Write(assistant.Text);
            break;

        case ResultMessage result:
            Console.WriteLine($"\n\nTokens used: {result.Usage.TotalTokens}");
            break;
    }
}
```

### Pattern 4: Session Continuation

```csharp
await using var client = new ClaudeAgentClient();

// First query
var messages = await client.QueryAsync("Remember: my favorite color is blue");
var sessionId = messages.OfType<ResultMessage>().First().SessionId;

// Continue session
var followUp = await client.QueryTextAsync(
    "What is my favorite color?",
    new ClaudeAgentOptions { ResumeSessionId = sessionId });
```

### Pattern 5: Custom Model Selection

```csharp
await using var client = new ClaudeAgentClient(new ClaudeAgentOptions
{
    Model = "claude-sonnet-4-20250514",
    DangerouslySkipPermissions = true
});
```

### Pattern 6: With Cancellation

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

try
{
    await using var client = new ClaudeAgentClient();
    var result = await client.QueryTextAsync("Long running task", ct: cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Query was cancelled");
}
```

### Pattern 7: Tool Allowlist

```csharp
await using var client = new ClaudeAgentClient(new ClaudeAgentOptions
{
    PermissionMode = PermissionMode.Allowlist,
    AllowedTools = ["Read", "Glob", "Grep"]  // Only allow read operations
});
```

## Integration with ASP.NET Core

### Dependency Injection Setup

```csharp
// Program.cs
builder.Services.AddSingleton<ClaudeAgentOptions>(_ => new ClaudeAgentOptions
{
    DangerouslySkipPermissions = true,
    WorkingDirectory = builder.Configuration["RepositoryPath"]
});

builder.Services.AddScoped<ClaudeAgentClient>(sp =>
    new ClaudeAgentClient(sp.GetRequiredService<ClaudeAgentOptions>()));
```

### Endpoint Example

```csharp
app.MapPost("/api/query", async (
    QueryRequest request,
    ClaudeAgentClient client,
    CancellationToken ct) =>
{
    var result = await client.QueryTextAsync(request, ct);
    return Results.Ok(new { response = result });
});
```

### Streaming Endpoint (SSE)

```csharp
app.MapGet("/api/query/stream", async (
    string prompt,
    ClaudeAgentClient client,
    CancellationToken ct) =>
{
    return Results.Stream(async stream =>
    {
        var writer = new StreamWriter(stream);

        await foreach (var msg in client.QueryStreamAsync(prompt, ct: ct))
        {
            if (msg is AssistantMessage assistant)
            {
                await writer.WriteAsync($"data: {assistant.Text}\n\n");
                await writer.FlushAsync();
            }
        }
    }, "text/event-stream");
});
```

## Testing

### Unit Testing (No CLI Required)

```csharp
// Test CommandBuilder
[Fact]
public void BuildArguments_WithMaxTurns_IncludesFlag()
{
    var options = new ClaudeAgentOptions { MaxTurns = 5 };
    var args = CommandBuilder.BuildArguments("test", options);

    Assert.Contains("--max-turns", args);
    Assert.Contains("5", args);
}

// Test MessageParser
[Fact]
public void Parse_AssistantMessage_ReturnsCorrectType()
{
    var json = """{"type":"assistant","content":[{"type":"text","text":"Hello"}]}""";
    var message = MessageParser.Parse(json);

    var assistant = Assert.IsType<AssistantMessage>(message);
    Assert.Equal("Hello", assistant.Text);
}
```

### Integration Testing (Requires CLI)

```csharp
[Fact]
public async Task QueryTextAsync_SimplePrompt_ReturnsResponse()
{
    await using var client = new ClaudeAgentClient(new ClaudeAgentOptions
    {
        DangerouslySkipPermissions = true,
        MaxTurns = 1
    });

    var result = await client.QueryTextAsync("Say 'test'");

    Assert.Contains("test", result, StringComparison.OrdinalIgnoreCase);
}
```

## Troubleshooting

### CLI Not Found

```
CliNotFoundException: Claude Code CLI not found. Searched paths: ...
```

**Solutions:**
1. Install Claude Code CLI: `npm install -g @anthropic-ai/claude-code`
2. Specify custom path: `new ClaudeAgentOptions { CliPath = "/path/to/claude" }`
3. Ensure CLI is in PATH

### Permission Denied

```
ProcessException: CLI process exited with code 1: Permission denied
```

**Solutions:**
1. Use `DangerouslySkipPermissions = true` for automation
2. Configure `AllowedTools` for specific tool access
3. Run in interactive mode for manual approval

### JSON Parse Errors

```
JsonDecodeException: Failed to parse JSON from CLI output
```

**Causes:**
- CLI outputting non-JSON content (warnings, errors)
- Incompatible CLI version

**Solutions:**
1. Update CLI to latest version
2. Check stderr for error messages
3. Enable `Verbose = true` for debugging

## Version Compatibility

| SDK Version | CLI Version | .NET Version |
|-------------|-------------|--------------|
| 1.0.x       | Latest      | .NET 10+     |

## License

This SDK is part of the Forge project. See repository root for license information.
