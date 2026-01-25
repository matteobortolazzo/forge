using System.Diagnostics;
using Claude.CodeSdk.Messages;
using Claude.CodeSdk.Tests.Console.Utilities;

namespace Claude.CodeSdk.Tests.Console.Tests;

/// <summary>
/// Tests for QueryStreamAsync functionality.
/// </summary>
public static class QueryStreamTests
{
    private static ClaudeAgentOptions CreateTestOptions() => new()
    {
        MaxTurns = 1,
        DangerouslySkipPermissions = true,
        Print = true
    };

    /// <summary>
    /// Verifies streaming returns messages.
    /// </summary>
    public sealed class QueryStreamAsync_YieldsMessages : ITest
    {
        public string Name => "QueryStreamAsync_YieldsMessages";
        public bool RequiresCli => true;

        public async Task<TestResult> RunAsync(CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            await using var client = new ClaudeAgentClient(CreateTestOptions());

            var messages = new List<IMessage>();
            await foreach (var message in client.QueryStreamAsync("Say hello", ct: ct))
            {
                messages.Add(message);
            }

            Assertions.AssertGreaterThan(messages.Count, 0, "message count");

            stopwatch.Stop();
            return TestResult.Pass(Name, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Verifies streaming AssistantMessage contains text.
    /// </summary>
    public sealed class QueryStreamAsync_AssistantHasText : ITest
    {
        public string Name => "QueryStreamAsync_AssistantHasText";
        public bool RequiresCli => true;

        public async Task<TestResult> RunAsync(CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            await using var client = new ClaudeAgentClient(CreateTestOptions());

            AssistantMessage? assistantMessage = null;
            await foreach (var message in client.QueryStreamAsync("Say hello", ct: ct))
            {
                if (message is AssistantMessage assistant)
                {
                    assistantMessage = assistant;
                    break; // Found one, that's enough
                }
            }

            Assertions.AssertNotNull(assistantMessage, "AssistantMessage");
            Assertions.AssertNotNullOrEmpty(assistantMessage!.Text, "AssistantMessage.Text");

            stopwatch.Stop();
            return TestResult.Pass(Name, stopwatch.Elapsed);
        }
    }
}
