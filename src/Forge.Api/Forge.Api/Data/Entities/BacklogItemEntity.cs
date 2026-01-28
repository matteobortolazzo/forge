using Forge.Api.Shared;

namespace Forge.Api.Data.Entities;

/// <summary>
/// Represents a high-level work item that gets refined and split into executable tasks.
/// BacklogItems are user-facing requests that progress through refinement before becoming tasks.
/// </summary>
public class BacklogItemEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// Brief title for the backlog item.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Detailed description of what needs to be done.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Current state in the backlog item lifecycle.
    /// </summary>
    public BacklogItemState State { get; set; } = BacklogItemState.New;

    /// <summary>
    /// Priority level for scheduling.
    /// </summary>
    public Priority Priority { get; set; } = Priority.Medium;

    /// <summary>
    /// Acceptance criteria (markdown or JSON array of criteria).
    /// </summary>
    public string? AcceptanceCriteria { get; set; }

    // Repository association
    public Guid RepositoryId { get; set; }
    public RepositoryEntity Repository { get; set; } = null!;

    // Agent context detection (auto-detected or user-specified)
    public string? DetectedLanguage { get; set; }    // e.g., "csharp", "typescript"
    public string? DetectedFramework { get; set; }   // e.g., "angular", "dotnet"

    // Confidence and human gate tracking
    public decimal? ConfidenceScore { get; set; }       // Agent-reported confidence (0.0-1.0)
    public bool HumanInputRequested { get; set; }       // Whether human input is needed
    public string? HumanInputReason { get; set; }       // Reason for requesting human input
    public bool HasPendingGate { get; set; }            // Whether there's a pending human gate

    // Refining iteration tracking
    public int RefiningIterations { get; set; }         // Number of refining loops

    // Agent assignment
    public string? AssignedAgentId { get; set; }

    // Error tracking
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }

    // Scheduling fields
    public bool IsPaused { get; set; }
    public string? PauseReason { get; set; }
    public DateTime? PausedAt { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Denormalized for query efficiency
    public int TaskCount { get; set; } = 0;              // Number of tasks created from split
    public int CompletedTaskCount { get; set; } = 0;     // Number of completed tasks

    // Navigation properties
    public ICollection<TaskEntity> Tasks { get; set; } = [];
    public ICollection<AgentArtifactEntity> Artifacts { get; set; } = [];
    public ICollection<HumanGateEntity> HumanGates { get; set; } = [];
    public ICollection<TaskLogEntity> Logs { get; set; } = [];
}
