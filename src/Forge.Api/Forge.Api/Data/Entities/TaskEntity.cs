using Forge.Api.Shared;

namespace Forge.Api.Data.Entities;

public class TaskEntity
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public PipelineState State { get; set; } = PipelineState.Backlog;
    public Priority Priority { get; set; } = Priority.Medium;
    public string? AssignedAgentId { get; set; }
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Scheduling fields
    public bool IsPaused { get; set; }
    public string? PauseReason { get; set; }
    public DateTime? PausedAt { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;

    // Hierarchy fields
    public Guid? ParentId { get; set; }
    public TaskEntity? Parent { get; set; }
    public ICollection<TaskEntity> Children { get; set; } = [];

    // Denormalized for query efficiency
    public int ChildCount { get; set; } = 0;
    public PipelineState? DerivedState { get; set; }  // Computed for parents

    // Agent context detection
    public string? DetectedLanguage { get; set; }    // Auto-detected or user-specified (e.g., "csharp", "typescript")
    public string? DetectedFramework { get; set; }   // Auto-detected or user-specified (e.g., "angular", "dotnet")
    public PipelineState? RecommendedNextState { get; set; }  // Agent's recommendation for next state

    // Navigation properties
    public ICollection<TaskLogEntity> Logs { get; set; } = [];
    public ICollection<AgentArtifactEntity> Artifacts { get; set; } = [];
}
