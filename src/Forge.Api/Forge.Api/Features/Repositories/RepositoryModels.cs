using Forge.Api.Data.Entities;

namespace Forge.Api.Features.Repositories;

public record RepositoryDto(
    Guid Id,
    string Name,
    string Path,
    bool IsActive,
    bool IsDefault,
    string? Branch,
    string? CommitHash,
    string? RemoteUrl,
    bool? IsDirty,
    bool IsGitRepository,
    DateTime? LastRefreshedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int BacklogItemCount)
{
    public static RepositoryDto FromEntity(RepositoryEntity entity, int? backlogItemCount = null) => new(
        entity.Id,
        entity.Name,
        entity.Path,
        entity.IsActive,
        entity.IsDefault,
        entity.Branch,
        entity.CommitHash,
        entity.RemoteUrl,
        entity.IsDirty,
        entity.IsGitRepository,
        entity.LastRefreshedAt,
        entity.CreatedAt,
        entity.UpdatedAt,
        backlogItemCount ?? entity.BacklogItems.Count);
}

public record CreateRepositoryDto(
    string Name,
    string Path,
    bool IsDefault = false);

public record UpdateRepositoryDto(
    string? Name = null);
