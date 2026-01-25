using System.Diagnostics;
using Claude.CodeSdk.Exceptions;
using Claude.CodeSdk.Tests.Console.Utilities;

namespace Claude.CodeSdk.Tests.Console.Tests;

/// <summary>
/// Tests for CLI discovery functionality.
/// </summary>
public static class CliLocatorTests
{
    /// <summary>
    /// Verifies SDK can locate the claude executable.
    /// </summary>
    public sealed class CliLocator_FindsClaude : ITest
    {
        public string Name => "CliLocator_FindsClaude";
        public bool RequiresCli => true;

        public async Task<TestResult> RunAsync(CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            // If we get here without RequiresCli check failing, CLI is available
            // Test that we can create a client (which internally calls CliLocator.FindCli)
            await using var client = new ClaudeAgentClient();

            stopwatch.Stop();
            return TestResult.Pass(Name, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Verifies CliNotFoundException contains the searched path when thrown.
    /// This test catches the exception and verifies its SearchedPaths property,
    /// or skips if CLI is found via fallback paths (e.g., common installation locations).
    /// </summary>
    public sealed class CliLocator_ThrowsWhenInvalidPath : ITest
    {
        public string Name => "CliLocator_ThrowsWhenInvalidPath";
        public bool RequiresCli => false;

        public Task<TestResult> RunAsync(CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var invalidPath = "/nonexistent/path/to/claude-that-does-not-exist-12345";

            // Save original PATH
            var originalPath = Environment.GetEnvironmentVariable("PATH");

            try
            {
                // Clear PATH to increase chance of exception
                Environment.SetEnvironmentVariable("PATH", "");

                try
                {
                    var options = new ClaudeAgentOptions
                    {
                        CliPath = invalidPath
                    };

                    // Creating a client should throw since CLI can't be found
                    _ = new ClaudeAgentClient(options);

                    // If we get here, CLI was found via fallback paths (common locations)
                    stopwatch.Stop();
                    return Task.FromResult(TestResult.Skip(Name,
                        "CLI found via fallback paths; cannot test CliNotFoundException"));
                }
                catch (CliNotFoundException ex)
                {
                    // Verify the exception contains our invalid path in SearchedPaths
                    Assertions.Assert(
                        ex.SearchedPaths.Contains(invalidPath),
                        $"Expected SearchedPaths to contain '{invalidPath}' but got: {string.Join(", ", ex.SearchedPaths)}");

                    stopwatch.Stop();
                    return Task.FromResult(TestResult.Pass(Name, stopwatch.Elapsed));
                }
            }
            finally
            {
                // Restore original PATH
                Environment.SetEnvironmentVariable("PATH", originalPath);
            }
        }
    }
}
