namespace Forge.Api.IntegrationTests.Features.Tasks;

[Collection("Api")]
public class GetTaskTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private Guid _repositoryId;

    public GetTaskTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        // Create a repository for all tests
        await using var db = _factory.CreateDbContext();
        var repo = await TestDatabaseHelper.SeedRepositoryAsync(db);
        _repositoryId = repo.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetTask_WithValidId_ReturnsTask()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Test Task", "Test Description", PipelineState.Planning, Priority.High, repositoryId: _repositoryId);

        // Act
        var response = await _client.GetAsync($"/api/repositories/{_repositoryId}/tasks/{entity.Id}");

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
        var response = await _client.GetAsync($"/api/repositories/{_repositoryId}/tasks/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTask_WithInvalidGuid_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/repositories/{_repositoryId}/tasks/not-a-guid");

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
            RepositoryId = _repositoryId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Tasks.Add(entity);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/repositories/{_repositoryId}/tasks/{entity.Id}");

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
            RepositoryId = _repositoryId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Tasks.Add(entity);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/repositories/{_repositoryId}/tasks/{entity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.ReadAsAsync<TaskDto>();
        task!.AssignedAgentId.Should().Be("claude-agent-123");
    }

    [Fact]
    public async Task GetTask_WithNonExistentRepository_ReturnsNotFound()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Test Task", repositoryId: _repositoryId);
        var nonExistentRepoId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/repositories/{nonExistentRepoId}/tasks/{entity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTask_WithWrongRepository_ReturnsNotFound()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var otherRepoPath = Path.Combine(Path.GetTempPath(), $"other-repo-{Guid.NewGuid()}");
        var otherRepo = await TestDatabaseHelper.SeedRepositoryAsync(db, "Other Repo", path: otherRepoPath, isDefault: false);
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Task in main repo", repositoryId: _repositoryId);

        // Act - Try to get task from wrong repository
        var response = await _client.GetAsync($"/api/repositories/{otherRepo.Id}/tasks/{entity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
