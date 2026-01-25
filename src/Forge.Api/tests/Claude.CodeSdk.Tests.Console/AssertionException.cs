namespace Claude.CodeSdk.Tests.Console;

/// <summary>
/// Thrown when a test assertion fails.
/// </summary>
public sealed class AssertionException : Exception
{
    public AssertionException(string message) : base(message)
    {
    }

    public AssertionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
