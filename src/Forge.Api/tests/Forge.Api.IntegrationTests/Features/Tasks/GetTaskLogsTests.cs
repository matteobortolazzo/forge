namespace Forge.Api.IntegrationTests.Features.Tasks;

[Collection("Api")]
public class GetTaskLogsTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GetTaskLogsTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetTaskLogs_WithLogs_ReturnsAllLogs()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "Task with Logs");
        await TestDatabaseHelper.SeedTaskLogAsync(db, task.Id, LogType.Info, "Log 1");
        await TestDatabaseHelper.SeedTaskLogAsync(db, task.Id, LogType.ToolUse, "Log 2", "ReadFile");
        await TestDatabaseHelper.SeedTaskLogAsync(db, task.Id, LogType.Error, "Log 3");

        // Act
        var response = await _client.GetAsync($"/api/tasks/{task.Id}/logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var logs = await response.ReadAsAsync<List<TaskLogDto>>();
        logs.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetTaskLogs_WithNoLogs_ReturnsEmptyList()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "Task without Logs");

        // Act
        var response = await _client.GetAsync($"/api/tasks/{task.Id}/logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var logs = await response.ReadAsAsync<List<TaskLogDto>>();
        logs.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTaskLogs_WithNonExistentTaskId_ReturnsNotFound()
    {
        // Act
        var nonExistentId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/tasks/{nonExistentId}/logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTaskLogs_ReturnsLogsOrderedByTimestamp()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "Ordered Logs Task");
        await TestDatabaseHelper.SeedTaskLogAsync(db, task.Id, LogType.Info, "First log");
        await Task.Delay(10);
        await TestDatabaseHelper.SeedTaskLogAsync(db, task.Id, LogType.Info, "Second log");
        await Task.Delay(10);
        await TestDatabaseHelper.SeedTaskLogAsync(db, task.Id, LogType.Info, "Third log");

        // Act
        var response = await _client.GetAsync($"/api/tasks/{task.Id}/logs");

        // Assert
        var logs = await response.ReadAsAsync<List<TaskLogDto>>();
        logs![0].Content.Should().Be("First log");
        logs[1].Content.Should().Be("Second log");
        logs[2].Content.Should().Be("Third log");
    }

    [Fact]
    public async Task GetTaskLogs_ReturnsCorrectLogTypes()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "Log Types Task");
        await TestDatabaseHelper.SeedTaskLogAsync(db, task.Id, LogType.Info, "Info log");
        await TestDatabaseHelper.SeedTaskLogAsync(db, task.Id, LogType.ToolUse, "Tool use", "Bash");
        await TestDatabaseHelper.SeedTaskLogAsync(db, task.Id, LogType.ToolResult, "Tool result");
        await TestDatabaseHelper.SeedTaskLogAsync(db, task.Id, LogType.Error, "Error log");
        await TestDatabaseHelper.SeedTaskLogAsync(db, task.Id, LogType.Thinking, "Thinking log");

        // Act
        var response = await _client.GetAsync($"/api/tasks/{task.Id}/logs");

        // Assert
        var logs = await response.ReadAsAsync<List<TaskLogDto>>();
        logs.Should().Contain(l => l.Type == LogType.Info);
        logs.Should().Contain(l => l.Type == LogType.ToolUse);
        logs.Should().Contain(l => l.Type == LogType.ToolResult);
        logs.Should().Contain(l => l.Type == LogType.Error);
        logs.Should().Contain(l => l.Type == LogType.Thinking);
    }

    [Fact]
    public async Task GetTaskLogs_IncludesToolName()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "Tool Name Task");
        await TestDatabaseHelper.SeedTaskLogAsync(db, task.Id, LogType.ToolUse, "Reading file", "ReadFile");
        await TestDatabaseHelper.SeedTaskLogAsync(db, task.Id, LogType.ToolUse, "Running command", "Bash");

        // Act
        var response = await _client.GetAsync($"/api/tasks/{task.Id}/logs");

        // Assert
        var logs = await response.ReadAsAsync<List<TaskLogDto>>();
        logs.Should().Contain(l => l.ToolName == "ReadFile");
        logs.Should().Contain(l => l.ToolName == "Bash");
    }

    [Fact]
    public async Task GetTaskLogs_OnlyReturnsLogsForSpecifiedTask()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task1 = await TestDatabaseHelper.SeedTaskAsync(db, "Task 1");
        var task2 = await TestDatabaseHelper.SeedTaskAsync(db, "Task 2");
        await TestDatabaseHelper.SeedTaskLogAsync(db, task1.Id, LogType.Info, "Task 1 Log");
        await TestDatabaseHelper.SeedTaskLogAsync(db, task2.Id, LogType.Info, "Task 2 Log");

        // Act
        var response = await _client.GetAsync($"/api/tasks/{task1.Id}/logs");

        // Assert
        var logs = await response.ReadAsAsync<List<TaskLogDto>>();
        logs.Should().HaveCount(1);
        logs![0].Content.Should().Be("Task 1 Log");
        logs[0].TaskId.Should().Be(task1.Id);
    }

    [Fact]
    public async Task GetTaskLogs_ReturnsCorrectTaskId()
    {
        // Arrange
        await using var db = _factory.CreateDbContext();
        var task = await TestDatabaseHelper.SeedTaskAsync(db, "Task ID Test");
        await TestDatabaseHelper.SeedTaskLogAsync(db, task.Id, LogType.Info, "Test log");

        // Act
        var response = await _client.GetAsync($"/api/tasks/{task.Id}/logs");

        // Assert
        var logs = await response.ReadAsAsync<List<TaskLogDto>>();
        logs![0].TaskId.Should().Be(task.Id);
    }
}
