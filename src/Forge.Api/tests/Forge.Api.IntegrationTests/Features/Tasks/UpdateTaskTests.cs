namespace Forge.Api.IntegrationTests.Features.Tasks;

[Collection("Api")]
public class UpdateTaskTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UpdateTaskTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UpdateTask_WithFullUpdate_UpdatesAllFields()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Original Title", "Original Description", priority: Priority.Low);

        var dto = new UpdateTaskDtoBuilder()
            .WithTitle("Updated Title")
            .WithDescription("Updated Description")
            .WithPriority(Priority.Critical)
            .Build();

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/tasks/{entity.Id}", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.ReadAsAsync<TaskDto>();
        task!.Title.Should().Be("Updated Title");
        task.Description.Should().Be("Updated Description");
        task.Priority.Should().Be(Priority.Critical);
    }

    [Fact]
    public async Task UpdateTask_WithPartialUpdate_UpdatesOnlyProvidedFields()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Original Title", "Original Description", priority: Priority.Medium);

        var dto = new UpdateTaskDtoBuilder()
            .WithTitle("Updated Title Only")
            .Build();

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/tasks/{entity.Id}", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.ReadAsAsync<TaskDto>();
        task!.Title.Should().Be("Updated Title Only");
        task.Description.Should().Be("Original Description");
        task.Priority.Should().Be(Priority.Medium);
    }

    [Fact]
    public async Task UpdateTask_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var dto = new UpdateTaskDtoBuilder()
            .WithTitle("Updated Title")
            .Build();

        // Act
        var nonExistentId = Guid.NewGuid();
        var response = await _client.PatchAsJsonAsync($"/api/tasks/{nonExistentId}", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTask_EmitsSseEvent()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "SSE Test Task");

        var dto = new UpdateTaskDtoBuilder()
            .WithTitle("SSE Updated")
            .Build();

        // Act
        await _client.PatchAsJsonAsync($"/api/tasks/{entity.Id}", dto);

        // Assert
        await _factory.SseServiceMock.Received(1).EmitTaskUpdatedAsync(
            Arg.Is<TaskDto>(t => t.Title == "SSE Updated"));
    }

    [Fact]
    public async Task UpdateTask_UpdatesTimestamp()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Timestamp Test");
        var originalUpdatedAt = entity.UpdatedAt;
        await Task.Delay(10);

        var dto = new UpdateTaskDtoBuilder()
            .WithTitle("Updated Timestamp")
            .Build();

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/tasks/{entity.Id}", dto);

        // Assert
        var task = await response.ReadAsAsync<TaskDto>();
        task!.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task UpdateTask_PersistsChanges()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Persistence Test");

        var dto = new UpdateTaskDtoBuilder()
            .WithTitle("Persisted Update")
            .Build();

        // Act
        await _client.PatchAsJsonAsync($"/api/tasks/{entity.Id}", dto);

        // Assert - Verify persistence with fresh context
        await using var verifyDb = _factory.CreateDbContext();
        var updated = await verifyDb.Tasks.FindAsync(entity.Id);
        updated!.Title.Should().Be("Persisted Update");
    }

    [Fact]
    public async Task UpdateTask_PreservesStateAndAgent()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = "Task With State",
            Description = "Description",
            State = PipelineState.Implementing,
            Priority = Priority.High,
            AssignedAgentId = "claude-agent",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Tasks.Add(entity);
        await db.SaveChangesAsync();

        var dto = new UpdateTaskDtoBuilder()
            .WithTitle("Updated But State Preserved")
            .Build();

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/tasks/{entity.Id}", dto);

        // Assert
        var task = await response.ReadAsAsync<TaskDto>();
        task!.State.Should().Be(PipelineState.Implementing);
        task.AssignedAgentId.Should().Be("claude-agent");
    }
}
