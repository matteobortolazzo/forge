using Claude.CodeSdk;
using Forge.Api.Features.Agent;
using Forge.Api.Features.Events;
using Forge.Api.Features.Scheduler;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Forge.Api.IntegrationTests.Infrastructure;

public class ForgeWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection? _connection;
    private static readonly string TestDatabasePath = Path.Combine(Path.GetTempPath(), $"forge_test_{Guid.NewGuid()}.db");
    private static readonly string ProjectRoot = GetProjectRoot();

    public ISseService SseServiceMock { get; private set; } = null!;
    public IClaudeAgentClientFactory ClientFactoryMock { get; private set; } = null!;
    public IAgentRunnerService AgentRunnerServiceMock { get; private set; } = null!;

    private static string GetProjectRoot()
    {
        // Find the repository root by looking for .git folder
        var dir = AppContext.BaseDirectory;
        while (dir != null && !Directory.Exists(Path.Combine(dir, ".git")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }
        return dir ?? AppContext.BaseDirectory;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set test database path via configuration to prevent forge.db creation in project directory
        // Also skip migrations since we use EnsureCreated() for in-memory SQLite
        // Set REPOSITORY_PATH to the actual git repo for repository info tests
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DATABASE_PATH"] = TestDatabasePath,
                ["SKIP_MIGRATIONS"] = "true",
                ["REPOSITORY_PATH"] = ProjectRoot
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            services.RemoveAll<DbContextOptions<ForgeDbContext>>();
            services.RemoveAll<ForgeDbContext>();

            // Create a shared in-memory SQLite connection
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Add DbContext with in-memory SQLite
            services.AddDbContext<ForgeDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Create mocks
            SseServiceMock = Substitute.For<ISseService>();
            ClientFactoryMock = Substitute.For<IClaudeAgentClientFactory>();
            AgentRunnerServiceMock = Substitute.For<IAgentRunnerService>();

            // Default mock behavior - agent is not running
            AgentRunnerServiceMock.GetStatus().Returns(new AgentStatusDto(false, null, null));
            AgentRunnerServiceMock.StartAgentAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(true));
            AgentRunnerServiceMock.AbortAsync().Returns(Task.FromResult(false));

            // Replace ISseService with mock
            services.RemoveAll<ISseService>();
            services.AddSingleton(SseServiceMock);

            // Replace IClaudeAgentClientFactory with mock
            services.RemoveAll<IClaudeAgentClientFactory>();
            services.AddSingleton(ClientFactoryMock);

            // Replace IAgentRunnerService with mock
            services.RemoveAll<IAgentRunnerService>();
            services.AddSingleton(AgentRunnerServiceMock);

            // Remove TaskSchedulerService background service during tests
            services.RemoveAll<IHostedService>();

            // Build service provider to create database
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ForgeDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public ForgeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ForgeDbContext>()
            .UseSqlite(_connection!)
            .Options;
        return new ForgeDbContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        await using var db = CreateDbContext();
        await db.AgentArtifacts.ExecuteDeleteAsync();
        await db.Notifications.ExecuteDeleteAsync();
        await db.TaskLogs.ExecuteDeleteAsync();
        await db.Tasks.ExecuteDeleteAsync();

        // Reset mock call history
        SseServiceMock.ClearReceivedCalls();
        ClientFactoryMock.ClearReceivedCalls();
        AgentRunnerServiceMock.ClearReceivedCalls();

        // Reset agent mock default behavior
        AgentRunnerServiceMock.GetStatus().Returns(new AgentStatusDto(false, null, null));
        AgentRunnerServiceMock.StartAgentAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        AgentRunnerServiceMock.AbortAsync().Returns(Task.FromResult(false));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        // Clean up test database file if it was created
        if (File.Exists(TestDatabasePath))
        {
            File.Delete(TestDatabasePath);
        }
    }
}
