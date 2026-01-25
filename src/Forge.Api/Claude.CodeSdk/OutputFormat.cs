namespace Claude.CodeSdk;

/// <summary>
/// Specifies the output format for CLI responses.
/// </summary>
public enum OutputFormat
{
    /// <summary>
    /// Plain text output (default).
    /// </summary>
    Text,

    /// <summary>
    /// Single JSON object output.
    /// Maps to CLI flag: --output-format json
    /// </summary>
    Json,

    /// <summary>
    /// Newline-delimited JSON streaming output.
    /// Maps to CLI flag: --output-format stream-json
    /// </summary>
    StreamJson
}
