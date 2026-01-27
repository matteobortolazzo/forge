namespace Forge.Api.IntegrationTests.Features.Tasks;

[Collection("Api")]
public class GetAllTasksTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private Guid _repositoryId;

    public GetAllTasksTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        // Create a repository for all tests
        await using var db = _factory.CreateDbContext();
        var repo = await TestDatabaseHelper.SeedRepositoryAsync(db);
        _repositoryId = repo.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAllTasks_WithNoTasks_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync($"/api/repositories/{_repositoryId}/tasks");

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
        await TestDatabaseHelper.SeedTaskAsync(db, "Task 1", "Description 1", repositoryId: _repositoryId);
        await TestDatabaseHelper.SeedTaskAsync(db, "Task 2", "Description 2", repositoryId: _repositoryId);
        await TestDatabaseHelper.SeedTaskAsync(db, "Task 3", "Description 3", repositoryId: _repositoryId);

        // Act
        var response = await _client.GetAsync($"/api/repositories/{_repositoryId}/tasks");

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
        var task1 = await TestDatabaseHelper.SeedTaskAsync(db, "Old Task", repositoryId: _repositoryId);
        await Task.Delay(10); // Ensure different timestamps
        var task2 = await TestDatabaseHelper.SeedTaskAsync(db, "New Task", repositoryId: _repositoryId);

        // Act
        var response = await _client.GetAsync($"/api/repositories/{_repositoryId}/tasks");

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
        await TestDatabaseHelper.SeedTaskAsync(db, "Low Priority", priority: Priority.Low, repositoryId: _repositoryId);
        await TestDatabaseHelper.SeedTaskAsync(db, "Critical Priority", priority: Priority.Critical, repositoryId: _repositoryId);

        // Act
        var response = await _client.GetAsync($"/api/repositories/{_repositoryId}/tasks");

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
        var response = await _client.GetAsync($"/api/repositories/{nonExistentRepoId}/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllTasks_OnlyReturnsTasksForSpecifiedRepository()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var otherRepoPath = Path.Combine(Path.GetTempPath(), $"other-repo-{Guid.NewGuid()}");
        var otherRepo = await TestDatabaseHelper.SeedRepositoryAsync(db, "Other Repo", path: otherRepoPath);
        await TestDatabaseHelper.SeedTaskAsync(db, "Task in main repo", repositoryId: _repositoryId);
        await TestDatabaseHelper.SeedTaskAsync(db, "Task in other repo", repositoryId: otherRepo.Id);

        // Act
        var response = await _client.GetAsync($"/api/repositories/{_repositoryId}/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await response.ReadAsAsync<List<TaskDto>>();
        tasks.Should().HaveCount(1);
        tasks![0].Title.Should().Be("Task in main repo");
    }
}
