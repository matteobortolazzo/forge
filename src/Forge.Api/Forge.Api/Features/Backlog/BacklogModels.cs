using Forge.Api.Data.Entities;
using Forge.Api.Shared;

namespace Forge.Api.Features.Backlog;

public record BacklogItemProgressDto(
    int Completed,
    int Total,
    int Percent);

public record BacklogItemDto(
    Guid Id,
    Guid RepositoryId,
    string Title,
    string Description,
    BacklogItemState State,
    Priority Priority,
    string? AcceptanceCriteria,
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
    int TaskCount,
    int CompletedTaskCount,
    decimal? ConfidenceScore,
    bool HasPendingGate,
    int RefiningIterations,
    BacklogItemProgressDto? Progress = null)
{
    public static BacklogItemDto FromEntity(BacklogItemEntity entity, BacklogItemProgressDto? progress = null) => new(
        entity.Id,
        entity.RepositoryId,
        entity.Title,
        entity.Description,
        entity.State,
        entity.Priority,
        entity.AcceptanceCriteria,
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
        entity.TaskCount,
        entity.CompletedTaskCount,
        entity.ConfidenceScore,
        entity.HasPendingGate,
        entity.RefiningIterations,
        progress ?? (entity.TaskCount > 0
            ? new BacklogItemProgressDto(
                entity.CompletedTaskCount,
                entity.TaskCount,
                entity.TaskCount > 0 ? (entity.CompletedTaskCount * 100) / entity.TaskCount : 0)
            : null));
}

public record CreateBacklogItemDto(
    string Title,
    string Description,
    Priority Priority = Priority.Medium,
    string? AcceptanceCriteria = null);

public record UpdateBacklogItemDto(
    string? Title = null,
    string? Description = null,
    Priority? Priority = null,
    string? AcceptanceCriteria = null);

public record TransitionBacklogItemDto(
    BacklogItemState TargetState);

public record PauseBacklogItemDto(
    string? Reason = null);
