namespace Forge.Api.Shared;

/// <summary>
/// Standardized API error response format.
/// </summary>
public record ApiErrorResponse(
    string Error,
    string? Details = null)
{
    /// <summary>
    /// Creates an error response from an exception message.
    /// </summary>
    public static ApiErrorResponse FromException(Exception ex)
        => new(ex.Message);

    /// <summary>
    /// Creates an error response with a custom message.
    /// </summary>
    public static ApiErrorResponse FromMessage(string message, string? details = null)
        => new(message, details);
}
