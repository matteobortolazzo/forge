using Forge.Api.Features.Scheduler;

namespace Forge.Api.IntegrationTests.Features.Scheduler;

[Collection("Api")]
public class SchedulerEndpointTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private Guid _repositoryId;

    public SchedulerEndpointTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        // Create a repository for all tests via HTTP
        var repoDto = new CreateRepositoryDtoBuilder()
            .WithName("Test Repository")
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var response = await _client.PostAsJsonAsync("/api/repositories", repoDto, HttpClientExtensions.JsonOptions);
        var repo = await response.ReadAsAsync<RepositoryDto>();
        _repositoryId = repo!.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

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
        await CreateTaskInStateAsync(PipelineState.Planning);
        await CreateTaskInStateAsync(PipelineState.Implementing);
        await CreateTaskInStateAsync(PipelineState.Backlog); // Not schedulable

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
        var taskId = await CreateTaskInStateAsync(PipelineState.Planning);
        await _client.PostAsJsonAsync($"/api/repositories/{_repositoryId}/tasks/{taskId}/pause",
            new PauseTaskDto("Test pause"), HttpClientExtensions.JsonOptions);

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

    private async Task<Guid> CreateTaskInStateAsync(PipelineState targetState)
    {
        // Create in backlog
        var createDto = new CreateTaskDtoBuilder().Build();
        var createResponse = await _client.PostAsJsonAsync($"/api/repositories/{_repositoryId}/tasks", createDto, HttpClientExtensions.JsonOptions);
        var task = await createResponse.ReadAsAsync<TaskDto>();
        var taskId = task!.Id;

        // Transition to target state (if not backlog)
        var states = new[] { PipelineState.Backlog, PipelineState.Split, PipelineState.Research,
                           PipelineState.Planning, PipelineState.Implementing, PipelineState.Simplifying,
                           PipelineState.Verifying, PipelineState.Reviewing, PipelineState.PrReady, PipelineState.Done };
        var currentIndex = 0;
        var targetIndex = Array.IndexOf(states, targetState);

        while (currentIndex < targetIndex)
        {
            currentIndex++;
            await _client.PostAsJsonAsync($"/api/repositories/{_repositoryId}/tasks/{taskId}/transition",
                new TransitionTaskDto(states[currentIndex]), HttpClientExtensions.JsonOptions);
        }

        return taskId;
    }

    private record EnabledResult(bool Enabled);
}
