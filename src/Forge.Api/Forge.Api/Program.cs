using System.Text.Json;
using System.Text.Json.Serialization;
using Claude.CodeSdk;
using Claude.CodeSdk.Mock;
using Forge.Api.Data;
using Forge.Api.Features.Agent;
using Forge.Api.Features.Events;
using Forge.Api.Features.Mock;
using Forge.Api.Features.Notifications;
using Forge.Api.Features.Repositories;
using Forge.Api.Features.Scheduler;
using Forge.Api.Features.Agents;
using Forge.Api.Features.Tasks;
using Forge.Api.Features.Worktree;
using Forge.Api.Features.Rollback;
using Forge.Api.Features.Subtasks;
using Forge.Api.Features.HumanGates;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Add OpenAPI
builder.Services.AddOpenApi();

// Database
var databasePath = builder.Configuration["DATABASE_PATH"] ?? "forge.db";
var connectionString = $"Data Source={databasePath}";
builder.Services.AddDbContext<ForgeDbContext>(options =>
    options.UseSqlite(connectionString));

// Services
builder.Services.AddSingleton<ISseService, SseService>();

// Claude agent client factory - use mock in E2E testing mode
var useMockMode = Environment.GetEnvironmentVariable("CLAUDE_MOCK_MODE") == "true";
if (useMockMode)
{
    builder.Services.AddSingleton<MockScenarioProvider>();
    builder.Services.AddSingleton<IClaudeAgentClientFactory, MockClaudeAgentClientFactory>();
}
else
{
    builder.Services.AddSingleton<IClaudeAgentClientFactory, ClaudeAgentClientFactory>();
}

builder.Services.AddSingleton<IAgentRunnerService, AgentRunnerService>();
builder.Services.AddScoped<IParentStateService, ParentStateService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.Configure<SchedulerOptions>(builder.Configuration.GetSection(SchedulerOptions.SectionName));
builder.Services.Configure<PipelineConfiguration>(builder.Configuration.GetSection(PipelineConfiguration.SectionName));
builder.Services.AddSingleton<SchedulerState>();
builder.Services.AddScoped<SchedulerService>();
builder.Services.AddHostedService<TaskSchedulerService>();
builder.Services.AddScoped<IRepositoryService, RepositoryService>();

// Agent orchestration services
builder.Services.AddSingleton<IAgentConfigLoader, AgentConfigLoader>();
builder.Services.AddSingleton<IContextDetector, ContextDetector>();
builder.Services.AddSingleton<IPromptBuilder, PromptBuilder>();
builder.Services.AddSingleton<IArtifactParser, ArtifactParser>();
builder.Services.AddSingleton<IOrchestratorService, OrchestratorService>();
builder.Services.AddSingleton<IWorktreeService, WorktreeService>();
builder.Services.AddScoped<IRollbackService, RollbackService>();
builder.Services.AddScoped<SubtaskService>();
builder.Services.AddScoped<HumanGateService>();

// CORS for Angular dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Apply pending migrations (creates database if not exists)
// Skip migrations if SKIP_MIGRATIONS is set (used in integration tests)
var skipMigrations = builder.Configuration.GetValue<bool>("SKIP_MIGRATIONS");
if (!skipMigrations)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ForgeDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

// Map endpoints
app.MapTaskEndpoints();
app.MapTaskArtifactEndpoints();
app.MapAgentEndpoints();
app.MapEventEndpoints();
app.MapNotificationEndpoints();
app.MapSchedulerEndpoints();
app.MapRepositoryEndpoints();
app.MapSubtaskEndpoints();
app.MapHumanGateEndpoints();

// Mock control endpoints (only in mock mode)
if (useMockMode)
{
    app.MapMockEndpoints();
}

await app.RunAsync();