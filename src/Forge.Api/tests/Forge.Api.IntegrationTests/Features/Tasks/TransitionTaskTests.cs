namespace Forge.Api.IntegrationTests.Features.Tasks;

[Collection("Api")]
public class TransitionTaskTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TransitionTaskTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TransitionTask_FromBacklogToPlanning_Succeeds()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Transition Test", state: PipelineState.Backlog);

        var dto = new TransitionTaskDtoBuilder()
            .WithTargetState(PipelineState.Planning)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/tasks/{entity.Id}/transition", dto, HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.ReadAsAsync<TaskDto>();
        task!.State.Should().Be(PipelineState.Planning);
    }

    [Fact]
    public async Task TransitionTask_ForwardThroughAllStates_Succeeds()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Full Workflow", state: PipelineState.Backlog);

        var states = new[]
        {
            PipelineState.Planning,
            PipelineState.Implementing,
            PipelineState.Reviewing,
            PipelineState.Testing,
            PipelineState.PrReady,
            PipelineState.Done
        };

        // Act & Assert
        foreach (var state in states)
        {
            var dto = new TransitionTaskDtoBuilder().WithTargetState(state).Build();
            var response = await _client.PostAsJsonAsync($"/api/tasks/{entity.Id}/transition", dto, HttpClientExtensions.JsonOptions);
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
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Backward Test", state: PipelineState.Implementing);

        var dto = new TransitionTaskDtoBuilder()
            .WithTargetState(PipelineState.Planning)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/tasks/{entity.Id}/transition", dto, HttpClientExtensions.JsonOptions);

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
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Skip Test", state: PipelineState.Backlog);

        var dto = new TransitionTaskDtoBuilder()
            .WithTargetState(PipelineState.Implementing) // Skipping Planning
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/tasks/{entity.Id}/transition", dto, HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TransitionTask_ToSameState_ReturnsBadRequest()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Same State Test", state: PipelineState.Planning);

        var dto = new TransitionTaskDtoBuilder()
            .WithTargetState(PipelineState.Planning) // Same state
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/tasks/{entity.Id}/transition", dto, HttpClientExtensions.JsonOptions);

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
        var response = await _client.PostAsJsonAsync($"/api/tasks/{nonExistentId}/transition", dto, HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TransitionTask_EmitsSseEvent()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "SSE Transition", state: PipelineState.Backlog);

        var dto = new TransitionTaskDtoBuilder()
            .WithTargetState(PipelineState.Planning)
            .Build();

        // Act
        await _client.PostAsJsonAsync($"/api/tasks/{entity.Id}/transition", dto, HttpClientExtensions.JsonOptions);

        // Assert
        await _factory.SseServiceMock.Received(1).EmitTaskUpdatedAsync(
            Arg.Is<TaskDto>(t => t.State == PipelineState.Planning));
    }

    [Fact]
    public async Task TransitionTask_UpdatesTimestamp()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Timestamp Transition", state: PipelineState.Backlog);
        var originalUpdatedAt = entity.UpdatedAt;
        await Task.Delay(10);

        var dto = new TransitionTaskDtoBuilder()
            .WithTargetState(PipelineState.Planning)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/tasks/{entity.Id}/transition", dto, HttpClientExtensions.JsonOptions);

        // Assert
        var task = await response.ReadAsAsync<TaskDto>();
        task!.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task TransitionTask_PersistsNewState()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Persist Transition", state: PipelineState.Backlog);

        var dto = new TransitionTaskDtoBuilder()
            .WithTargetState(PipelineState.Planning)
            .Build();

        // Act
        await _client.PostAsJsonAsync($"/api/tasks/{entity.Id}/transition", dto, HttpClientExtensions.JsonOptions);

        // Assert
        await using var verifyDb = _factory.CreateDbContext();
        var updated = await verifyDb.Tasks.FindAsync(entity.Id);
        updated!.State.Should().Be(PipelineState.Planning);
    }

    [Fact]
    public async Task TransitionTask_FromDoneToBacklog_ReturnsBadRequest()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var entity = await TestDatabaseHelper.SeedTaskAsync(db, "Done Task", state: PipelineState.Done);

        var dto = new TransitionTaskDtoBuilder()
            .WithTargetState(PipelineState.Backlog) // Multiple states away
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/tasks/{entity.Id}/transition", dto, HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
