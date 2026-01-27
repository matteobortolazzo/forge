namespace Forge.Api.IntegrationTests.Features.Repositories;

[Collection("Api")]
public class RefreshRepositoryTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RefreshRepositoryTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RefreshRepository_WithValidId_RefreshesGitInfo()
    {
        // Arrange
        var createDto = new CreateRepositoryDtoBuilder()
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var created = await createResponse.ReadAsAsync<RepositoryDto>();
        var originalRefreshedAt = created!.LastRefreshedAt;

        // Small delay to ensure timestamp difference
        await Task.Delay(10);

        // Act
        var response = await _client.PostAsync($"/api/repositories/{created.Id}/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await response.ReadAsAsync<RepositoryDto>();
        refreshed!.LastRefreshedAt.Should().BeAfter(originalRefreshedAt!.Value);
    }

    [Fact]
    public async Task RefreshRepository_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.PostAsync($"/api/repositories/{Guid.NewGuid()}/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RefreshRepository_EmitsSseEvent()
    {
        // Arrange
        var createDto = new CreateRepositoryDtoBuilder()
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var created = await createResponse.ReadAsAsync<RepositoryDto>();

        _factory.SseServiceMock.ClearReceivedCalls();

        // Act
        await _client.PostAsync($"/api/repositories/{created!.Id}/refresh", null);

        // Assert
        await _factory.SseServiceMock.Received(1).EmitRepositoryUpdatedAsync(
            Arg.Any<RepositoryDto>());
    }

    [Fact]
    public async Task RefreshRepository_UpdatesGitInfo()
    {
        // Arrange - ProjectRoot is a git repo
        var createDto = new CreateRepositoryDtoBuilder()
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var created = await createResponse.ReadAsAsync<RepositoryDto>();

        // Act
        var response = await _client.PostAsync($"/api/repositories/{created!.Id}/refresh", null);

        // Assert
        var refreshed = await response.ReadAsAsync<RepositoryDto>();
        refreshed!.IsGitRepository.Should().BeTrue();
        refreshed.Branch.Should().NotBeNullOrEmpty();
        refreshed.CommitHash.Should().NotBeNullOrEmpty();
    }
}
