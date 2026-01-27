using Forge.Api.Shared;

namespace Forge.Api.Data.Entities;

/// <summary>
/// Records rollback actions for audit and recovery purposes.
/// </summary>
public class RollbackRecordEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// The task associated with this rollback.
    /// </summary>
    public Guid? TaskId { get; set; }

    /// <summary>
    /// The subtask associated with this rollback (if applicable).
    /// </summary>
    public Guid? SubtaskId { get; set; }

    /// <summary>
    /// What triggered the rollback.
    /// </summary>
    public RollbackTrigger Trigger { get; set; }

    /// <summary>
    /// When the rollback occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// State before rollback (JSON: branch, commit, files_changed).
    /// </summary>
    public string StateBeforeJson { get; set; } = "{}";

    /// <summary>
    /// Actions taken during rollback (JSON: worktree_removed, branch_deleted, commits_reverted).
    /// </summary>
    public string ActionTakenJson { get; set; } = "{}";

    /// <summary>
    /// Preserved artifacts (JSON array of {stage, path}).
    /// </summary>
    public string PreservedArtifactsJson { get; set; } = "[]";

    /// <summary>
    /// Available recovery options (JSON array of strings).
    /// </summary>
    public string RecoveryOptionsJson { get; set; } = "[]";

    /// <summary>
    /// Optional notes about the rollback.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public TaskEntity? Task { get; set; }
    public SubtaskEntity? Subtask { get; set; }
}
