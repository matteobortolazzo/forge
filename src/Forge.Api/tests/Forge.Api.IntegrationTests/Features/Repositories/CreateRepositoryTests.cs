namespace Forge.Api.IntegrationTests.Features.Repositories;

[Collection("Api")]
public class CreateRepositoryTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CreateRepositoryTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateRepository_WithValidData_ReturnsCreatedRepository()
    {
        // Arrange
        var dto = new CreateRepositoryDtoBuilder()
            .WithName("Test Repo")
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/repositories", dto, HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var repo = await response.ReadAsAsync<RepositoryDto>();
        repo.Should().NotBeNull();
        repo!.Id.Should().NotBeEmpty();
        repo.Name.Should().Be("Test Repo");
        repo.Path.Should().Be(ForgeWebApplicationFactory.ProjectRoot);
        repo.IsDefault.Should().BeFalse();
        repo.IsActive.Should().BeTrue();
        repo.TaskCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateRepository_PersistsToDatabase()
    {
        // Arrange
        var dto = new CreateRepositoryDtoBuilder()
            .WithName("Persisted Repo")
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/repositories", dto, HttpClientExtensions.JsonOptions);
        var repo = await response.ReadAsAsync<RepositoryDto>();

        // Assert - Verify persistence
        await using var db = _factory.CreateDbContext();
        var entity = await db.Repositories.FindAsync(repo!.Id);
        entity.Should().NotBeNull();
        entity!.Name.Should().Be("Persisted Repo");
        entity.Path.Should().Be(ForgeWebApplicationFactory.ProjectRoot);
    }

    [Fact]
    public async Task CreateRepository_WithSetAsDefault_SetsAsDefault()
    {
        // Arrange
        var dto = new CreateRepositoryDtoBuilder()
            .WithName("Default Repo")
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .AsDefault()
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/repositories", dto, HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var repo = await response.ReadAsAsync<RepositoryDto>();
        repo!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task CreateRepository_WithSetAsDefault_ClearsPreviousDefault()
    {
        // Arrange - Create first default repository
        var firstDto = new CreateRepositoryDtoBuilder()
            .WithName("First Default")
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .AsDefault()
            .Build();
        await _client.PostAsJsonAsync("/api/repositories", firstDto, HttpClientExtensions.JsonOptions);

        // Create temp directory for second repository
        var tempDir = Path.Combine(Path.GetTempPath(), $"forge_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var secondDto = new CreateRepositoryDtoBuilder()
                .WithName("Second Default")
                .WithPath(tempDir)
                .AsDefault()
                .Build();

            // Act
            await _client.PostAsJsonAsync("/api/repositories", secondDto, HttpClientExtensions.JsonOptions);

            // Assert - First should no longer be default
            await using var db = _factory.CreateDbContext();
            var repos = await db.Repositories.Where(r => r.IsDefault && r.IsActive).ToListAsync();
            repos.Should().HaveCount(1);
            repos[0].Name.Should().Be("Second Default");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CreateRepository_ReturnsLocationHeader()
    {
        // Arrange
        var dto = new CreateRepositoryDtoBuilder()
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/repositories", dto, HttpClientExtensions.JsonOptions);
        var repo = await response.ReadAsAsync<RepositoryDto>();

        // Assert
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/repositories/{repo!.Id}");
    }

    [Fact]
    public async Task CreateRepository_EmitsSseEvent()
    {
        // Arrange
        var dto = new CreateRepositoryDtoBuilder()
            .WithName("SSE Repo")
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();

        // Act
        await _client.PostAsJsonAsync("/api/repositories", dto, HttpClientExtensions.JsonOptions);

        // Assert
        await _factory.SseServiceMock.Received(1).EmitRepositoryCreatedAsync(
            Arg.Is<RepositoryDto>(r => r.Name == "SSE Repo"));
    }

    [Fact]
    public async Task CreateRepository_WithNonExistentPath_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateRepositoryDtoBuilder()
            .WithName("Invalid Path Repo")
            .WithPath("/nonexistent/path/that/does/not/exist")
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/repositories", dto, HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRepository_WithDuplicatePath_ReturnsBadRequest()
    {
        // Arrange - Create first repository
        var firstDto = new CreateRepositoryDtoBuilder()
            .WithName("First Repo")
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        await _client.PostAsJsonAsync("/api/repositories", firstDto, HttpClientExtensions.JsonOptions);

        // Try to create second with same path
        var secondDto = new CreateRepositoryDtoBuilder()
            .WithName("Second Repo")
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/repositories", secondDto, HttpClientExtensions.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRepository_SetsTimestamps()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;
        var dto = new CreateRepositoryDtoBuilder()
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/repositories", dto, HttpClientExtensions.JsonOptions);
        var afterCreate = DateTime.UtcNow;

        // Assert
        var repo = await response.ReadAsAsync<RepositoryDto>();
        repo!.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        repo.CreatedAt.Should().BeOnOrBefore(afterCreate);
        repo.UpdatedAt.Should().BeOnOrAfter(beforeCreate);
        repo.UpdatedAt.Should().BeOnOrBefore(afterCreate);
    }

    [Fact]
    public async Task CreateRepository_DetectsGitRepository()
    {
        // Arrange - ProjectRoot is a git repo
        var dto = new CreateRepositoryDtoBuilder()
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/repositories", dto, HttpClientExtensions.JsonOptions);

        // Assert
        var repo = await response.ReadAsAsync<RepositoryDto>();
        repo!.IsGitRepository.Should().BeTrue();
        repo.Branch.Should().NotBeNullOrEmpty();
        repo.CommitHash.Should().NotBeNullOrEmpty();
    }
}
