using Forge.Api.Data;
using Forge.Api.Data.Entities;
using Forge.Api.Features.Events;
using Forge.Api.Features.Notifications;
using Forge.Api.Features.Tasks;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace Forge.Api.Features.Backlog;

public class BacklogService(ForgeDbContext db, ISseService sse, NotificationService notifications)
{
    public async Task<IReadOnlyList<BacklogItemDto>> GetAllAsync(Guid repositoryId)
    {
        var entities = await db.BacklogItems.AsNoTracking()
            .Where(b => b.RepositoryId == repositoryId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return entities.Select(e => BacklogItemDto.FromEntity(e)).ToList();
    }

    public async Task<BacklogItemDto?> GetByIdAsync(Guid id)
    {
        var entity = await db.BacklogItems.FindAsync(id);
        return entity is null ? null : BacklogItemDto.FromEntity(entity);
    }

    public async Task<BacklogItemDto> CreateAsync(Guid repositoryId, CreateBacklogItemDto dto)
    {
        var entity = new BacklogItemEntity
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            AcceptanceCriteria = dto.AcceptanceCriteria,
            State = BacklogItemState.New,
            HasError = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RepositoryId = repositoryId
        };

        db.BacklogItems.Add(entity);
        await db.SaveChangesAsync();

        var result = BacklogItemDto.FromEntity(entity);
        await sse.EmitBacklogItemCreatedAsync(result);
        return result;
    }

    public async Task<BacklogItemDto?> UpdateAsync(Guid id, UpdateBacklogItemDto dto)
    {
        var entity = await db.BacklogItems.FindAsync(id);
        if (entity is null) return null;

        if (dto.Title is not null) entity.Title = dto.Title;
        if (dto.Description is not null) entity.Description = dto.Description;
        if (dto.Priority.HasValue) entity.Priority = dto.Priority.Value;
        if (dto.AcceptanceCriteria is not null) entity.AcceptanceCriteria = dto.AcceptanceCriteria;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var result = BacklogItemDto.FromEntity(entity);
        await sse.EmitBacklogItemUpdatedAsync(result);
        return result;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await db.BacklogItems.FindAsync(id);
        if (entity is null) return false;

        db.BacklogItems.Remove(entity);
        await db.SaveChangesAsync();

        await sse.EmitBacklogItemDeletedAsync(id);
        return true;
    }

    public async Task<BacklogItemDto?> TransitionAsync(Guid id, TransitionBacklogItemDto dto)
    {
        var entity = await db.BacklogItems.FindAsync(id);
        if (entity is null) return null;

        // Cannot transition while there's a pending gate
        if (entity.HasPendingGate)
        {
            throw new InvalidOperationException("Cannot transition backlog item while there's a pending human gate.");
        }

        // Validate state transition
        if (!BacklogItemConstants.IsValidTransition(entity.State, dto.TargetState))
        {
            throw new InvalidOperationException(
                $"Cannot transition from {entity.State} to {dto.TargetState}.");
        }

        var previousState = entity.State;
        entity.State = dto.TargetState;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var result = BacklogItemDto.FromEntity(entity);
        await sse.EmitBacklogItemUpdatedAsync(result);
        await notifications.NotifyBacklogItemStateChangedAsync(entity.Id, entity.Title, previousState, dto.TargetState);

        return result;
    }

    public async Task<BacklogItemDto?> SetAgentAsync(Guid backlogItemId, string? agentId)
    {
        var entity = await db.BacklogItems.FindAsync(backlogItemId);
        if (entity is null) return null;

        entity.AssignedAgentId = agentId;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var result = BacklogItemDto.FromEntity(entity);
        await sse.EmitBacklogItemUpdatedAsync(result);
        return result;
    }

    public async Task<BacklogItemDto?> SetErrorAsync(Guid backlogItemId, string errorMessage)
    {
        var entity = await db.BacklogItems.FindAsync(backlogItemId);
        if (entity is null) return null;

        entity.HasError = true;
        entity.ErrorMessage = errorMessage;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var result = BacklogItemDto.FromEntity(entity);
        await sse.EmitBacklogItemUpdatedAsync(result);
        await notifications.NotifyBacklogItemErrorAsync(entity.Id, entity.Title, errorMessage);
        return result;
    }

