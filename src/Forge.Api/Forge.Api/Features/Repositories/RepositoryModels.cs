using Forge.Api.Data.Entities;

namespace Forge.Api.Features.Repositories;

public record RepositoryDto(
    Guid Id,
    string Name,
    string Path,
    bool IsDefault,
    bool IsActive,
    string? Branch,
    string? CommitHash,
    string? RemoteUrl,
    bool? IsDirty,
    bool IsGitRepository,
    DateTime? LastRefreshedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int TaskCount)
{
    public static RepositoryDto FromEntity(RepositoryEntity entity, int? taskCount = null) => new(
        entity.Id,
        entity.Name,
        entity.Path,
        entity.IsDefault,
        entity.IsActive,
        entity.Branch,
        entity.CommitHash,
        entity.RemoteUrl,
        entity.IsDirty,
        entity.IsGitRepository,
        entity.LastRefreshedAt,
        entity.CreatedAt,
        entity.UpdatedAt,
        taskCount ?? entity.Tasks.Count);
}

public record CreateRepositoryDto(
    string Name,
    string Path,
    bool SetAsDefault = false);

public record UpdateRepositoryDto(
    string? Name = null);
