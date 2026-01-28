using Forge.Api.Data;
using Forge.Api.Data.Entities;
using Forge.Api.Features.Events;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace Forge.Api.Features.Notifications;

public class NotificationService(ForgeDbContext db, ISseService sse)
{
    public async Task<IReadOnlyList<NotificationDto>> GetRecentAsync(int limit = 50)
    {
        var entities = await db.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();
        return entities.Select(NotificationDto.FromEntity).ToList();
    }

    public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto)
    {
        var entity = new NotificationEntity
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Message = dto.Message,
            Type = dto.Type,
            TaskId = dto.TaskId,
            BacklogItemId = dto.BacklogItemId,
            Read = false,
            CreatedAt = DateTime.UtcNow
        };

        db.Notifications.Add(entity);
        await db.SaveChangesAsync();

        var result = NotificationDto.FromEntity(entity);
        await sse.EmitNotificationNewAsync(result);
        return result;
    }

    public async Task<bool> MarkAsReadAsync(Guid id)
    {
        var entity = await db.Notifications.FindAsync(id);
        if (entity is null) return false;

        entity.Read = true;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<int> MarkAllAsReadAsync()
    {
        var count = await db.Notifications
            .Where(n => !n.Read)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Read, true));
        return count;
    }

    public async Task<int> GetUnreadCountAsync()
    {
        return await db.Notifications.CountAsync(n => !n.Read);
    }

    #region Task Notifications

    public async Task NotifyTaskStateChangedAsync(Guid taskId, string taskTitle, PipelineState fromState, PipelineState toState)
    {
        var type = toState == PipelineState.Done ? NotificationType.Success : NotificationType.Info;
        var title = toState == PipelineState.Done ? "Task Completed" : "Task State Changed";
        var message = toState == PipelineState.Done
            ? $"\"{taskTitle}\" completed"
            : $"\"{taskTitle}\" moved to {toState}";

        await CreateAsync(new CreateNotificationDto(title, message, type, taskId, null));
    }

    public async Task NotifyTaskErrorAsync(Guid taskId, string taskTitle, string errorMessage)
    {
        await CreateAsync(new CreateNotificationDto(
            "Task Error",
            $"Error on \"{taskTitle}\": {errorMessage}",
            NotificationType.Error,
            taskId,
            null));
    }

    public async Task NotifyTaskPausedAsync(Guid taskId, string taskTitle, string reason)
    {
        await CreateAsync(new CreateNotificationDto(
            "Task Paused",
            $"\"{taskTitle}\" was paused: {reason}",
            NotificationType.Warning,
            taskId,
            null));
    }

    #endregion

    #region Backlog Item Notifications

    public async Task NotifyBacklogItemStateChangedAsync(Guid backlogItemId, string title, BacklogItemState fromState, BacklogItemState toState)
    {
        var type = toState == BacklogItemState.Done ? NotificationType.Success : NotificationType.Info;
        var notificationTitle = toState == BacklogItemState.Done ? "Backlog Item Completed" : "Backlog Item State Changed";
        var message = toState == BacklogItemState.Done
            ? $"\"{title}\" completed"
            : $"\"{title}\" moved to {toState}";

        await CreateAsync(new CreateNotificationDto(notificationTitle, message, type, null, backlogItemId));
    }

    public async Task NotifyBacklogItemErrorAsync(Guid backlogItemId, string title, string errorMessage)
    {
        await CreateAsync(new CreateNotificationDto(
            "Backlog Item Error",
            $"Error on \"{title}\": {errorMessage}",
            NotificationType.Error,
            null,
            backlogItemId));
    }

    public async Task NotifyBacklogItemPausedAsync(Guid backlogItemId, string title, string reason)
    {
        await CreateAsync(new CreateNotificationDto(
            "Backlog Item Paused",
            $"\"{title}\" was paused: {reason}",
            NotificationType.Warning,
            null,
            backlogItemId));
    }

    #endregion
}
