using System.Text.Json;
using System.Text.Json.Serialization;

namespace Forge.Api.Shared;

/// <summary>
/// Centralized JSON serializer options for consistency across the application.
/// </summary>
public static class SharedJsonOptions
{
    /// <summary>
    /// Standard options using camelCase with string enums.
    /// Use for SSE events and general API responses.
    /// </summary>
    public static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Options using camelCase without additional converters.
    /// Use for internal JSON serialization (subtasks, etc.).
    /// </summary>
    public static readonly JsonSerializerOptions CamelCaseSimple = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Options using snake_case_lower naming policy.
    /// Use for rollback records and other snake_case requirements.
    /// </summary>
    public static readonly JsonSerializerOptions SnakeCaseLower = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
}
