using System.Text.Json;
using Claude.CodeSdk.ContentBlocks;
using Claude.CodeSdk.Messages;

namespace Claude.CodeSdk.Mock;

/// <summary>
/// Represents a mock scenario with pre-built message sequences.
/// </summary>
public sealed class MockScenario
{
    /// <summary>
    /// Gets or sets the unique identifier for this scenario.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or sets a description of what this scenario simulates.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the messages to yield in sequence.
    /// </summary>
    public required IReadOnlyList<IMessage> Messages { get; init; }

    /// <summary>
    /// Gets or sets the delay between messages in milliseconds.
    /// </summary>
    public int DelayBetweenMessagesMs { get; init; } = 100;

    /// <summary>
    /// Gets the pre-built Default scenario - a realistic multi-turn agent interaction.
    /// </summary>
    public static MockScenario Default => new()
    {
        Id = "default",
        Description = "Realistic multi-turn agent interaction with tool use",
        DelayBetweenMessagesMs = 150,
        Messages =
        [
            new SystemMessage(SessionId: Guid.NewGuid().ToString()),
            new AssistantMessage(
                [new TextBlock("I'll analyze the task and implement the required changes.")],
                Model: "claude-sonnet-4-20250514"),
            new AssistantMessage(
            [
                new TextBlock("Let me read the relevant files to understand the current implementation."),
                new ToolUseBlock(
                    Id: "tool_01",
                    Name: "Read",
                    Input: JsonDocument.Parse("""{"file_path": "/src/example.ts"}""").RootElement)
            ],
                Model: "claude-sonnet-4-20250514"),
            new AssistantMessage(
                [new TextBlock("Based on my analysis, I'll now implement the changes.")],
                Model: "claude-sonnet-4-20250514"),
            new AssistantMessage(
            [
                new TextBlock("Making the required modifications..."),
                new ToolUseBlock(
                    Id: "tool_02",
                    Name: "Edit",
                    Input: JsonDocument.Parse("""{"file_path": "/src/example.ts", "old_string": "old", "new_string": "new"}""").RootElement)
            ],
                Model: "claude-sonnet-4-20250514"),
            new AssistantMessage(
                [new TextBlock("The implementation is complete. I've made the necessary changes to address the requirements.")],
                Model: "claude-sonnet-4-20250514",
                StopReason: "end_turn"),
            new ResultMessage(
                Usage: new Usage(InputTokens: 1500, OutputTokens: 500),
                SessionId: Guid.NewGuid().ToString(),
                CostUsd: 0.01m,
                DurationMs: 5000,
                NumTurns: 3)
        ]
    };

    /// <summary>
    /// Gets the pre-built QuickSuccess scenario - fast completion for simple tests.
    /// </summary>
    public static MockScenario QuickSuccess => new()
    {
        Id = "quick-success",
        Description = "Fast completion for simple tests",
        DelayBetweenMessagesMs = 50,
        Messages =
        [
            new SystemMessage(SessionId: Guid.NewGuid().ToString()),
            new AssistantMessage(
                [new TextBlock("Task completed successfully.")],
                Model: "claude-sonnet-4-20250514",
                StopReason: "end_turn"),
            new ResultMessage(
                Usage: new Usage(InputTokens: 100, OutputTokens: 20),
                SessionId: Guid.NewGuid().ToString(),
                CostUsd: 0.001m,
                DurationMs: 500,
                NumTurns: 1)
        ]
    };

    /// <summary>
    /// Gets the pre-built Error scenario - simulates agent failure.
    /// </summary>
    public static MockScenario Error => new()
    {
        Id = "error",
        Description = "Simulates agent failure",
        DelayBetweenMessagesMs = 50,
        Messages =
        [
            new SystemMessage(SessionId: Guid.NewGuid().ToString()),
            new AssistantMessage(
                [new TextBlock("I encountered an error while processing the task. The operation could not be completed due to an unexpected issue.")],
                Model: "claude-sonnet-4-20250514",
                StopReason: "error"),
            new ResultMessage(
                Usage: new Usage(InputTokens: 100, OutputTokens: 30),
                SessionId: Guid.NewGuid().ToString(),
                CostUsd: 0.001m,
                DurationMs: 200,
                NumTurns: 1)
        ]
    };

    /// <summary>
    /// Gets the pre-built LongRunning scenario - simulates a long-running task with multiple turns.
    /// </summary>
    public static MockScenario LongRunning => new()
    {
        Id = "long-running",
        Description = "Long-running task with multiple tool interactions",
        DelayBetweenMessagesMs = 200,
        Messages =
        [
            new SystemMessage(SessionId: Guid.NewGuid().ToString()),
            new AssistantMessage(
                [new TextBlock("This is a complex task. I'll break it down into smaller steps.")],
                Model: "claude-sonnet-4-20250514"),
            new AssistantMessage(
            [
                new TextBlock("Step 1: Reading configuration files..."),
                new ToolUseBlock(
                    Id: "tool_01",
                    Name: "Glob",
                    Input: JsonDocument.Parse("""{"pattern": "**/*.config.ts"}""").RootElement)
            ],
                Model: "claude-sonnet-4-20250514"),
            new AssistantMessage(
            [
                new TextBlock("Step 2: Analyzing project structure..."),
                new ToolUseBlock(
                    Id: "tool_02",
                    Name: "Read",
                    Input: JsonDocument.Parse("""{"file_path": "/src/index.ts"}""").RootElement)
            ],
                Model: "claude-sonnet-4-20250514"),
            new AssistantMessage(
            [
                new TextBlock("Step 3: Implementing changes..."),
                new ToolUseBlock(
                    Id: "tool_03",
                    Name: "Write",
                    Input: JsonDocument.Parse("""{"file_path": "/src/new-feature.ts", "content": "export const feature = {}"}""").RootElement)
            ],
                Model: "claude-sonnet-4-20250514"),
            new AssistantMessage(
                [new TextBlock("All steps completed. The implementation is ready for review.")],
                Model: "claude-sonnet-4-20250514",
                StopReason: "end_turn"),
            new ResultMessage(
                Usage: new Usage(InputTokens: 3000, OutputTokens: 1000),
                SessionId: Guid.NewGuid().ToString(),
                CostUsd: 0.03m,
                DurationMs: 15000,
                NumTurns: 5)
        ]
    };

    /// <summary>
    /// Gets all pre-built scenarios.
    /// </summary>
    public static IReadOnlyList<MockScenario> AllScenarios =>
    [
        Default,
        QuickSuccess,
        Error,
        LongRunning
    ];
}
