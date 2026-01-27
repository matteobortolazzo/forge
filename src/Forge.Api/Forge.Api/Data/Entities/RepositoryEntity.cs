namespace Forge.Api.Data.Entities;

public class RepositoryEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Path { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Cached git info
    public string? Branch { get; set; }
    public string? CommitHash { get; set; }
    public string? RemoteUrl { get; set; }
    public bool? IsDirty { get; set; }
    public bool IsGitRepository { get; set; }
    public DateTime? LastRefreshedAt { get; set; }

    // Navigation
    public ICollection<TaskEntity> Tasks { get; set; } = [];
}
