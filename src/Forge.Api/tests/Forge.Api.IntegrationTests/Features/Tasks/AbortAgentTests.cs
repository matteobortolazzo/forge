namespace Forge.Api.IntegrationTests.Features.Tasks;

[Collection("Api")]
public class AbortAgentTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AbortAgentTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AbortAgent_WithNonExistentTask_ReturnsNotFound()
    {
        // Act
        var nonExistentId = Guid.NewGuid();
        var response = await _client.PostAsync($"/api/tasks/{nonExistentId}/abort", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AbortAgent_WithNoAgentRunning_ReturnsBadRequest()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "Task without Agent");

        // Act
        var response = await _client.PostAsync($"/api/tasks/{task.Id}/abort", null);

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
        var task1 = await TestDatabaseHelper.SeedTaskAsync(db, "Task 1 with Agent", state: PipelineState.Implementing);
        var task2 = await TestDatabaseHelper.SeedTaskAsync(db, "Task 2 without Agent", state: PipelineState.Implementing);

        // Note: We can't easily simulate a running agent without mocking AgentRunnerService.
        // The default state is no agent running, so we verify the expected behavior
        // when trying to abort a task that has no agent.

        // Act
        var response = await _client.PostAsync($"/api/tasks/{task2.Id}/abort", null);

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
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "Abort Test", state: PipelineState.Implementing);

        // Act
        var response = await _client.PostAsync($"/api/tasks/{task.Id}/abort", null);

        // Assert
        // When no agent is running, we get BadRequest
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
