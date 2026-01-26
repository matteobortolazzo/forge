namespace Forge.Api.Features.Repository;

public record RepositoryInfoDto(
    string Name,
    string Path,
    string? Branch,
    string? CommitHash,
    string? RemoteUrl,
    bool IsDirty,
    bool IsGitRepository);
