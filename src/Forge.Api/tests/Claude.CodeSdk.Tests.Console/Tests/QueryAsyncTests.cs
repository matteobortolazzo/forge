using System.Diagnostics;
using Claude.CodeSdk.Messages;
using Claude.CodeSdk.Tests.Console.Utilities;

namespace Claude.CodeSdk.Tests.Console.Tests;

/// <summary>
/// Tests for QueryAsync (full message) functionality.
/// </summary>
public static class QueryAsyncTests
{
    private static ClaudeAgentOptions CreateTestOptions() => new()
    {
        MaxTurns = 1,
        DangerouslySkipPermissions = true,
        Print = true
    };

    /// <summary>
    /// Verifies messages include SystemMessage.
    /// </summary>
    public sealed class QueryAsync_ReturnsSystemMessage : ITest
    {
        public string Name => "QueryAsync_ReturnsSystemMessage";
        public bool RequiresCli => true;

        public async Task<TestResult> RunAsync(CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            await using var client = new ClaudeAgentClient(CreateTestOptions());
            var messages = await client.QueryAsync("Say hello", ct: ct);

            Assertions.AssertAny(messages, m => m is SystemMessage,
                "Expected messages to contain a SystemMessage");

            stopwatch.Stop();
            return TestResult.Pass(Name, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Verifies messages include AssistantMessage.
    /// </summary>
    public sealed class QueryAsync_ReturnsAssistantMessage : ITest
    {
        public string Name => "QueryAsync_ReturnsAssistantMessage";
        public bool RequiresCli => true;

        public async Task<TestResult> RunAsync(CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            await using var client = new ClaudeAgentClient(CreateTestOptions());
            var messages = await client.QueryAsync("Say hello", ct: ct);

            Assertions.AssertAny(messages, m => m is AssistantMessage,
                "Expected messages to contain an AssistantMessage");

            stopwatch.Stop();
            return TestResult.Pass(Name, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Verifies messages include ResultMessage with Usage.
    /// </summary>
    public sealed class QueryAsync_ReturnsResultMessage : ITest
    {
        public string Name => "QueryAsync_ReturnsResultMessage";
        public bool RequiresCli => true;

        public async Task<TestResult> RunAsync(CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            await using var client = new ClaudeAgentClient(CreateTestOptions());
            var messages = await client.QueryAsync("Say hello", ct: ct);

            var resultMessage = messages.OfType<ResultMessage>().FirstOrDefault();
            Assertions.AssertNotNull(resultMessage, "ResultMessage");

            stopwatch.Stop();
            return TestResult.Pass(Name, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Verifies ResultMessage has valid token usage.
    /// </summary>
    public sealed class QueryAsync_ResultHasValidUsage : ITest
    {
        public string Name => "QueryAsync_ResultHasValidUsage";
        public bool RequiresCli => true;

        public async Task<TestResult> RunAsync(CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            await using var client = new ClaudeAgentClient(CreateTestOptions());
            var messages = await client.QueryAsync("Say hello", ct: ct);

            var resultMessage = messages.OfType<ResultMessage>().FirstOrDefault();
            Assertions.AssertNotNull(resultMessage, "ResultMessage");
            Assertions.AssertNotNull(resultMessage!.Usage, "Usage");
            Assertions.AssertGreaterThan(resultMessage.Usage.InputTokens, 0, "InputTokens");
            Assertions.AssertGreaterThan(resultMessage.Usage.OutputTokens, 0, "OutputTokens");

            stopwatch.Stop();
            return TestResult.Pass(Name, stopwatch.Elapsed);
        }
    }
}
