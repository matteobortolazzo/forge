namespace Claude.CodeSdk.Tests.Console.Tests;

/// <summary>
/// Interface for all test classes.
/// </summary>
public interface ITest
{
    /// <summary>
    /// Gets the name of the test.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets whether this test requires the Claude CLI to be available.
    /// </summary>
    bool RequiresCli { get; }

    /// <summary>
    /// Runs the test and returns the result.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The test result.</returns>
    Task<TestResult> RunAsync(CancellationToken ct = default);
}
