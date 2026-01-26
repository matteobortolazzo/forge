using Forge.Api.Features.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Forge.Api.UnitTests.Features.Repository;

public class RepositoryServiceTests
{
    private readonly ILogger<RepositoryService> _loggerMock;

    public RepositoryServiceTests()
    {
        _loggerMock = Substitute.For<ILogger<RepositoryService>>();
    }

    [Fact]
    public void GetRepositoryInfo_UsesRepositoryPathFromConfiguration()
    {
        // Arrange
        var testPath = Directory.GetCurrentDirectory();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["REPOSITORY_PATH"] = testPath
            })
            .Build();

        var sut = new RepositoryService(config, _loggerMock);

        // Act
        var result = sut.GetRepositoryInfo();

        // Assert
        result.Path.Should().Be(testPath);
    }

    [Fact]
    public void GetRepositoryInfo_FallsBackToCurrentDirectory_WhenConfigNotSet()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        var sut = new RepositoryService(config, _loggerMock);

        // Act
        var result = sut.GetRepositoryInfo();

        // Assert
        result.Path.Should().Be(Environment.CurrentDirectory);
    }

    [Fact]
    public void GetRepositoryInfo_ExtractsNameFromPath()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "test-repo");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["REPOSITORY_PATH"] = testPath
            })
            .Build();

        var sut = new RepositoryService(config, _loggerMock);

        // Act
        var result = sut.GetRepositoryInfo();

        // Assert
        result.Name.Should().Be("test-repo");
    }

    [Fact]
    public void GetRepositoryInfo_ReturnsIsGitRepositoryFalse_WhenNoGitFolder()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), $"non-git-{Guid.NewGuid()}");
        Directory.CreateDirectory(testPath);

        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["REPOSITORY_PATH"] = testPath
                })
                .Build();

            var sut = new RepositoryService(config, _loggerMock);

            // Act
            var result = sut.GetRepositoryInfo();

            // Assert
            result.IsGitRepository.Should().BeFalse();
            result.Branch.Should().BeNull();
            result.CommitHash.Should().BeNull();
            result.RemoteUrl.Should().BeNull();
            result.IsDirty.Should().BeFalse();
        }
        finally
        {
            Directory.Delete(testPath, true);
        }
    }

    [Fact]
    public void GetRepositoryInfo_ReturnsGitInfo_WhenGitRepository()
    {
        // Arrange - Use the actual project root which is a git repo
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["REPOSITORY_PATH"] = GetProjectRoot()
            })
            .Build();

        var sut = new RepositoryService(config, _loggerMock);

        // Act
        var result = sut.GetRepositoryInfo();

        // Assert
        result.IsGitRepository.Should().BeTrue();
        result.Branch.Should().NotBeNullOrEmpty();
        result.CommitHash.Should().NotBeNullOrEmpty();
        result.CommitHash!.Length.Should().BeLessThanOrEqualTo(10); // Short hash
    }

    private static string GetProjectRoot()
    {
        // Navigate from test project up to the repo root
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }
}
