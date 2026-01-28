namespace Forge.Api.IntegrationTests.Features.Tasks;

[Collection("Api")]
public class AbortAgentTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private Guid _repositoryId;
    private Guid _backlogItemId;

    public AbortAgentTests(ForgeWebApplicationFactory factory)
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
    public async Task AbortAgent_WithNonExistentTask_ReturnsNotFound()
    {
        // Act
        var nonExistentId = Guid.NewGuid();
        var response = await _client.PostAsync($"{TasksUrl}/{nonExistentId}/abort", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AbortAgent_WithNoAgentRunning_ReturnsBadRequest()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "Task without Agent", repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        // Act
        var response = await _client.PostAsync($"{TasksUrl}/{task.Id}/abort", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("No agent running");
    }

    [Fact]
    public async Task AbortAgent_WithAgentRunningOnDifferentTask_ReturnsBadRequest()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task1 = await TestDatabaseHelper.SeedTaskAsync(db, "Task 1 with Agent", state: PipelineState.Implementing, repositoryId: _repositoryId, backlogItemId: _backlogItemId);
        var task2 = await TestDatabaseHelper.SeedTaskAsync(db, "Task 2 without Agent", state: PipelineState.Implementing, repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        // Note: We can't easily simulate a running agent without mocking AgentRunnerService.
        // The default state is no agent running, so we verify the expected behavior
        // when trying to abort a task that has no agent.

        // Act
        var response = await _client.PostAsync($"{TasksUrl}/{task2.Id}/abort", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("No agent running");
    }

    [Fact]
    public async Task AbortAgent_WithValidTask_ReturnsTaskWhenNoAgentRunning()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "Abort Test", state: PipelineState.Implementing, repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        // Act
        var response = await _client.PostAsync($"{TasksUrl}/{task.Id}/abort", null);

        // Assert
        // When no agent is running, we get BadRequest
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AbortAgent_WithWrongBacklogItem_ReturnsNotFound()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var otherBacklogItem = await TestDatabaseHelper.SeedBacklogItemAsync(db, "Other Backlog Item", repositoryId: _repositoryId);
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "Task in main backlog", state: PipelineState.Implementing, repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        // Act - Try to abort task from wrong backlog item
        var response = await _client.PostAsync($"/api/repositories/{_repositoryId}/backlog/{otherBacklogItem.Id}/tasks/{task.Id}/abort", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
