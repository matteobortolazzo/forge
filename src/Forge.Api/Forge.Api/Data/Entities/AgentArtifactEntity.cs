using Forge.Api.Shared;

namespace Forge.Api.Data.Entities;

/// <summary>
/// Stores structured output (artifacts) from each agent execution.
/// Artifacts are passed between pipeline stages to provide context.
/// </summary>
public class AgentArtifactEntity
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }

    /// <summary>
    /// The pipeline state in which this artifact was produced.
    /// </summary>
    public PipelineState ProducedInState { get; set; }

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

    // Navigation property
    public TaskEntity? Task { get; set; }
}
