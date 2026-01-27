namespace Forge.Api.IntegrationTests.Features.Repositories;

[Collection("Api")]
public class UpdateRepositoryTests : IAsyncLifetime
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UpdateRepositoryTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UpdateRepository_WithValidData_ReturnsUpdatedRepository()
    {
        // Arrange
        var createDto = new CreateRepositoryDtoBuilder()
            .WithName("Original Name")
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var created = await createResponse.ReadAsAsync<RepositoryDto>();

        var updateDto = new UpdateRepositoryDtoBuilder()
            .WithName("Updated Name")
            .Build();

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/repositories/{created!.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.ReadAsAsync<RepositoryDto>();
        updated!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateRepository_PersistsChanges()
    {
        // Arrange
        var createDto = new CreateRepositoryDtoBuilder()
            .WithName("Original Name")
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var created = await createResponse.ReadAsAsync<RepositoryDto>();

        var updateDto = new UpdateRepositoryDtoBuilder()
            .WithName("Persisted Name")
            .Build();

        // Act
        await _client.PatchAsJsonAsync($"/api/repositories/{created!.Id}", updateDto);

        // Assert
        await using var db = _factory.CreateDbContext();
        var entity = await db.Repositories.FindAsync(created.Id);
        entity!.Name.Should().Be("Persisted Name");
    }

    [Fact]
    public async Task UpdateRepository_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var updateDto = new UpdateRepositoryDtoBuilder()
            .WithName("New Name")
            .Build();

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/repositories/{Guid.NewGuid()}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateRepository_EmitsSseEvent()
    {
        // Arrange
        var createDto = new CreateRepositoryDtoBuilder()
            .WithName("Original")
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var created = await createResponse.ReadAsAsync<RepositoryDto>();

        _factory.SseServiceMock.ClearReceivedCalls();

        var updateDto = new UpdateRepositoryDtoBuilder()
            .WithName("SSE Updated")
            .Build();

        // Act
        await _client.PatchAsJsonAsync($"/api/repositories/{created!.Id}", updateDto);

        // Assert
        await _factory.SseServiceMock.Received(1).EmitRepositoryUpdatedAsync(
            Arg.Is<RepositoryDto>(r => r.Name == "SSE Updated"));
    }

    [Fact]
    public async Task UpdateRepository_UpdatesTimestamp()
    {
        // Arrange
        var createDto = new CreateRepositoryDtoBuilder()
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var created = await createResponse.ReadAsAsync<RepositoryDto>();

        var beforeUpdate = DateTime.UtcNow;
        var updateDto = new UpdateRepositoryDtoBuilder()
            .WithName("Updated")
            .Build();

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/repositories/{created!.Id}", updateDto);
        var afterUpdate = DateTime.UtcNow;

        // Assert
        var updated = await response.ReadAsAsync<RepositoryDto>();
        updated!.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
        updated.UpdatedAt.Should().BeOnOrBefore(afterUpdate);
    }

    [Fact]
    public async Task UpdateRepository_PreservesOtherFields()
    {
        // Arrange
        var createDto = new CreateRepositoryDtoBuilder()
            .WithName("Original")
            .WithPath(ForgeWebApplicationFactory.ProjectRoot)
            .Build();
        var createResponse = await _client.PostAsJsonAsync("/api/repositories", createDto, HttpClientExtensions.JsonOptions);
        var created = await createResponse.ReadAsAsync<RepositoryDto>();

        var updateDto = new UpdateRepositoryDtoBuilder()
            .WithName("Updated")
            .Build();

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/repositories/{created!.Id}", updateDto);

        // Assert
        var updated = await response.ReadAsAsync<RepositoryDto>();
        updated!.Path.Should().Be(ForgeWebApplicationFactory.ProjectRoot);
        updated.IsActive.Should().BeTrue();
    }
}
