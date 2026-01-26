namespace Forge.Api.IntegrationTests.Infrastructure;

public static class TestDatabaseHelper
{
    public static async Task<TaskEntity> SeedTaskAsync(
        ForgeDbContext db,
        string title = "Test Task",
        string description = "Test Description",
        PipelineState state = PipelineState.Backlog,
        Priority priority = Priority.Medium)
    {
        var entity = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            State = state,
            Priority = priority,
            HasError = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Tasks.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public static async Task<TaskLogEntity> SeedTaskLogAsync(
        ForgeDbContext db,
        Guid taskId,
        LogType type = LogType.Info,
        string content = "Test log content",
        string? toolName = null)
    {
        var entity = new TaskLogEntity
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            Type = type,
            Content = content,
            ToolName = toolName,
            Timestamp = DateTime.UtcNow
        };

        db.TaskLogs.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }
}
