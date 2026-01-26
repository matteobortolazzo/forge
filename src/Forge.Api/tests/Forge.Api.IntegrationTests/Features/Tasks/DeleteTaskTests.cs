namespace Forge.Api.IntegrationTests.Features.Tasks;

[Collection("Api")]
public class DeleteTaskTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DeleteTaskTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DeleteTask_WithValidId_ReturnsNoContent()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Task to Delete");

        // Act
        var response = await _client.DeleteAsync($"/api/tasks/{entity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTask_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var nonExistentId = Guid.NewGuid();
        var response = await _client.DeleteAsync($"/api/tasks/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTask_RemovesFromDatabase()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Task to Remove");

        // Act
        await _client.DeleteAsync($"/api/tasks/{entity.Id}");

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
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "SSE Delete Task");

        // Act
        await _client.DeleteAsync($"/api/tasks/{entity.Id}");

        // Assert
        await _factory.SseServiceMock.Received(1).EmitTaskDeletedAsync(entity.Id);
    }

    [Fact]
    public async Task DeleteTask_CascadeDeletesLogs()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Task with Logs");
        await TestDatabaseHelper.SeedTaskLogAsync(db, entity.Id, LogType.Info, "Log 1");
        await TestDatabaseHelper.SeedTaskLogAsync(db, entity.Id, LogType.ToolUse, "Log 2", "ReadFile");
        await TestDatabaseHelper.SeedTaskLogAsync(db, entity.Id, LogType.Error, "Log 3");

        // Act
        await _client.DeleteAsync($"/api/tasks/{entity.Id}");

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
        var taskToDelete = await TestDatabaseHelper.SeedTaskAsync(db, "Task to Delete");
        var taskToKeep = await TestDatabaseHelper.SeedTaskAsync(db, "Task to Keep");

        // Act
        await _client.DeleteAsync($"/api/tasks/{taskToDelete.Id}");

        // Assert
        await using var verifyDb = _factory.CreateDbContext();
        var remaining = await verifyDb.Tasks.FindAsync(taskToKeep.Id);
        remaining.Should().NotBeNull();
        remaining!.Title.Should().Be("Task to Keep");
    }
}
