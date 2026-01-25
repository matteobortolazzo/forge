using System.Diagnostics;
using Claude.CodeSdk.ContentBlocks;
using Claude.CodeSdk.Messages;
using Claude.CodeSdk.Tests.Console.Utilities;

namespace Claude.CodeSdk.Tests.Console.Tests;

/// <summary>
/// Tests for content block parsing.
/// </summary>
public static class ContentBlockTests
{
    private static ClaudeAgentOptions CreateTestOptions() => new()
    {
        MaxTurns = 1,
        DangerouslySkipPermissions = true,
        Print = true
    };

    /// <summary>
    /// Verifies AssistantMessage contains TextBlock.
    /// </summary>
    public sealed class AssistantMessage_ContainsTextBlock : ITest
    {
        public string Name => "AssistantMessage_ContainsTextBlock";
        public bool RequiresCli => true;

        public async Task<TestResult> RunAsync(CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            await using var client = new ClaudeAgentClient(CreateTestOptions());
            var messages = await client.QueryAsync("Say hello", ct: ct);

            var assistantMessage = messages.OfType<AssistantMessage>().FirstOrDefault();
            Assertions.AssertNotNull(assistantMessage, "AssistantMessage");

            var textBlock = assistantMessage!.Content.OfType<TextBlock>().FirstOrDefault();
            Assertions.AssertNotNull(textBlock, "TextBlock");
            Assertions.AssertNotNullOrEmpty(textBlock!.Text, "TextBlock.Text");

            stopwatch.Stop();
            return TestResult.Pass(Name, stopwatch.Elapsed);
        }
    }
}
