namespace Forge.Api.Features.Events;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/events", GetEvents)
            .WithName("GetEvents")
            .WithTags("Events")
            .ExcludeFromDescription(); // SSE endpoints don't work well with OpenAPI
    }

    private static async Task GetEvents(HttpContext context, ISseService sseService)
    {
        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        var ct = context.RequestAborted;

        try
        {
            await foreach (var evt in sseService.GetEventsAsync(ct))
            {
                await context.Response.WriteAsync(evt, ct);
                await context.Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected, this is expected
        }
    }
}
