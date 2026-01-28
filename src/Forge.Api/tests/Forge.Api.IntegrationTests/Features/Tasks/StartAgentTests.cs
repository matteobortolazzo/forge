using Forge.Api.Features.Agent;

namespace Forge.Api.IntegrationTests.Features.Tasks;

[Collection("Api")]
public class StartAgentTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private Guid _repositoryId;
    private Guid _backlogItemId;

    public StartAgentTests(ForgeWebApplicationFactory factory)
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
    public async Task StartAgent_WithNonExistentTask_ReturnsNotFound()
    {
        // Act
        var nonExistentId = Guid.NewGuid();
        var response = await _client.PostAsync($"{TasksUrl}/{nonExistentId}/start-agent", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartAgent_WithValidTask_StartsAgent()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "Agent Task", "Implement feature", repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        // Act
        var response = await _client.PostAsync($"{TasksUrl}/{task.Id}/start-agent", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var returnedTask = await response.ReadAsAsync<TaskDto>();
        returnedTask.Should().NotBeNull();
        returnedTask!.Id.Should().Be(task.Id);
    }

    [Fact]
    public async Task StartAgent_CallsAgentRunnerService()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "SSE Agent Task", "Description", repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        // Act
        await _client.PostAsync($"{TasksUrl}/{task.Id}/start-agent", null);

        // Assert
        await _factory.AgentRunnerServiceMock.Received(1).StartAgentAsync(
            task.Id,
            task.Title,
            task.Description);
    }

    [Fact]
    public async Task StartAgent_WhenAgentAlreadyRunning_ReturnsBadRequest()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "First Task", repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        // Setup mock to indicate agent already running
        _factory.AgentRunnerServiceMock.StartAgentAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        // Act
        var response = await _client.PostAsync($"{TasksUrl}/{task.Id}/start-agent", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Agent is already running");
    }

    [Fact]
    public async Task StartAgent_ReturnsUpdatedTask()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "Options Test", repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        // Act
        var response = await _client.PostAsync($"{TasksUrl}/{task.Id}/start-agent", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var returnedTask = await response.ReadAsAsync<TaskDto>();
        returnedTask.Should().NotBeNull();
        returnedTask!.Id.Should().Be(task.Id);
        returnedTask.Title.Should().Be("Options Test");
    }

    [Fact]
    public async Task StartAgent_WithWrongBacklogItem_ReturnsNotFound()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var otherBacklogItem = await TestDatabaseHelper.SeedBacklogItemAsync(db, "Other Backlog Item", repositoryId: _repositoryId);
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "Task in main backlog", repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        // Act - Try to start agent from wrong backlog item
        var response = await _client.PostAsync($"/api/repositories/{_repositoryId}/backlog/{otherBacklogItem.Id}/tasks/{task.Id}/start-agent", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
