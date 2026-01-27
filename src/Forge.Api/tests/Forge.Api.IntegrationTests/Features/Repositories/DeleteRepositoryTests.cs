namespace Forge.Api.IntegrationTests.Features.Repositories;

[Collection("Api")]
public class DeleteRepositoryTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DeleteRepositoryTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DeleteRepository_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var createDto = new CreateRepositoryDtoBuilder()
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var created = await createResponse.ReadAsAsync<RepositoryDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/repositories/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteRepository_SoftDeletes()
    {
        // Arrange
        var createDto = new CreateRepositoryDtoBuilder()
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var created = await createResponse.ReadAsAsync<RepositoryDto>();

        // Act
        await _client.DeleteAsync($"/api/repositories/{created!.Id}");

        // Assert - Entity still exists but is inactive
        await using var db = _factory.CreateDbContext();
        var entity = await db.Repositories.FindAsync(created.Id);
        entity.Should().NotBeNull();
        entity!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteRepository_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/repositories/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRepository_WithTasks_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateRepositoryDtoBuilder()
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var repo = await createResponse.ReadAsAsync<RepositoryDto>();

        // Create a task
        var taskDto = new CreateTaskDtoBuilder()
            .WithTitle("Test Task")
            .Build();
        await _client.PostAsJsonAsync($"/api/repositories/{repo!.Id}/tasks", taskDto, HttpClientExtensions.JsonOptions);

        // Act
        var response = await _client.DeleteAsync($"/api/repositories/{repo.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteRepository_EmitsSseEvent()
    {
        // Arrange
        var createDto = new CreateRepositoryDtoBuilder()
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var created = await createResponse.ReadAsAsync<RepositoryDto>();

        _factory.SseServiceMock.ClearReceivedCalls();

        // Act
        await _client.DeleteAsync($"/api/repositories/{created!.Id}");

        // Assert
        await _factory.SseServiceMock.Received(1).EmitRepositoryDeletedAsync(created.Id);
    }

    [Fact]
    public async Task DeleteRepository_ClearsDefaultStatus()
    {
        // Arrange
        var createDto = new CreateRepositoryDtoBuilder()
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .AsDefault()
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var created = await createResponse.ReadAsAsync<RepositoryDto>();

        // Act
        await _client.DeleteAsync($"/api/repositories/{created!.Id}");

        // Assert
        await using var db = _factory.CreateDbContext();
        var entity = await db.Repositories.FindAsync(created.Id);
        entity!.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteRepository_AlreadyDeleted_ReturnsNotFound()
    {
        // Arrange
        var createDto = new CreateRepositoryDtoBuilder()
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var created = await createResponse.ReadAsAsync<RepositoryDto>();
        await _client.DeleteAsync($"/api/repositories/{created!.Id}");

        // Act - Try to delete again
        var response = await _client.DeleteAsync($"/api/repositories/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
