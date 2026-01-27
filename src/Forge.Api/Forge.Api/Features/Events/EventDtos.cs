using Forge.Api.Shared;

namespace Forge.Api.Features.Events;

/// <summary>
/// DTO for artifact created events.
/// </summary>
public record ArtifactDto(
    Guid Id,
    Guid TaskId,
    PipelineState ProducedInState,
    ArtifactType ArtifactType,
    string Content,
    DateTime CreatedAt,
    string? AgentId,
    decimal? ConfidenceScore
);

/// <summary>
/// DTO for human gate events.
/// </summary>
public record HumanGateDto(
    Guid Id,
    Guid TaskId,
    Guid? SubtaskId,
    HumanGateType GateType,
    HumanGateStatus Status,
    decimal ConfidenceScore,
    string Reason,
    DateTime RequestedAt,
    DateTime? ResolvedAt,
    string? ResolvedBy,
    string? Resolution
);

/// <summary>
/// DTO for subtask events.
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
    string? WorktreePath,
    decimal? ConfidenceScore,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? FailureReason
);

/// <summary>
/// DTO for rollback events.
/// </summary>
public record RollbackDto(
    Guid Id,
    Guid? TaskId,
    Guid? SubtaskId,
    RollbackTrigger Trigger,
    DateTime Timestamp,
    RollbackStateBefore StateBefore,
    RollbackActionTaken ActionTaken,
    IReadOnlyList<PreservedArtifact> PreservedArtifacts,
    IReadOnlyList<string> RecoveryOptions
);

/// <summary>
/// State before rollback was initiated.
/// </summary>
public record RollbackStateBefore(
    string? Branch,
    string? Commit,
    IReadOnlyList<string> FilesChanged
);

/// <summary>
/// Actions taken during rollback.
/// </summary>
public record RollbackActionTaken(
    bool WorktreeRemoved,
    bool BranchDeleted,
    IReadOnlyList<string> CommitsReverted
);

/// <summary>
/// Artifact preserved during rollback.
/// </summary>
public record PreservedArtifact(
    string Stage,
    string Path
);
