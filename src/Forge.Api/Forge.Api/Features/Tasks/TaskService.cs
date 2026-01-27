using Forge.Api.Data;
using Forge.Api.Data.Entities;
using Forge.Api.Features.Events;
using Forge.Api.Features.Notifications;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace Forge.Api.Features.Tasks;

public class TaskService(ForgeDbContext db, ISseService sse, NotificationService notifications, IParentStateService parentStateService)
{

    /// <summary>
    /// Computes derived state from children states.
    /// Parent state = minimum progress state among children (excluding Done, unless all are Done).
    /// </summary>
    public static PipelineState ComputeDerivedState(IEnumerable<PipelineState> childStates)
    {
        var states = childStates.ToList();
        if (states.Count == 0)
            return PipelineState.Backlog;

        // If all children are Done, parent is Done
        if (states.All(s => s == PipelineState.Done))
            return PipelineState.Done;

        // Return minimum progress state (excluding Done)
        var nonDoneStates = states.Where(s => s != PipelineState.Done).ToList();
        if (nonDoneStates.Count == 0)
            return PipelineState.Done;

        return nonDoneStates.MinBy(s => PipelineConstants.GetStateIndex(s));
    }

    /// <summary>
    /// Computes progress information for a parent task.
    /// </summary>
    public static TaskProgressDto ComputeProgress(IEnumerable<PipelineState> childStates)
    {
        var states = childStates.ToList();
        if (states.Count == 0)
            return new TaskProgressDto(0, 0, 0);

        var completed = states.Count(s => s == PipelineState.Done);
        var total = states.Count;
        var percent = total > 0 ? (completed * 100) / total : 0;

        return new TaskProgressDto(completed, total, percent);
    }

    public async Task<IReadOnlyList<TaskDto>> GetAllAsync(bool rootOnly = false)
    {
        var query = db.Tasks.AsNoTracking();

        if (rootOnly)
        {
            query = query.Where(t => t.ParentId == null);
        }

        var entities = await query
            .Include(t => t.Children)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return entities.Select(e =>
        {
            var childDtos = e.Children.Count > 0
                ? e.Children.Select(c => TaskDto.FromEntity(c)).ToList()
                : null;
            var progress = e.ChildCount > 0
                ? ComputeProgress(e.Children.Select(c => c.State))
                : null;
            return TaskDto.FromEntity(e, childDtos, progress);
        }).ToList();
    }

    public async Task<TaskDto?> GetByIdAsync(Guid id, bool includeChildren = false)
    {
        var query = db.Tasks.AsQueryable();

        if (includeChildren)
        {
            query = query.Include(t => t.Children);
        }

        var entity = await query.FirstOrDefaultAsync(t => t.Id == id);
        if (entity is null) return null;

        var childDtos = entity.Children?.Count > 0
            ? entity.Children.Select(c => TaskDto.FromEntity(c)).ToList()
            : null;
        var progress = entity.ChildCount > 0
            ? ComputeProgress(entity.Children?.Select(c => c.State) ?? [])
            : null;

        return TaskDto.FromEntity(entity, childDtos, progress);
    }

    public async Task<IReadOnlyList<TaskDto>> GetChildrenAsync(Guid parentId)
    {
        var children = await db.Tasks
            .AsNoTracking()
            .Where(t => t.ParentId == parentId)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        return children.Select(e => TaskDto.FromEntity(e)).ToList();
    }

    public async Task<TaskDto> CreateAsync(CreateTaskDto dto)
    {
        var entity = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            State = PipelineState.Backlog,
            HasError = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Tasks.Add(entity);
        await db.SaveChangesAsync();

        var result = TaskDto.FromEntity(entity);
        await sse.EmitTaskCreatedAsync(result);
        return result;
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

        db.Tasks.Remove(entity);
        await db.SaveChangesAsync();

        await sse.EmitTaskDeletedAsync(id);
        return true;
    }

