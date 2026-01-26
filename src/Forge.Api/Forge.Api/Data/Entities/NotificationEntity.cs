using Forge.Api.Shared;

namespace Forge.Api.Data.Entities;

public class NotificationEntity
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public NotificationType Type { get; set; }
    public Guid? TaskId { get; set; }
    public bool Read { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public TaskEntity? Task { get; set; }
}
