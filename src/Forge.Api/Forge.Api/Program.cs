using System.Text.Json;
using System.Text.Json.Serialization;
using Claude.CodeSdk;
using Forge.Api.Data;
using Forge.Api.Features.Agent;
using Forge.Api.Features.Events;
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
builder.Services.AddSingleton<AgentRunnerService>();
builder.Services.AddScoped<TaskService>();

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

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ForgeDbContext>();
    await db.Database.EnsureCreatedAsync();
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

await app.RunAsync();