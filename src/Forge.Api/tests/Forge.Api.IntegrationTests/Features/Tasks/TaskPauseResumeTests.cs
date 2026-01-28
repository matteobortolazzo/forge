namespace Forge.Api.IntegrationTests.Features.Tasks;

[Collection("Api")]
public class TaskPauseResumeTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private Guid _repositoryId;
    private Guid _backlogItemId;

    public TaskPauseResumeTests(ForgeWebApplicationFactory factory)
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

    #region Pause Tests

    [Fact]
    public async Task PauseTask_WithValidTask_SetsIsPausedTrue()
    {
        // Arrange
        var taskId = await CreateTaskAsync();

        // Act
        var response = await _client.PostAsJsonAsync($"{TasksUrl}/{taskId}/pause",
            new Forge.Api.Features.Tasks.PauseTaskDto("Manual pause"), HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.ReadAsAsync<TaskDto>();
        task!.IsPaused.Should().BeTrue();
        task.PauseReason.Should().Be("Manual pause");
        task.PausedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PauseTask_PersistsToDatabase()
    {
        // Arrange
        var taskId = await CreateTaskAsync();

        // Act
        await _client.PostAsJsonAsync($"{TasksUrl}/{taskId}/pause",
            new Forge.Api.Features.Tasks.PauseTaskDto("Persisted pause"), HttpClientExtensions.JsonOptions);

        // Assert
        await using var db = _factory.CreateDbContext();
        var entity = await db.Tasks.FindAsync(taskId);
        entity!.IsPaused.Should().BeTrue();
        entity.PauseReason.Should().Be("Persisted pause");
        entity.PausedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PauseTask_EmitsSseEvent()
    {
        // Arrange
        var taskId = await CreateTaskAsync();

        // Act
        await _client.PostAsJsonAsync($"{TasksUrl}/{taskId}/pause",
            new Forge.Api.Features.Tasks.PauseTaskDto("SSE pause"), HttpClientExtensions.JsonOptions);

        // Assert
        await _factory.SseServiceMock.Received(1).EmitTaskPausedAsync(
            Arg.Is<TaskDto>(t => t.IsPaused && t.PauseReason == "SSE pause"));
    }

    [Fact]
    public async Task PauseTask_WithNonExistentTask_ReturnsNotFound()
    {
        // Act
        var response = await _client.PostAsJsonAsync($"{TasksUrl}/{Guid.NewGuid()}/pause",
            new Forge.Api.Features.Tasks.PauseTaskDto("Test"), HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Resume Tests

    [Fact]
    public async Task ResumeTask_ClearsIsPaused()
    {
        // Arrange
        var taskId = await CreateAndPauseTaskAsync();

        // Act
        var response = await _client.PostAsync($"{TasksUrl}/{taskId}/resume", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.ReadAsAsync<TaskDto>();
        task!.IsPaused.Should().BeFalse();
        task.PauseReason.Should().BeNull();
        task.PausedAt.Should().BeNull();
    }

    [Fact]
    public async Task ResumeTask_ClearsErrorState()
    {
        // Arrange
        var taskId = await CreateAndPauseTaskAsync();

        // Set error state directly in database
        await using (var db = _factory.CreateDbContext())
        {
            var entity = await db.Tasks.FindAsync(taskId);
            entity!.HasError = true;
            entity.ErrorMessage = "Test error";
            entity.RetryCount = 2;
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.PostAsync($"{TasksUrl}/{taskId}/resume", null);

        // Assert
        var task = await response.ReadAsAsync<TaskDto>();
        task!.HasError.Should().BeFalse();
        task.ErrorMessage.Should().BeNull();
        task.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task ResumeTask_PersistsToDatabase()
    {
        // Arrange
        var taskId = await CreateAndPauseTaskAsync();

        // Act
        await _client.PostAsync($"{TasksUrl}/{taskId}/resume", null);

        // Assert
        await using var db = _factory.CreateDbContext();
        var entity = await db.Tasks.FindAsync(taskId);
        entity!.IsPaused.Should().BeFalse();
        entity.PauseReason.Should().BeNull();
        entity.PausedAt.Should().BeNull();
    }

    [Fact]
    public async Task ResumeTask_EmitsSseEvent()
    {
        // Arrange
        var taskId = await CreateAndPauseTaskAsync();

        // Act
        await _client.PostAsync($"{TasksUrl}/{taskId}/resume", null);

        // Assert
        await _factory.SseServiceMock.Received(1).EmitTaskResumedAsync(
            Arg.Is<TaskDto>(t => !t.IsPaused));
    }

    [Fact]
    public async Task ResumeTask_WithNonExistentTask_ReturnsNotFound()
    {
        // Act
        var response = await _client.PostAsync($"{TasksUrl}/{Guid.NewGuid()}/resume", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Helpers

    private async Task<Guid> CreateTaskAsync()
    {
        await using var db = _factory.CreateDbContext();
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "Test Task", repositoryId: _repositoryId, backlogItemId: _backlogItemId);
        return task.Id;
    }

    private async Task<Guid> CreateAndPauseTaskAsync()
    {
        var taskId = await CreateTaskAsync();
        await _client.PostAsJsonAsync($"{TasksUrl}/{taskId}/pause",
            new Forge.Api.Features.Tasks.PauseTaskDto("Initial pause"), HttpClientExtensions.JsonOptions);
        _factory.SseServiceMock.ClearReceivedCalls(); // Clear for subsequent assertions
        return taskId;
    }

    #endregion
}
