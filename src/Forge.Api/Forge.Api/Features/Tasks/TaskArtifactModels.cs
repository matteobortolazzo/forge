using Forge.Api.Data.Entities;
using Forge.Api.Shared;

namespace Forge.Api.Features.Tasks;

/// <summary>
/// DTO for agent artifact.
/// </summary>
public record ArtifactDto(
    Guid Id,
    Guid TaskId,
    Guid? SubtaskId,
    PipelineState ProducedInState,
    ArtifactType ArtifactType,
    string Content,
    DateTime CreatedAt,
    string? AgentId,
    decimal? ConfidenceScore,
    bool HumanInputRequested,
    string? HumanInputReason)
{
    public static ArtifactDto FromEntity(AgentArtifactEntity entity) =>
        new(
            entity.Id,
            entity.TaskId,
            entity.SubtaskId,
            entity.ProducedInState,
            entity.ArtifactType,
            entity.Content,
            entity.CreatedAt,
            entity.AgentId,
            entity.ConfidenceScore,
            entity.HumanInputRequested,
            entity.HumanInputReason);
}
