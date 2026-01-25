namespace Claude.CodeSdk.Exceptions;

/// <summary>
/// Thrown when JSON parsing of CLI output fails.
/// </summary>
public sealed class JsonDecodeException : ClaudeAgentException
{
    /// <summary>
    /// Gets the raw data that failed to parse.
    /// </summary>
    public string RawData { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDecodeException"/> class.
    /// </summary>
    /// <param name="rawData">The raw data that failed to parse.</param>
    /// <param name="innerException">The inner JSON exception.</param>
    public JsonDecodeException(string rawData, Exception innerException)
        : base($"Failed to parse JSON from CLI output: {innerException.Message}", innerException)
    {
        RawData = rawData;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDecodeException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="rawData">The raw data that failed to parse.</param>
    public JsonDecodeException(string message, string rawData) : base(message)
    {
        RawData = rawData;
    }
}
