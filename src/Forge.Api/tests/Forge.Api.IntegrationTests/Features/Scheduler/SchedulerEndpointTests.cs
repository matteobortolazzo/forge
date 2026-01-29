using Forge.Api.Features.Scheduler;

namespace Forge.Api.IntegrationTests.Features.Scheduler;

[Collection("Api")]
public class SchedulerEndpointTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private Guid _repositoryId;
    private Guid _backlogItemId;

    public SchedulerEndpointTests(ForgeWebApplicationFactory factory)
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
    public async Task GetStatus_ReturnsSchedulerStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/scheduler/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await response.ReadAsAsync<SchedulerStatusDto>();
        status.Should().NotBeNull();
        status!.IsEnabled.Should().BeTrue();
        status.IsAgentRunning.Should().BeFalse();
    }

    [Fact]
    public async Task GetStatus_ReturnsPendingTaskCount()
    {
        // Arrange - Create tasks in different states
        await using var db = _factory.CreateDbContext();
        await TestDatabaseHelper.SeedTaskAsync(db, "Planning Task", state: PipelineState.Planning, repositoryId: _repositoryId, backlogItemId: _backlogItemId);
        await TestDatabaseHelper.SeedTaskAsync(db, "Implementing Task", state: PipelineState.Implementing, repositoryId: _repositoryId, backlogItemId: _backlogItemId);
        await TestDatabaseHelper.SeedTaskAsync(db, "PrReady Task", state: PipelineState.PrReady, repositoryId: _repositoryId, backlogItemId: _backlogItemId); // Not schedulable

        // Act
        var response = await _client.GetAsync("/api/scheduler/status");

        // Assert
        var status = await response.ReadAsAsync<SchedulerStatusDto>();
        status!.PendingTaskCount.Should().Be(2);
    }

    [Fact]
    public async Task GetStatus_ReturnsPausedTaskCount()
    {
        // Arrange - Create a task and pause it
        await using var db = _factory.CreateDbContext();
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "Paused Task", state: PipelineState.Planning, repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        await _client.PostAsJsonAsync($"{TasksUrl}/{task.Id}/pause",
            new Forge.Api.Features.Tasks.PauseTaskDto("Test pause"), HttpClientExtensions.JsonOptions);

        // Act
        var response = await _client.GetAsync("/api/scheduler/status");

        // Assert
        var status = await response.ReadAsAsync<SchedulerStatusDto>();
        status!.PausedTaskCount.Should().Be(1);
    }

    [Fact]
    public async Task EnableScheduler_ReturnsEnabledTrue()
    {
        // Act
        var response = await _client.PostAsync("/api/scheduler/enable", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadAsAsync<EnabledResult>();
        result!.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task DisableScheduler_ReturnsEnabledFalse()
    {
        // Act
        var response = await _client.PostAsync("/api/scheduler/disable", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadAsAsync<EnabledResult>();
        result!.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task DisableAndEnable_SchedulerStatus_ReflectsCurrentState()
    {
        // Act - Disable
        await _client.PostAsync("/api/scheduler/disable", null);
        var disabledStatus = await (await _client.GetAsync("/api/scheduler/status")).ReadAsAsync<SchedulerStatusDto>();

        // Assert
        disabledStatus!.IsEnabled.Should().BeFalse();

        // Act - Enable
        await _client.PostAsync("/api/scheduler/enable", null);
        var enabledStatus = await (await _client.GetAsync("/api/scheduler/status")).ReadAsAsync<SchedulerStatusDto>();

        // Assert
        enabledStatus!.IsEnabled.Should().BeTrue();
    }

    private record EnabledResult(bool Enabled);
}
