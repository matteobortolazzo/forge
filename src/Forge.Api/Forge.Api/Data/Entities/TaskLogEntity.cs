using Forge.Api.Shared;

namespace Forge.Api.Data.Entities;

/// <summary>
/// Stores log entries from agent execution.
/// Can belong to either a BacklogItem (for refining/splitting) or a Task (for implementation).
/// </summary>
public class TaskLogEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// The task this log belongs to (null for backlog item logs).
    /// </summary>
    public Guid? TaskId { get; set; }

    /// <summary>
    /// The backlog item this log belongs to (null for task logs).
    /// </summary>
    public Guid? BacklogItemId { get; set; }

    public LogType Type { get; set; }
    public required string Content { get; set; }
    public string? ToolName { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public TaskEntity? Task { get; set; }
    public BacklogItemEntity? BacklogItem { get; set; }
}
