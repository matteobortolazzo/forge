using System.Runtime.CompilerServices;
using System.Text.Json;
using Forge.E2E.Console.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forge.E2E.Console.Services;

/// <summary>
/// SSE event listener with auto-reconnection support.
/// </summary>
public sealed class SseEventListener : IDisposable
{
    private readonly HttpClient _http;
    private readonly ILogger<SseEventListener> _logger;
    private readonly string _baseUrl;

    private HttpResponseMessage? _response;
    private StreamReader? _reader;
    private bool _disposed;

    private const int MaxReconnectAttempts = 5;
    private static readonly TimeSpan[] ReconnectDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8),
        TimeSpan.FromSeconds(16)
    ];

    public SseEventListener(HttpClient http, IOptions<ForgeApiOptions> options, ILogger<SseEventListener> logger)
    {
        _http = http;
        _logger = logger;
        _baseUrl = options.Value.BaseUrl.TrimEnd('/');
    }

    /// <summary>
    /// Connects to the SSE endpoint and streams events.
    /// </summary>
    public async IAsyncEnumerable<SseEvent> GetEventsAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        var reconnectAttempts = 0;

        while (!ct.IsCancellationRequested && !_disposed)
        {
            await foreach (var evt in TryStreamEventsAsync(ct))
            {
                reconnectAttempts = 0; // Reset on successful event
                yield return evt;
            }

            if (ct.IsCancellationRequested || _disposed)
                break;

            // Handle reconnection
            if (reconnectAttempts >= MaxReconnectAttempts)
            {
                _logger.LogError("Max reconnection attempts reached, giving up");
                throw new InvalidOperationException("SSE connection lost after max reconnection attempts");
            }

            var delay = ReconnectDelays[Math.Min(reconnectAttempts, ReconnectDelays.Length - 1)];
            _logger.LogWarning("SSE connection lost, reconnecting in {Delay}...", delay);
            await Task.Delay(delay, ct);
            reconnectAttempts++;
        }
    }

    private async IAsyncEnumerable<SseEvent> TryStreamEventsAsync([EnumeratorCancellation] CancellationToken ct)
    {
        Cleanup();

        HttpResponseMessage? response = null;
        StreamReader? reader = null;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/events");
            request.Headers.Accept.ParseAdd("text/event-stream");

            response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            _response = response;
            var stream = await response.Content.ReadAsStreamAsync(ct);
            reader = new StreamReader(stream);
            _reader = reader;

            _logger.LogInformation("Connected to SSE endpoint");

            while (!ct.IsCancellationRequested && !_disposed)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == null)
                {
                    // Connection closed
                    _logger.LogDebug("SSE stream ended");
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("data:", StringComparison.Ordinal))
                {
                    var data = line[5..].Trim();
                    if (!string.IsNullOrEmpty(data))
                    {
                        SseEvent? evt = null;
                        try
                        {
                            evt = JsonSerializer.Deserialize<SseEvent>(data, ForgeApiClient.JsonOptions);
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse SSE event: {Data}", data);
                        }

                        if (evt != null)
                        {
                            yield return evt;
                        }
                    }
                }
            }
        }
        finally
        {
            // Only cleanup if these are still our instances
            if (ReferenceEquals(_reader, reader))
            {
                _reader = null;
            }
            if (ReferenceEquals(_response, response))
            {
                _response = null;
            }

            reader?.Dispose();
            response?.Dispose();
        }
    }

    private void Cleanup()
    {
        _reader?.Dispose();
        _reader = null;
        _response?.Dispose();
        _response = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Cleanup();
    }
}
