namespace Forge.Api.Features.Notifications;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .WithTags("Notifications");

        group.MapGet("/", GetNotifications)
            .WithName("GetNotifications");

        group.MapPatch("/{id:guid}/read", MarkAsRead)
            .WithName("MarkNotificationAsRead");

        group.MapPost("/mark-all-read", MarkAllAsRead)
            .WithName("MarkAllNotificationsAsRead");

        group.MapGet("/unread-count", GetUnreadCount)
            .WithName("GetUnreadNotificationCount");
    }

    private static async Task<IResult> GetNotifications(NotificationService notificationService, int? limit = null)
    {
        var notifications = await notificationService.GetRecentAsync(limit ?? 50);
        return Results.Ok(notifications);
    }

    private static async Task<IResult> MarkAsRead(Guid id, NotificationService notificationService)
    {
        var marked = await notificationService.MarkAsReadAsync(id);
        return marked ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> MarkAllAsRead(NotificationService notificationService)
    {
        var count = await notificationService.MarkAllAsReadAsync();
        return Results.Ok(new { markedCount = count });
    }

    private static async Task<IResult> GetUnreadCount(NotificationService notificationService)
    {
        var count = await notificationService.GetUnreadCountAsync();
        return Results.Ok(new { count });
    }
}
