using Forge.Api.Shared;

namespace Forge.Api.Features.Subtasks;

/// <summary>
/// DTO for subtask responses.
/// </summary>
public record SubtaskDto(
    Guid Id,
    Guid ParentTaskId,
    string Title,
    string Description,
    IReadOnlyList<string> AcceptanceCriteria,
    EstimatedScope EstimatedScope,
    IReadOnlyList<Guid> Dependencies,
    int ExecutionOrder,
    SubtaskStatus Status,
    PipelineState CurrentStage,
    string? WorktreePath,
    string? BranchName,
    decimal? ConfidenceScore,
    int ImplementationRetries,
    int SimplificationIterations,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? FailureReason
);

/// <summary>
/// DTO for creating a subtask.
/// </summary>
public record CreateSubtaskDto(
    string Title,
    string Description,
    IReadOnlyList<string>? AcceptanceCriteria = null,
    EstimatedScope EstimatedScope = EstimatedScope.Medium,
    IReadOnlyList<Guid>? Dependencies = null,
    int? ExecutionOrder = null
);

/// <summary>
/// DTO for updating a subtask.
/// </summary>
public record UpdateSubtaskDto(
    string? Title = null,
    string? Description = null,
    IReadOnlyList<string>? AcceptanceCriteria = null,
    EstimatedScope? EstimatedScope = null,
    IReadOnlyList<Guid>? Dependencies = null,
    int? ExecutionOrder = null,
    SubtaskStatus? Status = null
);
