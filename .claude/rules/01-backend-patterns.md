# Backend Patterns

## Minimal API Endpoint Pattern

Each feature defines endpoints as a static class with a `Map*` extension method:

```csharp
// Features/Tasks/TaskEndpoints.cs
public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        // Tasks are scoped under repositories
        var group = app.MapGroup("/api/repositories/{repositoryId:guid}/tasks");

        group.MapGet("/", GetAllTasks);
        group.MapGet("/{id:guid}", GetTask);
        group.MapPost("/", CreateTask);
        group.MapPatch("/{id:guid}", UpdateTask);
        group.MapDelete("/{id:guid}", DeleteTask);
        group.MapPost("/{id:guid}/transition", TransitionTask);
        group.MapGet("/{id:guid}/logs", GetTaskLogs);
        group.MapPost("/{id:guid}/abort", AbortAgent);
        group.MapPost("/{id:guid}/start-agent", StartAgent);
    }
}
```

Register in Program.cs:
```csharp
app.MapRepositoryEndpoints();
app.MapTaskEndpoints();
app.MapAgentEndpoints();
app.MapEventEndpoints();
```

## Feature Folder Structure

```
Features/
├── Repositories/
│   ├── RepositoryEndpoints.cs  # 7 endpoints (list, get, create, update, delete, refresh, set-default)
│   ├── RepositoryService.cs    # Repository CRUD with git info caching (scoped)
│   └── RepositoryModels.cs     # DTOs: RepositoryDto, CreateRepositoryDto, UpdateRepositoryDto
├── Tasks/
│   ├── TaskEndpoints.cs        # 13 endpoints (CRUD, transition, logs, abort, start-agent, pause, resume, split)
│   ├── TaskArtifactEndpoints.cs # 4 endpoints (list, get, latest, by-state)
│   ├── TaskArtifactModels.cs   # DTOs: ArtifactDto
│   ├── TaskService.cs          # Business logic (scoped)
│   └── TaskModels.cs           # DTOs: TaskDto, CreateTaskDto, UpdateTaskDto, etc.
├── Subtasks/
│   ├── SubtaskEndpoints.cs     # 5 endpoints (list, get, create, update-status, delete)
│   ├── SubtaskService.cs       # Subtask lifecycle management (scoped)
│   └── SubtaskModels.cs        # DTOs: SubtaskDto, CreateSubtaskDto, etc.
├── HumanGates/
│   ├── HumanGateEndpoints.cs   # 4 endpoints (pending, get, resolve, by-task)
│   ├── HumanGateService.cs     # Gate management and resolution (scoped)
│   └── HumanGateModels.cs      # DTOs: HumanGateDto, ResolveHumanGateDto
├── Agents/
│   ├── OrchestratorService.cs  # Agent selection, prompt assembly, artifact management (singleton)
│   ├── AgentConfigLoader.cs    # Loads YAML configurations from agents/ directory (singleton)
│   ├── AgentConfig.cs          # Configuration DTOs: AgentConfig, AgentMatchRules, ResolvedAgentConfig
│   ├── ContextDetector.cs      # Repository language/framework detection (singleton)
│   ├── PromptBuilder.cs        # Template variable substitution (singleton)
│   └── ArtifactParser.cs       # Extracts structured content, confidence scores (singleton)
├── Agent/
│   ├── AgentEndpoints.cs       # GET /status
│   ├── AgentRunnerService.cs   # Claude Code process lifecycle (singleton)
│   └── AgentModels.cs          # AgentStatusDto
├── Scheduler/
│   ├── SchedulerEndpoints.cs   # 3 endpoints (status, enable, disable)
│   ├── SchedulerService.cs     # Task selection, human gates, simplification loops (scoped)
│   ├── TaskSchedulerService.cs # Background service for automatic scheduling (hosted)
│   ├── SchedulerState.cs       # Scheduler enabled/disabled state (singleton)
│   ├── SchedulerOptions.cs     # Configuration options
│   └── SchedulerModels.cs      # DTOs: SchedulerStatusDto, PauseTaskDto, AgentCompletionResult
├── Worktree/
│   └── WorktreeService.cs      # Git worktree creation/removal for subtasks (singleton)
├── Rollback/
│   └── RollbackService.cs      # Subtask and task rollback procedures (scoped)
├── Notifications/
│   ├── NotificationEndpoints.cs  # 4 endpoints (list, mark read, mark all read, unread count)
│   ├── NotificationService.cs    # Notification CRUD and task event helpers (scoped)
│   └── NotificationModels.cs     # DTOs: NotificationDto, CreateNotificationDto
├── Events/
│   ├── EventEndpoints.cs       # GET /events (SSE)
│   ├── EventDtos.cs            # DTOs for SSE events (HumanGateDto, SubtaskDto, etc.)
│   └── SseService.cs           # Channel-based event broadcasting (singleton)
├── AgentQuestions/
│   ├── AgentQuestionEndpoints.cs     # 3 endpoints (pending, get, answer)
│   ├── AgentQuestionService.cs       # Question lifecycle management (scoped)
│   ├── AgentQuestionWaiter.cs        # In-memory signaling (singleton)
│   ├── AgentQuestionModels.cs        # DTOs and strongly-typed models
│   └── AgentQuestionEntity.cs        # Database entity (in Data/Entities/)
└── Mock/
    └── MockEndpoints.cs        # 5 endpoints (status, scenarios, set, remove, reset) - E2E only
```

## Service Organization

- **Scoped Services**: DbContext-dependent (TaskService, SubtaskService, HumanGateService, RepositoryService)
- **Singleton Services**: Stateless or managing global state (SseService, AgentRunnerService, OrchestratorService)
- **Hosted Services**: Background processing (TaskSchedulerService)

## Integration Test Patterns

Located in `src/Forge.Api/tests/Forge.Api.IntegrationTests/`. Uses `WebApplicationFactory` with SQLite in-memory database.

**Key Patterns:**

1. **Shared JSON Options**: Use `HttpClientExtensions.JsonOptions` with built-in HTTP methods:
```csharp
// POST with built-in method (System.Net.Http.Json)
var response = await client.PostAsJsonAsync("/api/tasks", dto, HttpClientExtensions.JsonOptions);

// PATCH uses custom extension (no built-in equivalent)
var response = await client.PatchAsJsonAsync("/api/tasks/id", dto);

// Read response body
var task = await response.ReadAsAsync<TaskDto>();
```

2. **Test Database Reset**: Each test resets to clean state:
```csharp
public Task InitializeAsync() => _factory.ResetDatabaseAsync();
```

3. **Builder Pattern for Test Data**:
```csharp
var dto = new CreateTaskDtoBuilder()
    .WithTitle("Test Task")
    .WithPriority(Priority.High)
    .Build();
```

4. **Direct Database Verification**:
```csharp
await using var db = _factory.CreateDbContext();
var entity = await db.Tasks.FindAsync(taskId);
entity.Should().NotBeNull();
```

## SSE Implementation

```csharp
// Features/Events/EventEndpoints.cs
public static void MapEventEndpoints(this IEndpointRouteBuilder app)
{
    app.MapGet("/api/events", async (ISseService sseService, CancellationToken ct) =>
    {
        return Results.Stream(async stream =>
        {
            var writer = new StreamWriter(stream);
            await foreach (var evt in sseService.GetEventsAsync(ct))
            {
                await writer.WriteAsync(evt);
                await writer.FlushAsync();
            }
        }, "text/event-stream");
    });
}
```
