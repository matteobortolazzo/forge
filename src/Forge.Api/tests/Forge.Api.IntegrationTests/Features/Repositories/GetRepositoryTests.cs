namespace Forge.Api.IntegrationTests.Features.Repositories;

[Collection("Api")]
public class GetRepositoryTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GetRepositoryTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetRepository_WithValidId_ReturnsRepository()
    {
        // Arrange
        var createDto = new CreateRepositoryDtoBuilder()
            .WithName("Test Repo")
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var created = await createResponse.ReadAsAsync<RepositoryDto>();

        // Act
        var response = await _client.GetAsync($"/api/repositories/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var repo = await response.ReadAsAsync<RepositoryDto>();
        repo.Should().NotBeNull();
        repo!.Id.Should().Be(created.Id);
        repo.Name.Should().Be("Test Repo");
    }

    [Fact]
    public async Task GetRepository_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/repositories/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRepository_WithDeletedRepository_ReturnsNotFound()
    {
        // Arrange - Create and delete
        var createDto = new CreateRepositoryDtoBuilder()
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var created = await createResponse.ReadAsAsync<RepositoryDto>();
        await _client.DeleteAsync($"/api/repositories/{created!.Id}");

        // Act
        var response = await _client.GetAsync($"/api/repositories/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRepository_IncludesGitInfo()
    {
        // Arrange - ProjectRoot is a git repository
        var createDto = new CreateRepositoryDtoBuilder()
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var created = await createResponse.ReadAsAsync<RepositoryDto>();

        // Act
        var response = await _client.GetAsync($"/api/repositories/{created!.Id}");

        // Assert
        var repo = await response.ReadAsAsync<RepositoryDto>();
        repo!.IsGitRepository.Should().BeTrue();
        repo.Branch.Should().NotBeNullOrEmpty();
        repo.CommitHash.Should().NotBeNullOrEmpty();
        repo.LastRefreshedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRepository_IncludesTaskCount()
    {
        // Arrange
        var createDto = new CreateRepositoryDtoBuilder()
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var repo = await createResponse.ReadAsAsync<RepositoryDto>();

        // Create tasks
        var taskDto = new CreateTaskDtoBuilder()
            .WithTitle("Task 1")
            .Build();
        await _client.PostAsJsonAsync($"/api/repositories/{repo!.Id}/tasks", taskDto, HttpClientExtensions.JsonOptions);
        await _client.PostAsJsonAsync($"/api/repositories/{repo.Id}/tasks", taskDto, HttpClientExtensions.JsonOptions);

        // Act
        var response = await _client.GetAsync($"/api/repositories/{repo.Id}");

        // Assert
        var fetched = await response.ReadAsAsync<RepositoryDto>();
        fetched!.TaskCount.Should().Be(2);
    }
}
