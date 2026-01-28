using Forge.Api.Shared;

namespace Forge.Api.Data.Entities;

/// <summary>
/// Stores structured output (artifacts) from each agent execution.
/// Artifacts are passed between pipeline stages to provide context.
/// Can belong to either a BacklogItem (for refining/splitting) or a Task (for implementation).
/// </summary>
public class AgentArtifactEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// The task this artifact belongs to (null for backlog item artifacts).
    /// </summary>
    public Guid? TaskId { get; set; }

    /// <summary>
    /// The backlog item this artifact belongs to (null for task artifacts).
    /// </summary>
    public Guid? BacklogItemId { get; set; }

    /// <summary>
    /// The pipeline state in which this artifact was produced (for task artifacts).
    /// </summary>
    public PipelineState? ProducedInState { get; set; }

    /// <summary>
    /// The backlog item state in which this artifact was produced (for backlog item artifacts).
    /// </summary>
    public BacklogItemState? ProducedInBacklogState { get; set; }

    /// <summary>
    /// The type of artifact (plan, implementation, review, test, general).
    /// </summary>
    public ArtifactType ArtifactType { get; set; }

    /// <summary>
    /// The markdown content of the artifact.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// When this artifact was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The ID of the agent configuration that produced this artifact.
    /// </summary>
    public string? AgentId { get; set; }

    /// <summary>
    /// Agent-reported confidence score for this artifact (0.0-1.0).
    /// </summary>
    public decimal? ConfidenceScore { get; set; }

    /// <summary>
    /// Whether the agent requested human input for this artifact.
    /// </summary>
    public bool HumanInputRequested { get; set; }

    /// <summary>
    /// Reason for requesting human input (if applicable).
    /// </summary>
    public string? HumanInputReason { get; set; }

    // Navigation properties
    public TaskEntity? Task { get; set; }
    public BacklogItemEntity? BacklogItem { get; set; }
}
