using Forge.Api.Shared;

namespace Forge.Api.Data.Entities;

public class TaskLogEntity
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public LogType Type { get; set; }
    public required string Content { get; set; }
    public string? ToolName { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation property
    public TaskEntity? Task { get; set; }
}
