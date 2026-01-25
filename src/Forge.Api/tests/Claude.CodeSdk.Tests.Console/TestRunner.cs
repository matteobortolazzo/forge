using System.Diagnostics;
using Claude.CodeSdk.Tests.Console.Tests;
using Claude.CodeSdk.Tests.Console.Utilities;

namespace Claude.CodeSdk.Tests.Console;

/// <summary>
/// Discovers and runs all tests.
/// </summary>
public sealed class TestRunner
{
    private readonly bool _verbose;
    private readonly string? _filter;
    private readonly bool _cliAvailable;

    public TestRunner(bool verbose, string? filter, bool cliAvailable)
    {
        _verbose = verbose;
        _filter = filter;
        _cliAvailable = cliAvailable;
    }

    /// <summary>
    /// Runs all discovered tests and returns the exit code.
    /// </summary>
    /// <returns>0 if all tests passed, 1 if any failed, 2 if infrastructure error.</returns>
    public async Task<int> RunAllAsync(CancellationToken ct = default)
    {
        var tests = DiscoverTests();

        if (!string.IsNullOrEmpty(_filter))
        {
            tests = tests
                .Where(t => t.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (tests.Count == 0)
        {
            ConsoleHelper.WriteWarning("No tests found matching the filter.");
            return 0;
        }

        ConsoleHelper.WriteHeader($"Running {tests.Count} tests");

        if (!_cliAvailable)
        {
            ConsoleHelper.WriteWarning("Claude CLI not available. Tests requiring CLI will be skipped.");
        }

        var results = new List<TestResult>();
        var totalStopwatch = Stopwatch.StartNew();

        foreach (var test in tests)
        {
            if (ct.IsCancellationRequested)
                break;

            // Skip CLI-dependent tests if CLI is not available
            if (test.RequiresCli && !_cliAvailable)
            {
                var skipResult = TestResult.Skip(test.Name, "Claude CLI not available");
                results.Add(skipResult);
                ConsoleHelper.WriteSkip(test.Name, "Claude CLI not available");
                continue;
            }

            if (_verbose)
            {
                ConsoleHelper.WriteTestStart(test.Name);
            }

            var result = await RunTestAsync(test, ct);
            results.Add(result);

            switch (result.Outcome)
            {
                case TestOutcome.Passed:
                    ConsoleHelper.WritePass(result.Name, result.Duration);
                    break;
                case TestOutcome.Failed:
                    ConsoleHelper.WriteFail(result.Name, result.Duration, result.Message ?? "Unknown failure");
                    break;
                case TestOutcome.Skipped:
                    ConsoleHelper.WriteSkip(result.Name, result.Message ?? "Skipped");
                    break;
                case TestOutcome.Error:
                    ConsoleHelper.WriteError(result.Name, result.Duration, result.Exception ?? new Exception(result.Message));
                    break;
            }
        }

        totalStopwatch.Stop();

        var passed = results.Count(r => r.Outcome == TestOutcome.Passed);
        var failed = results.Count(r => r.Outcome == TestOutcome.Failed);
        var skipped = results.Count(r => r.Outcome == TestOutcome.Skipped);
        var errors = results.Count(r => r.Outcome == TestOutcome.Error);

        ConsoleHelper.WriteSummary(passed, failed, skipped, errors, totalStopwatch.Elapsed);

        // Exit codes
        if (errors > 0)
            return 2; // Infrastructure error
        if (failed > 0)
            return 1; // Test failure
        return 0; // All passed (skipped tests don't affect exit code)
    }

    private async Task<TestResult> RunTestAsync(ITest test, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await test.RunAsync(ct);
            stopwatch.Stop();

            // Update duration if not already set
            if (result.Duration == TimeSpan.Zero && result.Outcome != TestOutcome.Skipped)
            {
                return result with { Duration = stopwatch.Elapsed };
            }

            return result;
        }
        catch (AssertionException ex)
        {
            stopwatch.Stop();
            return TestResult.Fail(test.Name, stopwatch.Elapsed, ex.Message);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return TestResult.Skip(test.Name, "Test cancelled");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return TestResult.Error(test.Name, stopwatch.Elapsed, ex);
        }
    }

    private static List<ITest> DiscoverTests()
    {
        // Return all test instances in execution order
        return
        [
            // CLI Discovery (Fast, no API calls)
            new CliLocatorTests.CliLocator_FindsClaude(),
            new CliLocatorTests.CliLocator_ThrowsWhenInvalidPath(),

            // Error Handling (Fast, no API calls)
            new ErrorHandlingTests.Client_ThrowsOnEmptyPrompt(),
            new ErrorHandlingTests.Client_ThrowsOnWhitespacePrompt(),

            // Query Tests (Require CLI)
            new QueryTextTests.QueryTextAsync_ReturnsNonEmpty(),
            new QueryTextTests.QueryTextAsync_MathQuestion(),

            // Full Message Query Tests (Require CLI)
            new QueryAsyncTests.QueryAsync_ReturnsSystemMessage(),
            new QueryAsyncTests.QueryAsync_ReturnsAssistantMessage(),
            new QueryAsyncTests.QueryAsync_ReturnsResultMessage(),
            new QueryAsyncTests.QueryAsync_ResultHasValidUsage(),

            // Streaming Tests (Require CLI)
            new QueryStreamTests.QueryStreamAsync_YieldsMessages(),
            new QueryStreamTests.QueryStreamAsync_AssistantHasText(),

            // Content Block Tests (Require CLI)
            new ContentBlockTests.AssistantMessage_ContainsTextBlock(),

            // Options Tests (Require CLI)
            new OptionsTests.Options_MaxTurns_LimitsConversation(),
        ];
    }
}
