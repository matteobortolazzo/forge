using Forge.Api.Data.Entities;
using Forge.Api.Shared;

namespace Forge.Api.Features.Tasks;

public record TaskProgressDto(
    int Completed,
    int Total,
    int Percent);

public record TaskDto(
    Guid Id,
    string Title,
    string Description,
    PipelineState State,
    Priority Priority,
    string? AssignedAgentId,
    bool HasError,
    string? ErrorMessage,
    bool IsPaused,
    string? PauseReason,
    DateTime? PausedAt,
    int RetryCount,
    int MaxRetries,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    // Hierarchy fields
    Guid? ParentId,
    int ChildCount,
    PipelineState? DerivedState,
    IReadOnlyList<TaskDto>? Children = null,
    TaskProgressDto? Progress = null)
{
    public static TaskDto FromEntity(TaskEntity entity, IReadOnlyList<TaskDto>? children = null, TaskProgressDto? progress = null) => new(
        entity.Id,
        entity.Title,
        entity.Description,
        entity.State,
        entity.Priority,
        entity.AssignedAgentId,
        entity.HasError,
        entity.ErrorMessage,
        entity.IsPaused,
        entity.PauseReason,
        entity.PausedAt,
        entity.RetryCount,
        entity.MaxRetries,
        entity.CreatedAt,
        entity.UpdatedAt,
        entity.ParentId,
        entity.ChildCount,
        entity.DerivedState,
        children,
        progress);
}

public record TaskLogDto(
    Guid Id,
    Guid TaskId,
    LogType Type,
    string Content,
    string? ToolName,
    DateTime Timestamp)
{
    public static TaskLogDto FromEntity(TaskLogEntity entity) => new(
        entity.Id,
        entity.TaskId,
        entity.Type,
        entity.Content,
        entity.ToolName,
        entity.Timestamp);
}

public record CreateTaskDto(
    string Title,
    string Description,
    Priority Priority);

public record UpdateTaskDto(
    string? Title = null,
    string? Description = null,
    Priority? Priority = null);

public record TransitionTaskDto(
    PipelineState TargetState);

// Hierarchy DTOs
public record CreateSubtaskDto(
    string Title,
    string Description,
    Priority Priority);

public record SplitTaskDto(
    IReadOnlyList<CreateSubtaskDto> Subtasks);

public record SplitTaskResultDto(
    TaskDto Parent,
    IReadOnlyList<TaskDto> Children);
