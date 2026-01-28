namespace Forge.Api.IntegrationTests.Features.Tasks;

[Collection("Api")]
public class GetAllTasksTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private Guid _repositoryId;
    private Guid _backlogItemId;

    public GetAllTasksTests(ForgeWebApplicationFactory factory)
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
    public async Task GetAllTasks_WithNoTasks_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync(TasksUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await response.ReadAsAsync<List<TaskDto>>();
        tasks.Should().NotBeNull();
        tasks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTasks_WithMultipleTasks_ReturnsAllTasks()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        await TestDatabaseHelper.SeedTaskAsync(db, "Task 1", "Description 1", repositoryId: _repositoryId, backlogItemId: _backlogItemId);
        await TestDatabaseHelper.SeedTaskAsync(db, "Task 2", "Description 2", repositoryId: _repositoryId, backlogItemId: _backlogItemId);
        await TestDatabaseHelper.SeedTaskAsync(db, "Task 3", "Description 3", repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        // Act
        var response = await _client.GetAsync(TasksUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await response.ReadAsAsync<List<TaskDto>>();
        tasks.Should().HaveCount(3);
        tasks.Select(t => t.Title).Should().Contain(["Task 1", "Task 2", "Task 3"]);
    }

    [Fact]
    public async Task GetAllTasks_ReturnsTasksOrderedByCreatedAtDescending()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task1 = await TestDatabaseHelper.SeedTaskAsync(db, "Old Task", repositoryId: _repositoryId, backlogItemId: _backlogItemId);
        await Task.Delay(10); // Ensure different timestamps
        var task2 = await TestDatabaseHelper.SeedTaskAsync(db, "New Task", repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        // Act
        var response = await _client.GetAsync(TasksUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await response.ReadAsAsync<List<TaskDto>>();
        tasks.Should().HaveCount(2);
        tasks![0].Title.Should().Be("New Task");
        tasks[1].Title.Should().Be("Old Task");
    }

    [Fact]
    public async Task GetAllTasks_WithDifferentPriorities_ReturnsCorrectPriorities()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        await TestDatabaseHelper.SeedTaskAsync(db, "Low Priority", priority: Priority.Low, repositoryId: _repositoryId, backlogItemId: _backlogItemId);
        await TestDatabaseHelper.SeedTaskAsync(db, "Critical Priority", priority: Priority.Critical, repositoryId: _repositoryId, backlogItemId: _backlogItemId);

        // Act
        var response = await _client.GetAsync(TasksUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await response.ReadAsAsync<List<TaskDto>>();
        tasks.Should().Contain(t => t.Title == "Low Priority" && t.Priority == Priority.Low);
        tasks.Should().Contain(t => t.Title == "Critical Priority" && t.Priority == Priority.Critical);
    }

    [Fact]
    public async Task GetAllTasks_WithNonExistentRepository_ReturnsNotFound()
    {
        // Act
        var nonExistentRepoId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/repositories/{nonExistentRepoId}/backlog/{_backlogItemId}/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllTasks_WithNonExistentBacklogItem_ReturnsNotFound()
    {
        // Act
        var nonExistentBacklogItemId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/repositories/{_repositoryId}/backlog/{nonExistentBacklogItemId}/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllTasks_OnlyReturnsTasksForSpecifiedBacklogItem()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var otherBacklogItem = await TestDatabaseHelper.SeedBacklogItemAsync(db, "Other Backlog Item", repositoryId: _repositoryId);
        await TestDatabaseHelper.SeedTaskAsync(db, "Task in main backlog", repositoryId: _repositoryId, backlogItemId: _backlogItemId);
        await TestDatabaseHelper.SeedTaskAsync(db, "Task in other backlog", repositoryId: _repositoryId, backlogItemId: otherBacklogItem.Id);

        // Act
        var response = await _client.GetAsync(TasksUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await response.ReadAsAsync<List<TaskDto>>();
        tasks.Should().HaveCount(1);
        tasks![0].Title.Should().Be("Task in main backlog");
    }
}
