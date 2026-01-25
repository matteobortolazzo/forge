using System.Diagnostics;
using Claude.CodeSdk.Tests.Console.Utilities;

namespace Claude.CodeSdk.Tests.Console.Tests;

/// <summary>
/// Tests for error handling functionality.
/// </summary>
public static class ErrorHandlingTests
{
    /// <summary>
    /// Verifies empty prompt throws ArgumentException.
    /// </summary>
    public sealed class Client_ThrowsOnEmptyPrompt : ITest
    {
        public string Name => "Client_ThrowsOnEmptyPrompt";
        public bool RequiresCli => true;

        public async Task<TestResult> RunAsync(CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            await using var client = new ClaudeAgentClient();

            await Assertions.AssertThrowsAsync<ArgumentException>(
                async () => await client.QueryTextAsync("", ct: ct));

            stopwatch.Stop();
            return TestResult.Pass(Name, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Verifies whitespace-only prompt throws ArgumentException.
    /// </summary>
    public sealed class Client_ThrowsOnWhitespacePrompt : ITest
    {
        public string Name => "Client_ThrowsOnWhitespacePrompt";
        public bool RequiresCli => true;

        public async Task<TestResult> RunAsync(CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            await using var client = new ClaudeAgentClient();

            await Assertions.AssertThrowsAsync<ArgumentException>(
                async () => await client.QueryTextAsync("   ", ct: ct));

            stopwatch.Stop();
            return TestResult.Pass(Name, stopwatch.Elapsed);
        }
    }
}
