namespace Forge.Api.IntegrationTests.Features.Tasks;

[Collection("Api")]
public class GetAllTasksTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GetAllTasksTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAllTasks_WithNoTasks_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/tasks");

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
        await TestDatabaseHelper.SeedTaskAsync(db, "Task 1", "Description 1");
        await TestDatabaseHelper.SeedTaskAsync(db, "Task 2", "Description 2");
        await TestDatabaseHelper.SeedTaskAsync(db, "Task 3", "Description 3");

        // Act
        var response = await _client.GetAsync("/api/tasks");

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
        var task1 = await TestDatabaseHelper.SeedTaskAsync(db, "Old Task");
        await Task.Delay(10); // Ensure different timestamps
        var task2 = await TestDatabaseHelper.SeedTaskAsync(db, "New Task");

        // Act
        var response = await _client.GetAsync("/api/tasks");

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
        await TestDatabaseHelper.SeedTaskAsync(db, "Low Priority", priority: Priority.Low);
        await TestDatabaseHelper.SeedTaskAsync(db, "Critical Priority", priority: Priority.Critical);

        // Act
        var response = await _client.GetAsync("/api/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await response.ReadAsAsync<List<TaskDto>>();
        tasks.Should().Contain(t => t.Title == "Low Priority" && t.Priority == Priority.Low);
        tasks.Should().Contain(t => t.Title == "Critical Priority" && t.Priority == Priority.Critical);
    }
}
