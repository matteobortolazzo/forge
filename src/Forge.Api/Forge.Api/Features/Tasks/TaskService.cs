using Forge.Api.Data;
using Forge.Api.Data.Entities;
using Forge.Api.Features.Events;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace Forge.Api.Features.Tasks;

public class TaskService(ForgeDbContext db, ISseService sse)
{
    private static readonly PipelineState[] StateOrder =
    [
        PipelineState.Backlog,
        PipelineState.Planning,
        PipelineState.Implementing,
        PipelineState.Reviewing,
        PipelineState.Testing,
        PipelineState.PrReady,
        PipelineState.Done
    ];

    public async Task<IReadOnlyList<TaskDto>> GetAllAsync()
    {
        var entities = await db.Tasks
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        return entities.Select(TaskDto.FromEntity).ToList();
    }

    public async Task<TaskDto?> GetByIdAsync(Guid id)
    {
        var entity = await db.Tasks.FindAsync(id);
        return entity is null ? null : TaskDto.FromEntity(entity);
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

        // Validate adjacent state transition
        var currentIndex = Array.IndexOf(StateOrder, entity.State);
        var targetIndex = Array.IndexOf(StateOrder, dto.TargetState);

        if (Math.Abs(targetIndex - currentIndex) != 1)
        {
            throw new InvalidOperationException(
                $"Cannot transition from {entity.State} to {dto.TargetState}. Only adjacent state transitions are allowed.");
        }

        entity.State = dto.TargetState;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var result = TaskDto.FromEntity(entity);
        await sse.EmitTaskUpdatedAsync(result);
        return result;
    }

    public async Task<IReadOnlyList<TaskLogDto>> GetLogsAsync(Guid taskId)
    {
        var logs = await db.TaskLogs
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
}
