using Forge.Api.Data.Entities;
using Forge.Api.Shared;

namespace Forge.Api.Features.Tasks;

public record TaskDto(
    Guid Id,
    string Title,
    string Description,
    PipelineState State,
    Priority Priority,
    string? AssignedAgentId,
    bool HasError,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static TaskDto FromEntity(TaskEntity entity) => new(
        entity.Id,
        entity.Title,
        entity.Description,
        entity.State,
        entity.Priority,
        entity.AssignedAgentId,
        entity.HasError,
        entity.ErrorMessage,
        entity.CreatedAt,
        entity.UpdatedAt);
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
