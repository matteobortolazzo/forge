using Forge.Api.Shared;

namespace Forge.Api.Data.Entities;

/// <summary>
/// Represents a concrete, schedulable implementation task.
/// Tasks are leaf units created from BacklogItem splitting.
/// All tasks are directly schedulable (no hierarchy).
/// </summary>
public class TaskEntity
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public PipelineState State { get; set; } = PipelineState.Research;
    public Priority Priority { get; set; } = Priority.Medium;
    public string? AssignedAgentId { get; set; }
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Repository association
    public Guid RepositoryId { get; set; }
    public RepositoryEntity Repository { get; set; } = null!;

    // BacklogItem association (required - all tasks belong to a backlog item)
    public Guid BacklogItemId { get; set; }
    public BacklogItemEntity BacklogItem { get; set; } = null!;

    // Execution order within the backlog item (1-based)
    public int ExecutionOrder { get; set; }

    // Scheduling fields
    public bool IsPaused { get; set; }
    public string? PauseReason { get; set; }
    public DateTime? PausedAt { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;

    // Agent context detection (inherited from BacklogItem or detected)
    public string? DetectedLanguage { get; set; }    // Auto-detected or user-specified (e.g., "csharp", "typescript")
    public string? DetectedFramework { get; set; }   // Auto-detected or user-specified (e.g., "angular", "dotnet")
    public PipelineState? RecommendedNextState { get; set; }  // Agent's recommendation for next state

    // Confidence and human gate tracking
    public decimal? ConfidenceScore { get; set; }       // Current overall confidence score
    public bool HumanInputRequested { get; set; }       // Whether human input is needed
    public string? HumanInputReason { get; set; }       // Reason for requesting human input
    public bool HasPendingGate { get; set; }            // Whether there's a pending human gate

    // Pipeline iteration tracking
    public int ImplementationRetries { get; set; }      // Current implementation retry count
    public int SimplificationIterations { get; set; }   // Current simplification iteration count

    // Navigation properties
    public ICollection<TaskLogEntity> Logs { get; set; } = [];
    public ICollection<AgentArtifactEntity> Artifacts { get; set; } = [];
    public ICollection<HumanGateEntity> HumanGates { get; set; } = [];
}
