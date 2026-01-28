namespace Forge.Api.IntegrationTests.Features.Repositories;

[Collection("Api")]
public class GetAllRepositoriesTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GetAllRepositoriesTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAllRepositories_WhenEmpty_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/repositories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var repos = await response.ReadAsAsync<List<RepositoryDto>>();
        repos.Should().NotBeNull();
        repos.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllRepositories_ReturnsAllActiveRepositories()
    {
        // Arrange - Create a repository
        var createDto = new CreateRepositoryDtoBuilder()
            .WithName("Test Repo")
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);

        // Act
        var response = await _client.GetAsync("/api/repositories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var repos = await response.ReadAsAsync<List<RepositoryDto>>();
        repos.Should().HaveCount(1);
        repos![0].Name.Should().Be("Test Repo");
    }

    [Fact]
    public async Task GetAllRepositories_ExcludesInactiveRepositories()
    {
        // Arrange - Create and delete a repository
        var createDto = new CreateRepositoryDtoBuilder()
            .WithName("Deleted Repo")
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var repo = await createResponse.ReadAsAsync<RepositoryDto>();
        await _client.DeleteAsync($"/api/repositories/{repo!.Id}");

        // Act
        var response = await _client.GetAsync("/api/repositories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var repos = await response.ReadAsAsync<List<RepositoryDto>>();
        repos.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllRepositories_OrdersByCreatedAtDescending()
    {
        // Arrange - Create first repository
        var firstDto = new CreateRepositoryDtoBuilder()
            .WithName("First Repo")
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        await _client.PostAsJsonAsync("/api/repositories", firstDto, HttpClientExtensions.JsonOptions);

        // Create temp directory for second
        var tempDir = Path.Combine(Path.GetTempPath(), $"forge_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var secondDto = new CreateRepositoryDtoBuilder()
                .WithName("Second Repo")
                .WithPath(tempDir)
                .Build();
            await _client.PostAsJsonAsync("/api/repositories", secondDto, HttpClientExtensions.JsonOptions);

            // Act
            var response = await _client.GetAsync("/api/repositories");

            // Assert - Newest first
            var repos = await response.ReadAsAsync<List<RepositoryDto>>();
            repos.Should().HaveCount(2);
            repos![0].Name.Should().Be("Second Repo"); // Created second, so newest
            repos[1].Name.Should().Be("First Repo");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetAllRepositories_IncludesBacklogItemCount()
    {
        // Arrange - Create repository
        var createDto = new CreateRepositoryDtoBuilder()
            .WithName("Repo With Backlog Items")
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var repoResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var repo = await repoResponse.ReadAsAsync<RepositoryDto>();

        // Create a backlog item in this repository
        var backlogDto = new CreateBacklogItemDtoBuilder()
            .WithTitle("Test Backlog Item")
            .Build();
        await _client.PostAsJsonAsync($"/api/repositories/{repo!.Id}/backlog", backlogDto, HttpClientExtensions.JsonOptions);

        // Act
        var response = await _client.GetAsync("/api/repositories");

        // Assert
        var repos = await response.ReadAsAsync<List<RepositoryDto>>();
        repos![0].BacklogItemCount.Should().Be(1);
    }
}
