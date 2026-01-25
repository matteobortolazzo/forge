namespace Claude.CodeSdk.Tests.Console;

/// <summary>
/// Represents the outcome of a test execution.
/// </summary>
public enum TestOutcome
{
    Passed,
    Failed,
    Skipped,
    Error
}

/// <summary>
/// Represents the result of a single test execution.
/// </summary>
/// <param name="Name">The name of the test.</param>
/// <param name="Outcome">The outcome of the test.</param>
/// <param name="Duration">How long the test took to execute.</param>
/// <param name="Message">Optional message (error message for failures, skip reason for skipped).</param>
/// <param name="Exception">The exception if the test failed with an error.</param>
public sealed record TestResult(
    string Name,
    TestOutcome Outcome,
    TimeSpan Duration,
    string? Message = null,
    Exception? Exception = null)
{
    public static TestResult Pass(string name, TimeSpan duration)
        => new(name, TestOutcome.Passed, duration);

    public static TestResult Fail(string name, TimeSpan duration, string message)
        => new(name, TestOutcome.Failed, duration, message);

    public static TestResult Skip(string name, string reason)
        => new(name, TestOutcome.Skipped, TimeSpan.Zero, reason);

    public static TestResult Error(string name, TimeSpan duration, Exception ex)
        => new(name, TestOutcome.Error, duration, ex.Message, ex);
}
