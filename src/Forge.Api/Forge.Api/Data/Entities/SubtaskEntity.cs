using Forge.Api.Shared;

namespace Forge.Api.Data.Entities;

/// <summary>
/// Represents a subtask created during the Split stage.
/// Subtasks are executed sequentially in isolated git worktrees.
/// </summary>
public class SubtaskEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// The parent task this subtask belongs to.
    /// </summary>
    public Guid ParentTaskId { get; set; }

    /// <summary>
    /// Brief, actionable title for the subtask.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Detailed description of what needs to be done.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Acceptance criteria as a JSON array of strings.
    /// </summary>
    public string AcceptanceCriteriaJson { get; set; } = "[]";

    /// <summary>
    /// Estimated scope (small/medium/large).
    /// </summary>
    public EstimatedScope EstimatedScope { get; set; } = EstimatedScope.Medium;

    /// <summary>
    /// IDs of other subtasks this one depends on (JSON array).
    /// </summary>
    public string DependenciesJson { get; set; } = "[]";

    /// <summary>
    /// Execution order (1-based).
    /// </summary>
    public int ExecutionOrder { get; set; }

    /// <summary>
    /// Current status of the subtask.
    /// </summary>
    public SubtaskStatus Status { get; set; } = SubtaskStatus.Pending;

    /// <summary>
    /// Path to the git worktree for this subtask (when active).
    /// </summary>
    public string? WorktreePath { get; set; }

    /// <summary>
    /// Git branch name for this subtask.
    /// </summary>
    public string? BranchName { get; set; }

    /// <summary>
    /// Agent-reported confidence score for this subtask (0.0-1.0).
    /// </summary>
    public decimal? ConfidenceScore { get; set; }

    /// <summary>
    /// Current pipeline stage of the subtask.
    /// </summary>
    public PipelineState CurrentStage { get; set; } = PipelineState.Research;

    /// <summary>
    /// Number of implementation retry attempts.
    /// </summary>
    public int ImplementationRetries { get; set; }

    /// <summary>
    /// Number of simplification review iterations.
    /// </summary>
    public int SimplificationIterations { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Reason for failure (if Status is Failed).
    /// </summary>
    public string? FailureReason { get; set; }

    // Navigation properties
    public TaskEntity? ParentTask { get; set; }
    public ICollection<AgentArtifactEntity> Artifacts { get; set; } = [];
    public ICollection<HumanGateEntity> HumanGates { get; set; } = [];
}
