using Claude.CodeSdk;
using Claude.CodeSdk.Messages;

namespace Forge.Api.IntegrationTests.Features.Tasks;

[Collection("Api")]
public class StartAgentTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public StartAgentTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task StartAgent_WithNonExistentTask_ReturnsNotFound()
    {
        // Act
        var nonExistentId = Guid.NewGuid();
        var response = await _client.PostAsync($"/api/tasks/{nonExistentId}/start-agent", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartAgent_WithValidTask_StartsAgent()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "Agent Task", "Implement feature");

        // Setup mock to return empty stream
        var mockClient = Substitute.For<IClaudeAgentClient>();
        mockClient.QueryStreamAsync(Arg.Any<string>(), Arg.Any<ClaudeAgentOptions?>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerable.Empty<IMessage>());
        _factory.ClientFactoryMock.Create(Arg.Any<ClaudeAgentOptions?>()).Returns(mockClient);

        // Act
        var response = await _client.PostAsync($"/api/tasks/{task.Id}/start-agent", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var returnedTask = await response.ReadAsAsync<TaskDto>();
        returnedTask.Should().NotBeNull();
        returnedTask!.Id.Should().Be(task.Id);
    }

    [Fact]
    public async Task StartAgent_EmitsAgentStatusChangedEvent()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "SSE Agent Task");

        var mockClient = Substitute.For<IClaudeAgentClient>();
        mockClient.QueryStreamAsync(Arg.Any<string>(), Arg.Any<ClaudeAgentOptions?>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerable.Empty<IMessage>());
        _factory.ClientFactoryMock.Create(Arg.Any<ClaudeAgentOptions?>()).Returns(mockClient);

        // Act
        await _client.PostAsync($"/api/tasks/{task.Id}/start-agent", null);

        // Assert
        await _factory.SseServiceMock.Received().EmitAgentStatusChangedAsync(
            true,
            task.Id,
            Arg.Any<DateTime?>());
    }

    [Fact]
    public async Task StartAgent_AssignsAgentToTask()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "Assign Agent Task");

        var mockClient = Substitute.For<IClaudeAgentClient>();
        mockClient.QueryStreamAsync(Arg.Any<string>(), Arg.Any<ClaudeAgentOptions?>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerable.Empty<IMessage>());
        _factory.ClientFactoryMock.Create(Arg.Any<ClaudeAgentOptions?>()).Returns(mockClient);

        // Act
        await _client.PostAsync($"/api/tasks/{task.Id}/start-agent", null);

        // Allow some time for the assignment to occur
        await Task.Delay(100);

        // Assert - Check that task was updated
        await _factory.SseServiceMock.Received().EmitTaskUpdatedAsync(
            Arg.Is<TaskDto>(t => t.Id == task.Id && t.AssignedAgentId == "claude-agent"));
    }

    [Fact]
    public async Task StartAgent_WhenAgentAlreadyRunning_ReturnsBadRequest()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task1 = await TestDatabaseHelper.SeedTaskAsync(db, "First Task");
        var task2 = await TestDatabaseHelper.SeedTaskAsync(db, "Second Task");

        // Setup mock with a long-running task to simulate an agent that doesn't complete
        var mockClient = Substitute.For<IClaudeAgentClient>();
        var tcs = new TaskCompletionSource<bool>();
        mockClient.QueryStreamAsync(Arg.Any<string>(), Arg.Any<ClaudeAgentOptions?>(), Arg.Any<CancellationToken>())
            .Returns(LongRunningStream(tcs.Task));
        _factory.ClientFactoryMock.Create(Arg.Any<ClaudeAgentOptions?>()).Returns(mockClient);

        // Start first agent
        var response1 = await _client.PostAsync($"/api/tasks/{task1.Id}/start-agent", null);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Try to start second agent
        var response2 = await _client.PostAsync($"/api/tasks/{task2.Id}/start-agent", null);

        // Assert
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response2.Content.ReadAsStringAsync();
        content.Should().Contain("Agent is already running");

        // Cleanup
        tcs.SetResult(true);
    }

    [Fact]
    public async Task StartAgent_CreatesClientWithCorrectOptions()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "Options Test");

        var mockClient = Substitute.For<IClaudeAgentClient>();
        mockClient.QueryStreamAsync(Arg.Any<string>(), Arg.Any<ClaudeAgentOptions?>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerable.Empty<IMessage>());
        _factory.ClientFactoryMock.Create(Arg.Any<ClaudeAgentOptions?>()).Returns(mockClient);

        // Act
        await _client.PostAsync($"/api/tasks/{task.Id}/start-agent", null);

        // Assert
        _factory.ClientFactoryMock.Received().Create(Arg.Is<ClaudeAgentOptions?>(opt =>
            opt != null &&
            opt.DangerouslySkipPermissions == true &&
            opt.MaxTurns == 50));
    }

    private static async IAsyncEnumerable<IMessage> LongRunningStream(Task waitTask)
    {
        await waitTask;
        yield break;
    }
}
