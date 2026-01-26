using System.Text.Json;
using System.Text.Json.Serialization;
using Claude.CodeSdk;
using Forge.Api.Data;
using Forge.Api.Features.Agent;
using Forge.Api.Features.Events;
using Forge.Api.Features.Notifications;
using Forge.Api.Features.Repository;
using Forge.Api.Features.Scheduler;
using Forge.Api.Features.Tasks;
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
builder.Services.AddSingleton<IClaudeAgentClientFactory, ClaudeAgentClientFactory>();
builder.Services.AddSingleton<IAgentRunnerService, AgentRunnerService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.Configure<SchedulerOptions>(builder.Configuration.GetSection(SchedulerOptions.SectionName));
builder.Services.AddSingleton<SchedulerState>();
builder.Services.AddScoped<SchedulerService>();
builder.Services.AddHostedService<TaskSchedulerService>();
builder.Services.AddSingleton<RepositoryService>();

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
using (var scope = app.Services.CreateScope())
{
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
app.MapAgentEndpoints();
app.MapEventEndpoints();
app.MapNotificationEndpoints();
app.MapSchedulerEndpoints();
app.MapRepositoryEndpoints();

await app.RunAsync();