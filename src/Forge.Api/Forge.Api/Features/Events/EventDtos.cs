using Forge.Api.Shared;

namespace Forge.Api.Features.Events;

/// <summary>
/// DTO for artifact created events.
/// </summary>
public record ArtifactDto(
    Guid Id,
    Guid? TaskId,
    Guid? BacklogItemId,
    PipelineState? ProducedInState,
    BacklogItemState? ProducedInBacklogState,
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
    Guid? TaskId,
    Guid? BacklogItemId,
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
/// DTO for backlog item log events.
/// </summary>
public record BacklogItemLogDto(
    Guid Id,
    Guid BacklogItemId,
    LogType Type,
    string Content,
    string? ToolName,
    DateTime Timestamp
);

/// <summary>
/// DTO for rollback events.
/// </summary>
public record RollbackDto(
    Guid Id,
    Guid? TaskId,
    Guid? BacklogItemId,
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
