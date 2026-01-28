using Forge.Api.Data.Entities;
using Forge.Api.Shared;

namespace Forge.Api.Features.Notifications;

public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    NotificationType Type,
    Guid? TaskId,
    Guid? BacklogItemId,
    bool Read,
    DateTime CreatedAt)
{
    public static NotificationDto FromEntity(NotificationEntity entity) => new(
        entity.Id,
        entity.Title,
        entity.Message,
        entity.Type,
        entity.TaskId,
        entity.BacklogItemId,
        entity.Read,
        entity.CreatedAt);
}

public record CreateNotificationDto(
    string Title,
    string Message,
    NotificationType Type,
    Guid? TaskId = null,
    Guid? BacklogItemId = null);
