namespace Forge.Api.IntegrationTests.Features.Repositories;

[Collection("Api")]
public class SetDefaultRepositoryTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SetDefaultRepositoryTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SetDefaultRepository_WithValidId_SetsAsDefault()
    {
        // Arrange
        var createDto = new CreateRepositoryDtoBuilder()
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var created = await createResponse.ReadAsAsync<RepositoryDto>();

        // Act
        var response = await _client.PostAsync($"/api/repositories/{created!.Id}/set-default", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.ReadAsAsync<RepositoryDto>();
        updated!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task SetDefaultRepository_ClearsPreviousDefault()
    {
        // Arrange - Create first repository as default
        var firstDto = new CreateRepositoryDtoBuilder()
            .WithName("First")
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .AsDefault()
            .Build();
        var firstResponse = await _client.PostAsJsonAsync("/api/repositories", firstDto, HttpClientExtensions.JsonOptions);
        var first = await firstResponse.ReadAsAsync<RepositoryDto>();

        // Create second repository (non-default)
        var tempDir = Path.Combine(Path.GetTempPath(), $"forge_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var secondDto = new CreateRepositoryDtoBuilder()
                .WithName("Second")
                .WithPath(tempDir)
                .Build();
            var secondResponse = await _client.PostAsJsonAsync("/api/repositories", secondDto, HttpClientExtensions.JsonOptions);
            var second = await secondResponse.ReadAsAsync<RepositoryDto>();

            // Act - Set second as default
            await _client.PostAsync($"/api/repositories/{second!.Id}/set-default", null);

            // Assert
            await using var db = _factory.CreateDbContext();
            var firstEntity = await db.Repositories.FindAsync(first!.Id);
            var secondEntity = await db.Repositories.FindAsync(second.Id);
            firstEntity!.IsDefault.Should().BeFalse();
            secondEntity!.IsDefault.Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task SetDefaultRepository_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.PostAsync($"/api/repositories/{Guid.NewGuid()}/set-default", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetDefaultRepository_EmitsSseEvent()
    {
        // Arrange
        var createDto = new CreateRepositoryDtoBuilder()
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var created = await createResponse.ReadAsAsync<RepositoryDto>();

        _factory.SseServiceMock.ClearReceivedCalls();

        // Act
        await _client.PostAsync($"/api/repositories/{created!.Id}/set-default", null);

        // Assert
        await _factory.SseServiceMock.Received(1).EmitRepositoryUpdatedAsync(
            Arg.Is<RepositoryDto>(r => r.IsDefault == true));
    }

    [Fact]
    public async Task SetDefaultRepository_WhenAlreadyDefault_StaysDefault()
    {
        // Arrange
        var createDto = new CreateRepositoryDtoBuilder()
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .AsDefault()
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var created = await createResponse.ReadAsAsync<RepositoryDto>();

        // Act
        var response = await _client.PostAsync($"/api/repositories/{created!.Id}/set-default", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.ReadAsAsync<RepositoryDto>();
        updated!.IsDefault.Should().BeTrue();
    }
}
