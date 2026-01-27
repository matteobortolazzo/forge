namespace Forge.Api.IntegrationTests.Infrastructure;

public static class TestDatabaseHelper
{
    /// <summary>
    /// Creates a repository in the database. Required before creating tasks.
    /// </summary>
    public static async Task<RepositoryEntity> SeedRepositoryAsync(
        ForgeDbContext db,
        string name = "Test Repository",
        string? path = null)
    {
        var entity = new RepositoryEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Path = path ?? ForgeWebApplicationFactory.ProjectRoot,
            IsActive = true,
            IsGitRepository = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Repositories.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public static async Task<TaskEntity> SeedTaskAsync(
        ForgeDbContext db,
        string title = "Test Task",
        string description = "Test Description",
        PipelineState state = PipelineState.Backlog,
        Priority priority = Priority.Medium,
        Guid? repositoryId = null)
    {
        // If no repository provided, create or get an existing one
        if (repositoryId is null)
        {
            var existingRepo = await db.Repositories
                .FirstOrDefaultAsync(r => r.IsActive);

            if (existingRepo is null)
            {
                existingRepo = await SeedRepositoryAsync(db);
            }

            repositoryId = existingRepo.Id;
        }

        var entity = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            State = state,
            Priority = priority,
            HasError = false,
            RepositoryId = repositoryId.Value,
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
