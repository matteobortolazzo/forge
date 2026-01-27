namespace Forge.Api.IntegrationTests.Features.Tasks;

[Collection("Api")]
public class CreateTaskTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private Guid _repositoryId;

    public CreateTaskTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        // Create a repository for all task tests
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
    public async Task CreateTask_WithValidData_ReturnsCreatedTask()
    {
        // Arrange
        var dto = new CreateTaskDtoBuilder()
            .WithTitle("New Feature")
            .WithDescription("Implement new feature")
            .WithPriority(Priority.High)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/repositories/{_repositoryId}/tasks", dto, HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = await response.ReadAsAsync<TaskDto>();
        task.Should().NotBeNull();
        task!.Id.Should().NotBeEmpty();
        task.RepositoryId.Should().Be(_repositoryId);
        task.Title.Should().Be("New Feature");
        task.Description.Should().Be("Implement new feature");
        task.Priority.Should().Be(Priority.High);
        task.State.Should().Be(PipelineState.Backlog);
        task.HasError.Should().BeFalse();
        task.AssignedAgentId.Should().BeNull();
    }

    [Fact]
    public async Task CreateTask_PersistsToDatabase()
    {
        // Arrange
        var dto = new CreateTaskDtoBuilder()
            .WithTitle("Persisted Task")
            .WithDescription("Should persist")
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/repositories/{_repositoryId}/tasks", dto, HttpClientExtensions.JsonOptions);
        var task = await response.ReadAsAsync<TaskDto>();

        // Assert - Verify persistence
        await using var db = _factory.CreateDbContext();
        var entity = await db.Tasks.FindAsync(task!.Id);
        entity.Should().NotBeNull();
        entity!.Title.Should().Be("Persisted Task");
        entity.Description.Should().Be("Should persist");
        entity.RepositoryId.Should().Be(_repositoryId);
    }

    [Fact]
    public async Task CreateTask_EmitsSseEvent()
    {
        // Arrange
        var dto = new CreateTaskDtoBuilder()
            .WithTitle("SSE Task")
            .Build();

        // Act
        await _client.PostAsJsonAsync($"/api/repositories/{_repositoryId}/tasks", dto, HttpClientExtensions.JsonOptions);

        // Assert
        await _factory.SseServiceMock.Received(1).EmitTaskCreatedAsync(
            Arg.Is<TaskDto>(t => t.Title == "SSE Task"));
    }

    [Fact]
    public async Task CreateTask_ReturnsLocationHeader()
    {
        // Arrange
        var dto = new CreateTaskDtoBuilder().Build();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/repositories/{_repositoryId}/tasks", dto, HttpClientExtensions.JsonOptions);
        var task = await response.ReadAsAsync<TaskDto>();

        // Assert
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/repositories/{_repositoryId}/tasks/{task!.Id}");
    }

    [Theory]
    [InlineData(Priority.Low)]
    [InlineData(Priority.Medium)]
    [InlineData(Priority.High)]
    [InlineData(Priority.Critical)]
    public async Task CreateTask_WithDifferentPriorities_ReturnsCorrectPriority(Priority priority)
    {
        // Arrange
        var dto = new CreateTaskDtoBuilder()
            .WithTitle($"{priority} Priority Task")
            .WithPriority(priority)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/repositories/{_repositoryId}/tasks", dto, HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = await response.ReadAsAsync<TaskDto>();
        task!.Priority.Should().Be(priority);
    }

    [Fact]
    public async Task CreateTask_SetsTimestamps()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;
        var dto = new CreateTaskDtoBuilder().Build();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/repositories/{_repositoryId}/tasks", dto, HttpClientExtensions.JsonOptions);
        var afterCreate = DateTime.UtcNow;

        // Assert
        var task = await response.ReadAsAsync<TaskDto>();
        task!.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        task.CreatedAt.Should().BeOnOrBefore(afterCreate);
        task.UpdatedAt.Should().BeOnOrAfter(beforeCreate);
        task.UpdatedAt.Should().BeOnOrBefore(afterCreate);
    }

    [Fact]
    public async Task CreateTask_WithNonExistentRepository_ReturnsNotFound()
    {
        // Arrange
        var dto = new CreateTaskDtoBuilder().Build();
        var nonExistentRepoId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/repositories/{nonExistentRepoId}/tasks", dto, HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