    public async Task<BacklogItemDto?> ClearErrorAsync(Guid backlogItemId)
    {
        var entity = await db.BacklogItems.FindAsync(backlogItemId);
        if (entity is null) return null;

        entity.HasError = false;
        entity.ErrorMessage = null;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var result = BacklogItemDto.FromEntity(entity);
        await sse.EmitBacklogItemUpdatedAsync(result);
        return result;
    }

    public async Task<BacklogItemDto?> PauseAsync(Guid id, string? reason)
    {
        var entity = await db.BacklogItems.FindAsync(id);
        if (entity is null) return null;

        if (entity.IsPaused)
        {
            throw new InvalidOperationException("Backlog item is already paused.");
        }

        entity.IsPaused = true;
        entity.PauseReason = reason;
        entity.PausedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var result = BacklogItemDto.FromEntity(entity);
        await sse.EmitBacklogItemPausedAsync(result);
        return result;
    }

    public async Task<BacklogItemDto?> ResumeAsync(Guid id)
    {
        var entity = await db.BacklogItems.FindAsync(id);
        if (entity is null) return null;

        if (!entity.IsPaused)
        {
            throw new InvalidOperationException("Backlog item is not paused.");
        }

        entity.IsPaused = false;
        entity.PauseReason = null;
        entity.PausedAt = null;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var result = BacklogItemDto.FromEntity(entity);
        await sse.EmitBacklogItemResumedAsync(result);
        return result;
    }

    /// <summary>
    /// Updates the backlog item's state based on task completion.
    /// Called when a task is completed or when tasks are created.
    /// </summary>
    public async Task UpdateFromTasksAsync(Guid backlogItemId)
    {
        var entity = await db.BacklogItems
            .Include(b => b.Tasks)
            .FirstOrDefaultAsync(b => b.Id == backlogItemId);

        if (entity is null) return;

        var taskCount = entity.Tasks.Count;
        var completedCount = entity.Tasks.Count(t => t.State == PipelineState.Done);

        entity.TaskCount = taskCount;
        entity.CompletedTaskCount = completedCount;

        // If all tasks are done, move to Done state
        if (taskCount > 0 && completedCount == taskCount && entity.State == BacklogItemState.Executing)
        {
            entity.State = BacklogItemState.Done;
            await notifications.NotifyBacklogItemStateChangedAsync(
                entity.Id, entity.Title, BacklogItemState.Executing, BacklogItemState.Done);
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var result = BacklogItemDto.FromEntity(entity);
        await sse.EmitBacklogItemUpdatedAsync(result);
    }

    /// <summary>
    /// Creates tasks from split agent output.
    /// </summary>
    public async Task<IReadOnlyList<TaskDto>> CreateTasksFromSplitAsync(
        Guid backlogItemId,
        IReadOnlyList<CreateTaskDto> taskDtos)
    {
        var entity = await db.BacklogItems.FindAsync(backlogItemId);
        if (entity is null)
            throw new InvalidOperationException("Backlog item not found");

        if (entity.State != BacklogItemState.Splitting)
            throw new InvalidOperationException("Backlog item is not in Splitting state");

        var tasks = new List<TaskEntity>();
        var order = 1;

        foreach (var dto in taskDtos)
        {
            var task = new TaskEntity
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Description = dto.Description,
                Priority = dto.Priority,
                State = PipelineState.Research,
                BacklogItemId = backlogItemId,
                RepositoryId = entity.RepositoryId,
                ExecutionOrder = order++,
                HasError = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                // Inherit context from backlog item
                DetectedLanguage = entity.DetectedLanguage,
                DetectedFramework = entity.DetectedFramework
            };

            tasks.Add(task);
            db.Tasks.Add(task);
        }

        entity.TaskCount = tasks.Count;
        entity.State = BacklogItemState.Executing;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var taskDtosResult = tasks.Select(TaskDto.FromEntity).ToList();

        // Emit SSE events
        foreach (var taskDto in taskDtosResult)
        {
            await sse.EmitTaskCreatedAsync(taskDto);
        }

        var backlogDto = BacklogItemDto.FromEntity(entity);
        await sse.EmitBacklogItemUpdatedAsync(backlogDto);

        return taskDtosResult;
    }
}