    public async Task<TaskDto?> TransitionAsync(Guid id, TransitionTaskDto dto)
    {
        var entity = await db.Tasks.FindAsync(id);
        if (entity is null) return null;

        // Parent tasks cannot be transitioned directly - their state is derived
        if (entity.ChildCount > 0)
        {
            throw new InvalidOperationException(
                "Cannot transition a parent task. Parent state is derived from children.");
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

        var result = TaskDto.FromEntity(entity);
        await sse.EmitTaskUpdatedAsync(result);
        await notifications.NotifyTaskStateChangedAsync(entity.Id, entity.Title, previousState, dto.TargetState);

        // Update parent's derived state if this is a child task
        if (entity.ParentId is not null)
        {
            await parentStateService.UpdateFromChildAsync(id);
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

    /// <summary>
    /// Splits a task into subtasks, converting the original task into a parent.
    /// </summary>
    public async Task<SplitTaskResultDto?> SplitTaskAsync(Guid taskId, SplitTaskDto dto)
    {
        var parent = await db.Tasks.FindAsync(taskId);
        if (parent is null) return null;

        // Cannot split a task that already has children
        if (parent.ChildCount > 0)
        {
            throw new InvalidOperationException("Task already has children and cannot be split again.");
        }

        // Cannot split a task that is a child
        if (parent.ParentId is not null)
        {
            throw new InvalidOperationException("Cannot split a subtask. Only root tasks can be split.");
        }

        // Create children
        var children = new List<TaskEntity>();
        foreach (var subtask in dto.Subtasks)
        {
            var child = new TaskEntity
            {
                Id = Guid.NewGuid(),
                Title = subtask.Title,
                Description = subtask.Description,
                Priority = subtask.Priority,
                State = PipelineState.Backlog,
                ParentId = parent.Id,
                HasError = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            children.Add(child);
            db.Tasks.Add(child);
        }

        // Update parent
        parent.ChildCount = children.Count;
        parent.DerivedState = ComputeDerivedState(children.Select(c => c.State));
        parent.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var childDtos = children.Select(c => TaskDto.FromEntity(c)).ToList();
        var progress = ComputeProgress(children.Select(c => c.State));
        var parentDto = TaskDto.FromEntity(parent, childDtos, progress);

        // Emit SSE events
        await sse.EmitTaskSplitAsync(parentDto, childDtos);

        return new SplitTaskResultDto(parentDto, childDtos);
    }

    /// <summary>
    /// Adds a single child task to a parent.
    /// </summary>
    public async Task<TaskDto?> AddChildAsync(Guid parentId, CreateSubtaskDto dto)
    {
        var parent = await db.Tasks
            .Include(t => t.Children)
            .FirstOrDefaultAsync(t => t.Id == parentId);

        if (parent is null) return null;

        // Cannot add child to a task that is itself a child
        if (parent.ParentId is not null)
        {
            throw new InvalidOperationException("Cannot add children to a subtask. Max depth is 2 levels.");
        }

        var child = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            State = PipelineState.Backlog,
            ParentId = parent.Id,
            HasError = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Tasks.Add(child);

        // Update parent
        parent.ChildCount++;
        var allChildStates = parent.Children.Select(c => c.State).Append(child.State);
        parent.DerivedState = ComputeDerivedState(allChildStates);
        parent.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var childDto = TaskDto.FromEntity(child);
        await sse.EmitChildAddedAsync(parentId, childDto);

        // Also emit parent update
        var progress = ComputeProgress(parent.Children.Select(c => c.State).Append(child.State));
        var parentDto = TaskDto.FromEntity(parent, null, progress);
        await sse.EmitTaskUpdatedAsync(parentDto);

        return childDto;
    }

    /// <summary>
    /// Checks if a task is a leaf task (has no children).
    /// </summary>
    public async Task<bool> IsLeafTaskAsync(Guid taskId)
    {
        var entity = await db.Tasks.FindAsync(taskId);
        return entity is not null && entity.ChildCount == 0;
    }
}
