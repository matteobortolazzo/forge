using System.Diagnostics;
using Claude.CodeSdk.Messages;
using Claude.CodeSdk.Tests.Console.Utilities;

namespace Claude.CodeSdk.Tests.Console.Tests;

/// <summary>
/// Tests for ClaudeAgentOptions functionality.
/// </summary>
public static class OptionsTests
{
    /// <summary>
    /// Verifies MaxTurns=1 limits conversation to single turn.
    /// </summary>
    public sealed class Options_MaxTurns_LimitsConversation : ITest
    {
        public string Name => "Options_MaxTurns_LimitsConversation";
        public bool RequiresCli => true;

        public async Task<TestResult> RunAsync(CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            var options = new ClaudeAgentOptions
            {
                MaxTurns = 1,
                DangerouslySkipPermissions = true,
                Print = true
            };

            await using var client = new ClaudeAgentClient(options);
            var messages = await client.QueryAsync("Say hello", ct: ct);

            var resultMessage = messages.OfType<ResultMessage>().FirstOrDefault();
            Assertions.AssertNotNull(resultMessage, "ResultMessage");

            // With MaxTurns=1, NumTurns should be 1
            if (resultMessage!.NumTurns.HasValue)
            {
                Assertions.AssertEqual(1, resultMessage.NumTurns.Value,
                    $"Expected NumTurns to be 1 but was {resultMessage.NumTurns.Value}");
            }

            stopwatch.Stop();
            return TestResult.Pass(Name, stopwatch.Elapsed);
        }
    }
}
