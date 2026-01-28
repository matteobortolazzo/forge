namespace Forge.Api.IntegrationTests.Features.Tasks;

[Collection("Api")]
public class DeleteTaskTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private Guid _repositoryId;
    private Guid _backlogItemId;

    public DeleteTaskTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        // Create a repository and backlog item for all tests
        await using var db = _factory.CreateDbContext();
        var repo = await TestDatabaseHelper.SeedRepositoryAsync(db);
        _repositoryId = repo.Id;
        var backlogItem = await TestDatabaseHelper.SeedBacklogItemAsync(db, repositoryId: _repositoryId);
        _backlogItemId = backlogItem.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private string TasksUrl => $"/api/repositories/{_repositoryId}/backlog/{_backlogItemId}/tasks";

    [Fact]
    public async Task DeleteTask_WithValidId_ReturnsNoContent()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Task to Delete", repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        // Act
        var response = await _client.DeleteAsync($"{TasksUrl}/{entity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTask_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var nonExistentId = Guid.NewGuid();
        var response = await _client.DeleteAsync($"{TasksUrl}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTask_RemovesFromDatabase()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Task to Remove", repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        // Act
        await _client.DeleteAsync($"{TasksUrl}/{entity.Id}");

        // Assert
        await using var verifyDb = _factory.CreateDbContext();
        var deleted = await verifyDb.Tasks.FindAsync(entity.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTask_EmitsSseEvent()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "SSE Delete Task", repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        // Act
        await _client.DeleteAsync($"{TasksUrl}/{entity.Id}");

        // Assert
        await _factory.SseServiceMock.Received(1).EmitTaskDeletedAsync(entity.Id);
    }

    [Fact]
    public async Task DeleteTask_CascadeDeletesLogs()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Task with Logs", repositoryId: _repositoryId, backlogItemId: _backlogItemId);
        await TestDatabaseHelper.SeedTaskLogAsync(db, entity.Id, LogType.Info, "Log 1");
        await TestDatabaseHelper.SeedTaskLogAsync(db, entity.Id, LogType.ToolUse, "Log 2", "ReadFile");
        await TestDatabaseHelper.SeedTaskLogAsync(db, entity.Id, LogType.Error, "Log 3");

        // Act
        await _client.DeleteAsync($"{TasksUrl}/{entity.Id}");

        // Assert
        await using var verifyDb = _factory.CreateDbContext();
        var orphanLogs = await verifyDb.TaskLogs.Where(l => l.TaskId == entity.Id).ToListAsync();
        orphanLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteTask_DoesNotAffectOtherTasks()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var taskToDelete = await TestDatabaseHelper.SeedTaskAsync(db, "Task to Delete", repositoryId: _repositoryId, backlogItemId: _backlogItemId);
        var taskToKeep = await TestDatabaseHelper.SeedTaskAsync(db, "Task to Keep", repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        // Act
        await _client.DeleteAsync($"{TasksUrl}/{taskToDelete.Id}");

        // Assert
        await using var verifyDb = _factory.CreateDbContext();
        var remaining = await verifyDb.Tasks.FindAsync(taskToKeep.Id);
        remaining.Should().NotBeNull();
        remaining!.Title.Should().Be("Task to Keep");
    }

    [Fact]
    public async Task DeleteTask_WithWrongBacklogItem_ReturnsNotFound()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var otherBacklogItem = await TestDatabaseHelper.SeedBacklogItemAsync(db, "Other Backlog Item", repositoryId: _repositoryId);
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Task in main backlog", repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        // Act - Try to delete task from wrong backlog item
        var response = await _client.DeleteAsync($"/api/repositories/{_repositoryId}/backlog/{otherBacklogItem.Id}/tasks/{entity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
