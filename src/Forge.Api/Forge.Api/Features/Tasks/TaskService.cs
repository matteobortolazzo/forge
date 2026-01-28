using Forge.Api.Data;
using Forge.Api.Data.Entities;
using Forge.Api.Features.Backlog;
using Forge.Api.Features.Events;
using Forge.Api.Features.Notifications;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace Forge.Api.Features.Tasks;

public class TaskService(ForgeDbContext db, ISseService sse, NotificationService notifications, ILogger<TaskService> logger)
{
    public async Task<IReadOnlyList<TaskDto>> GetAllAsync(Guid backlogItemId)
    {
        var entities = await db.Tasks.AsNoTracking()
            .Where(t => t.BacklogItemId == backlogItemId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return entities.Select(TaskDto.FromEntity).ToList();
    }

    public async Task<IReadOnlyList<TaskDto>> GetAllByRepositoryAsync(Guid repositoryId)
    {
        var entities = await db.Tasks.AsNoTracking()
            .Where(t => t.RepositoryId == repositoryId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return entities.Select(TaskDto.FromEntity).ToList();
    }

    public async Task<TaskDto?> GetByIdAsync(Guid id)
    {
        var entity = await db.Tasks.FindAsync(id);
        return entity is null ? null : TaskDto.FromEntity(entity);
    }

    public async Task<TaskDto?> UpdateAsync(Guid id, UpdateTaskDto dto)
    {
        var entity = await db.Tasks.FindAsync(id);
        if (entity is null) return null;

        if (dto.Title is not null) entity.Title = dto.Title;
        if (dto.Description is not null) entity.Description = dto.Description;
        if (dto.Priority.HasValue) entity.Priority = dto.Priority.Value;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var result = TaskDto.FromEntity(entity);
        await sse.EmitTaskUpdatedAsync(result);
        return result;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await db.Tasks.FindAsync(id);
        if (entity is null) return false;

        var backlogItemId = entity.BacklogItemId;

        db.Tasks.Remove(entity);
        await db.SaveChangesAsync();

        logger.LogInformation("Task {TaskId} deleted", id);

        await sse.EmitTaskDeletedAsync(id);

        // Update backlog item task count
        await UpdateBacklogItemTaskCountAsync(backlogItemId);

        return true;
    }

    public async Task<TaskDto?> TransitionAsync(Guid id, TransitionTaskDto dto)
    {
        var entity = await db.Tasks.FindAsync(id);
        if (entity is null) return null;

        // Cannot transition while there's a pending gate
        if (entity.HasPendingGate)
        {
            throw new InvalidOperationException("Cannot transition task while there's a pending human gate.");
        }

        // Validate adjacent state transition
        if (!PipelineConstants.IsValidTransition(entity.State, dto.TargetState))
        {
            throw new InvalidOperationException(
                $"Cannot transition from {entity.State} to {dto.TargetState}. Only adjacent state transitions are allowed.");
        }

        var previousState = entity.State;
        entity.State = dto.TargetState;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        logger.LogInformation("Task {TaskId} transitioned from {FromState} to {ToState}", id, previousState, dto.TargetState);

        var result = TaskDto.FromEntity(entity);
        await sse.EmitTaskUpdatedAsync(result);
        await notifications.NotifyTaskStateChangedAsync(entity.Id, entity.Title, previousState, dto.TargetState);

        // Update backlog item if task is now Done
        if (dto.TargetState == PipelineState.Done)
        {
            await UpdateBacklogItemTaskCountAsync(entity.BacklogItemId);
        }

        return result;
    }

    public async Task<IReadOnlyList<TaskLogDto>> GetLogsAsync(Guid taskId)
    {
        var logs = await db.TaskLogs
            .AsNoTracking()
            .Where(l => l.TaskId == taskId)
            .OrderBy(l => l.Timestamp)
            .ToListAsync();
        return logs.Select(TaskLogDto.FromEntity).ToList();
    }

    public async Task<TaskLogDto> AddLogAsync(Guid taskId, LogType type, string content, string? toolName = null)
    {
        var entity = new TaskLogEntity
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            Type = type,
            Content = content,
            ToolName = toolName,
            Timestamp = DateTime.UtcNow
        };

        db.TaskLogs.Add(entity);
        await db.SaveChangesAsync();

        var result = TaskLogDto.FromEntity(entity);
        await sse.EmitTaskLogAsync(result);
        return result;
    }

    public async Task<TaskDto?> SetAgentAsync(Guid taskId, string? agentId)
    {
        var entity = await db.Tasks.FindAsync(taskId);
        if (entity is null) return null;

        entity.AssignedAgentId = agentId;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        logger.LogDebug("Task {TaskId} assigned to agent {AgentId}", taskId, agentId ?? "(none)");

        var result = TaskDto.FromEntity(entity);
        await sse.EmitTaskUpdatedAsync(result);
        return result;
    }

    public async Task<TaskDto?> SetErrorAsync(Guid taskId, string errorMessage)
    {
        var entity = await db.Tasks.FindAsync(taskId);
        if (entity is null) return null;

        entity.HasError = true;
        entity.ErrorMessage = errorMessage;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        logger.LogWarning("Task {TaskId} error: {ErrorMessage}", taskId, errorMessage);

        var result = TaskDto.FromEntity(entity);
        await sse.EmitTaskUpdatedAsync(result);
        await notifications.NotifyTaskErrorAsync(entity.Id, entity.Title, errorMessage);
        return result;
    }

    public async Task<TaskDto?> ClearErrorAsync(Guid taskId)
    {
        var entity = await db.Tasks.FindAsync(taskId);
        if (entity is null) return null;

        entity.HasError = false;
        entity.ErrorMessage = null;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var result = TaskDto.FromEntity(entity);
        await sse.EmitTaskUpdatedAsync(result);
        return result;
    }

    public async Task<TaskDto?> PauseAsync(Guid id, string? reason)
    {
        var entity = await db.Tasks.FindAsync(id);
        if (entity is null) return null;

        if (entity.IsPaused)
        {
            throw new InvalidOperationException("Task is already paused.");
        }

        entity.IsPaused = true;
        entity.PauseReason = reason;
        entity.PausedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        logger.LogInformation("Task {TaskId} paused: {Reason}", id, reason ?? "(no reason)");

        var result = TaskDto.FromEntity(entity);
        await sse.EmitTaskPausedAsync(result);
        return result;
    }

    public async Task<TaskDto?> ResumeAsync(Guid id)
    {
        var entity = await db.Tasks.FindAsync(id);
        if (entity is null) return null;

        if (!entity.IsPaused)
        {
            throw new InvalidOperationException("Task is not paused.");
        }

        entity.IsPaused = false;
        entity.PauseReason = null;
        entity.PausedAt = null;
        entity.HasError = false;
        entity.ErrorMessage = null;
        entity.RetryCount = 0;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        logger.LogInformation("Task {TaskId} resumed", id);

        var result = TaskDto.FromEntity(entity);
        await sse.EmitTaskResumedAsync(result);
        return result;
    }

    private async Task UpdateBacklogItemTaskCountAsync(Guid backlogItemId)
    {
        var backlogItem = await db.BacklogItems
            .Include(b => b.Tasks)
            .FirstOrDefaultAsync(b => b.Id == backlogItemId);

        if (backlogItem is null) return;

        backlogItem.TaskCount = backlogItem.Tasks.Count;
        backlogItem.CompletedTaskCount = backlogItem.Tasks.Count(t => t.State == PipelineState.Done);

        // If all tasks are done, transition backlog item to Done
        if (backlogItem.TaskCount > 0 &&
            backlogItem.CompletedTaskCount == backlogItem.TaskCount &&
            backlogItem.State == BacklogItemState.Executing)
        {
            backlogItem.State = BacklogItemState.Done;
            await notifications.NotifyBacklogItemStateChangedAsync(
                backlogItem.Id, backlogItem.Title, BacklogItemState.Executing, BacklogItemState.Done);
        }

        backlogItem.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var backlogDto = BacklogItemDto.FromEntity(backlogItem);
        await sse.EmitBacklogItemUpdatedAsync(backlogDto);
    }
}
