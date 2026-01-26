namespace Forge.Api.IntegrationTests.Features.Tasks;

[Collection("Api")]
public class GetTaskTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GetTaskTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetTask_WithValidId_ReturnsTask()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Test Task", "Test Description", PipelineState.Planning, Priority.High);

        // Act
        var response = await _client.GetAsync($"/api/tasks/{entity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.ReadAsAsync<TaskDto>();
        task.Should().NotBeNull();
        task!.Id.Should().Be(entity.Id);
        task.Title.Should().Be("Test Task");
        task.Description.Should().Be("Test Description");
        task.State.Should().Be(PipelineState.Planning);
        task.Priority.Should().Be(Priority.High);
        task.HasError.Should().BeFalse();
        task.AssignedAgentId.Should().BeNull();
    }

    [Fact]
    public async Task GetTask_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var nonExistentId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/tasks/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTask_WithInvalidGuid_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/tasks/not-a-guid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTask_WithTaskHavingError_ReturnsErrorDetails()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = "Error Task",
            Description = "Task with error",
            State = PipelineState.Implementing,
            Priority = Priority.Critical,
            HasError = true,
            ErrorMessage = "Agent crashed",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Tasks.Add(entity);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/tasks/{entity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.ReadAsAsync<TaskDto>();
        task!.HasError.Should().BeTrue();
        task.ErrorMessage.Should().Be("Agent crashed");
    }

    [Fact]
    public async Task GetTask_WithAssignedAgent_ReturnsAgentId()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = "Assigned Task",
            Description = "Task with agent",
            State = PipelineState.Implementing,
            Priority = Priority.Medium,
            AssignedAgentId = "claude-agent-123",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Tasks.Add(entity);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/tasks/{entity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.ReadAsAsync<TaskDto>();
        task!.AssignedAgentId.Should().Be("claude-agent-123");
    }
}
