using System.Diagnostics;
using Claude.CodeSdk.Tests.Console.Utilities;

namespace Claude.CodeSdk.Tests.Console.Tests;

/// <summary>
/// Tests for QueryTextAsync functionality.
/// </summary>
public static class QueryTextTests
{
    private static ClaudeAgentOptions CreateTestOptions() => new()
    {
        MaxTurns = 1,
        DangerouslySkipPermissions = true,
        Print = true
    };

    /// <summary>
    /// Verifies simple prompt returns non-empty response.
    /// </summary>
    public sealed class QueryTextAsync_ReturnsNonEmpty : ITest
    {
        public string Name => "QueryTextAsync_ReturnsNonEmpty";
        public bool RequiresCli => true;

        public async Task<TestResult> RunAsync(CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            await using var client = new ClaudeAgentClient(CreateTestOptions());
            var response = await client.QueryTextAsync("Say hello", ct: ct);

            Assertions.AssertNotNullOrEmpty(response, "response");

            stopwatch.Stop();
            return TestResult.Pass(Name, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Verifies math question returns correct answer.
    /// </summary>
    public sealed class QueryTextAsync_MathQuestion : ITest
    {
        public string Name => "QueryTextAsync_MathQuestion";
        public bool RequiresCli => true;

        public async Task<TestResult> RunAsync(CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            await using var client = new ClaudeAgentClient(CreateTestOptions());
            var response = await client.QueryTextAsync(
                "What is 2+2? Reply with just the number.",
                ct: ct);

            Assertions.AssertContains(response, "4");

            stopwatch.Stop();
            return TestResult.Pass(Name, stopwatch.Elapsed);
        }
    }
}
