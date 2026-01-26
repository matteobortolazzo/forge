using Claude.CodeSdk;
using Forge.Api.Features.Events;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Forge.Api.IntegrationTests.Infrastructure;

public class ForgeWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection? _connection;

    public ISseService SseServiceMock { get; private set; } = null!;
    public IClaudeAgentClientFactory ClientFactoryMock { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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

            // Replace ISseService with mock
            services.RemoveAll<ISseService>();
            services.AddSingleton(SseServiceMock);

            // Replace IClaudeAgentClientFactory with mock
            services.RemoveAll<IClaudeAgentClientFactory>();
            services.AddSingleton(ClientFactoryMock);

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
        await db.TaskLogs.ExecuteDeleteAsync();
        await db.Tasks.ExecuteDeleteAsync();

        // Reset mock call history
        SseServiceMock.ClearReceivedCalls();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
