using Forge.Api.Data.Entities;
using Forge.Api.Shared;

namespace Forge.Api.Features.Tasks;

public record TaskDto(
    Guid Id,
    Guid RepositoryId,
    Guid BacklogItemId,
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
    int ExecutionOrder,
    decimal? ConfidenceScore,
    bool HasPendingGate)
{
    public static TaskDto FromEntity(TaskEntity entity) => new(
        entity.Id,
        entity.RepositoryId,
        entity.BacklogItemId,
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
        entity.ExecutionOrder,
        entity.ConfidenceScore,
        entity.HasPendingGate);
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
        entity.TaskId ?? Guid.Empty,
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

public record PauseTaskDto(
    string? Reason = null);
