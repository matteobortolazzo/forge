using System.Text.Json;
using System.Text.Json.Serialization;

namespace Forge.Api.IntegrationTests.Helpers;

/// <summary>
/// HTTP extensions for integration tests. Provides JSON serialization options
/// matching the API's configuration and convenience methods not available in
/// System.Net.Http.Json.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// JSON options matching the API's serialization (camelCase, enum as string).
    /// Use with built-in methods: client.PostAsJsonAsync(url, data, JsonOptions)
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// PATCH with JSON body. No built-in equivalent in System.Net.Http.Json.
    /// </summary>
    public static async Task<HttpResponseMessage> PatchAsJsonAsync<T>(this HttpClient client, string uri, T data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        return await client.PatchAsync(uri, content);
    }

    /// <summary>
    /// Convenience wrapper for response.Content.ReadFromJsonAsync with correct options.
    /// </summary>
    public static async Task<T?> ReadAsAsync<T>(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }
}
