namespace Forge.Api.IntegrationTests.Features.Tasks;

[Collection("Api")]
public class TransitionTaskTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private Guid _repositoryId;
    private Guid _backlogItemId;

    public TransitionTaskTests(ForgeWebApplicationFactory factory)
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
    public async Task TransitionTask_FromPlanningToImplementing_Succeeds()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Transition Test", state: PipelineState.Planning, repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        var dto = new TransitionTaskDtoBuilder()
            .WithTargetState(PipelineState.Implementing)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync($"{TasksUrl}/{entity.Id}/transition", dto, HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.ReadAsAsync<TaskDto>();
        task!.State.Should().Be(PipelineState.Implementing);
    }

    [Fact]
    public async Task TransitionTask_ForwardThroughAllStates_Succeeds()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Full Workflow", state: PipelineState.Planning, repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        // Simplified pipeline: Planning → Implementing → PrReady
        var states = new[]
        {
            PipelineState.Implementing,
            PipelineState.PrReady
        };

        // Act & Assert
        foreach (var state in states)
        {
            var dto = new TransitionTaskDtoBuilder().WithTargetState(state).Build();
            var response = await _client.PostAsJsonAsync($"{TasksUrl}/{entity.Id}/transition", dto, HttpClientExtensions.JsonOptions);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var task = await response.ReadAsAsync<TaskDto>();
            task!.State.Should().Be(state);
        }
    }

    [Fact]
    public async Task TransitionTask_BackwardOneState_Succeeds()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Backward Test", state: PipelineState.Implementing, repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        var dto = new TransitionTaskDtoBuilder()
            .WithTargetState(PipelineState.Planning)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync($"{TasksUrl}/{entity.Id}/transition", dto, HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.ReadAsAsync<TaskDto>();
        task!.State.Should().Be(PipelineState.Planning);
    }

    [Fact]
    public async Task TransitionTask_SkippingStates_ReturnsBadRequest()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Skip Test", state: PipelineState.Planning, repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        var dto = new TransitionTaskDtoBuilder()
            .WithTargetState(PipelineState.PrReady) // Skipping Implementing
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync($"{TasksUrl}/{entity.Id}/transition", dto, HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TransitionTask_ToSameState_ReturnsBadRequest()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Same State Test", state: PipelineState.Planning, repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        var dto = new TransitionTaskDtoBuilder()
            .WithTargetState(PipelineState.Planning) // Same state
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync($"{TasksUrl}/{entity.Id}/transition", dto, HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TransitionTask_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var dto = new TransitionTaskDtoBuilder()
            .WithTargetState(PipelineState.Planning)
            .Build();

        // Act
        var nonExistentId = Guid.NewGuid();
        var response = await _client.PostAsJsonAsync($"{TasksUrl}/{nonExistentId}/transition", dto, HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TransitionTask_EmitsSseEvent()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "SSE Transition", state: PipelineState.Planning, repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        var dto = new TransitionTaskDtoBuilder()
            .WithTargetState(PipelineState.Implementing)
            .Build();

        // Act
        await _client.PostAsJsonAsync($"{TasksUrl}/{entity.Id}/transition", dto, HttpClientExtensions.JsonOptions);

        // Assert
        await _factory.SseServiceMock.Received(1).EmitTaskUpdatedAsync(
            Arg.Is<TaskDto>(t => t.State == PipelineState.Implementing));
    }

    [Fact]
    public async Task TransitionTask_UpdatesTimestamp()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Timestamp Transition", state: PipelineState.Planning, repositoryId: _repositoryId, backlogItemId: _backlogItemId);
        var originalUpdatedAt = entity.UpdatedAt;
        await Task.Delay(10);

        var dto = new TransitionTaskDtoBuilder()
            .WithTargetState(PipelineState.Implementing)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync($"{TasksUrl}/{entity.Id}/transition", dto, HttpClientExtensions.JsonOptions);

        // Assert
        var task = await response.ReadAsAsync<TaskDto>();
        task!.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task TransitionTask_PersistsNewState()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Persist Transition", state: PipelineState.Planning, repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        var dto = new TransitionTaskDtoBuilder()
            .WithTargetState(PipelineState.Implementing)
            .Build();

        // Act
        await _client.PostAsJsonAsync($"{TasksUrl}/{entity.Id}/transition", dto, HttpClientExtensions.JsonOptions);

        // Assert
        await using var verifyDb = _factory.CreateDbContext();
        var updated = await verifyDb.Tasks.FindAsync(entity.Id);
        updated!.State.Should().Be(PipelineState.Implementing);
    }

    [Fact]
    public async Task TransitionTask_FromPrReadyToPlanning_ReturnsBadRequest()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "PrReady Task", state: PipelineState.PrReady, repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        var dto = new TransitionTaskDtoBuilder()
            .WithTargetState(PipelineState.Planning) // Multiple states away
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync($"{TasksUrl}/{entity.Id}/transition", dto, HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TransitionTask_WrongBacklogItem_ReturnsNotFound()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Wrong Backlog Task", state: PipelineState.Planning, repositoryId: _repositoryId, backlogItemId: _backlogItemId);
        var wrongBacklogItemId = Guid.NewGuid();

        var dto = new TransitionTaskDtoBuilder()
            .WithTargetState(PipelineState.Implementing)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/repositories/{_repositoryId}/backlog/{wrongBacklogItemId}/tasks/{entity.Id}/transition", dto, HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
