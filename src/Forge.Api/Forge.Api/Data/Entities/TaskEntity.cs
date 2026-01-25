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

    // Navigation property
    public ICollection<TaskLogEntity> Logs { get; set; } = [];
}
