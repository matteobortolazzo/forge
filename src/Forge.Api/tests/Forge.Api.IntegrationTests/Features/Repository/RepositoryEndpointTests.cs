using Forge.Api.Features.Repository;

namespace Forge.Api.IntegrationTests.Features.Repository;

[Collection("Api")]
public class RepositoryEndpointTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RepositoryEndpointTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetRepositoryInfo_ReturnsOkStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/repository/info");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRepositoryInfo_ReturnsRepositoryName()
    {
        // Act
        var response = await _client.GetAsync("/api/repository/info");
        var info = await response.ReadAsAsync<RepositoryInfoDto>();

        // Assert
        info.Should().NotBeNull();
        info!.Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetRepositoryInfo_ReturnsPath()
    {
        // Act
        var response = await _client.GetAsync("/api/repository/info");
        var info = await response.ReadAsAsync<RepositoryInfoDto>();

        // Assert
        info.Should().NotBeNull();
        info!.Path.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetRepositoryInfo_WhenInGitRepository_ReturnsGitDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/repository/info");
        var info = await response.ReadAsAsync<RepositoryInfoDto>();

        // Assert - Test server runs in the actual project directory which is a git repo
        info.Should().NotBeNull();
        info!.IsGitRepository.Should().BeTrue();
        info.Branch.Should().NotBeNullOrEmpty();
        info.CommitHash.Should().NotBeNullOrEmpty();
    }
}
