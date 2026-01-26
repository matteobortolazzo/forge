# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Forge is an AI Agent Dashboard for orchestrating and monitoring AI coding agents powered by Claude Code CLI. The system implements a Kanban-style pipeline where tasks flow through stages (Backlog → Planning → Implementing → Reviewing → Testing → PR Ready → Done), with agents executed via stdin/stdout communication with the Claude Code process.

## Repository Structure

```
forge/
├── src/
│   ├── Forge.Api/                  # .NET 10 API Solution
│   │   ├── Forge.Api/              # Main API project
│   │   │   ├── Features/           # Task, Agent, Events, Scheduler endpoints
│   │   │   │   ├── Tasks/          # Task CRUD, transitions, logs, agent start, pause/resume
│   │   │   │   ├── Agent/          # Agent status, runner service
│   │   │   │   ├── Scheduler/      # Automatic task scheduling
│   │   │   │   └── Events/         # SSE endpoint
│   │   │   ├── Data/               # EF Core DbContext, Entities
│   │   │   ├── Shared/             # Enums, common types
│   │   │   └── Program.cs          # Entry point
│   │   ├── Claude.CodeSdk/         # C# SDK for Claude Code CLI
│   │   └── tests/                  # Test projects
│   └── Forge.Ui/                   # Angular 21 SPA
│       └── src/app/
│           ├── features/           # Feature folders
│           │   ├── board/          # Kanban board components
│           │   ├── task-detail/    # Task detail view with logs
│           │   └── notifications/  # Notification panel
│           ├── core/               # Stores, Services, Mocks
│           ├── shared/             # Reusable components, models
│           └── app.routes.ts       # Route configuration
```

**Code Organization**: Both API and UI use feature folder organization. Each feature contains all necessary files (endpoints, services, components, models).

## Implementation Status

### Backend (Fully Implemented)

| Feature       | Files                                                                       | Description                                            |
|---------------|-----------------------------------------------------------------------------|--------------------------------------------------------|
| Tasks         | `TaskEndpoints.cs`, `TaskService.cs`, `TaskModels.cs`                       | 11 endpoints for CRUD, transitions, logs, agent control, pause/resume |
| Agent         | `AgentEndpoints.cs`, `AgentRunnerService.cs`, `AgentModels.cs`              | Agent status, process lifecycle management             |
| Scheduler     | `SchedulerEndpoints.cs`, `SchedulerService.cs`, `TaskSchedulerService.cs`, `SchedulerModels.cs` | Automatic task scheduling and pipeline progression |
| Events        | `EventEndpoints.cs`, `SseService.cs`                                        | SSE event broadcasting via channels                    |
| Notifications | `NotificationEndpoints.cs`, `NotificationService.cs`, `NotificationModels.cs` | Notification CRUD, SSE events                          |
| Data          | `ForgeDbContext.cs`, `TaskEntity.cs`, `TaskLogEntity.cs`, `NotificationEntity.cs` | EF Core with SQLite                                    |
| Shared        | `Enums.cs`                                                                  | TaskState, Priority, NotificationType enums            |

### Claude.CodeSdk (Fully Implemented)

| Component      | Files                                                                           | Description                     |
|----------------|---------------------------------------------------------------------------------|---------------------------------|
| Client         | `ClaudeAgentClient.cs`, `ClaudeAgentOptions.cs`                                 | Main client for CLI interaction |
| Messages       | `SystemMessage.cs`, `UserMessage.cs`, `AssistantMessage.cs`, `ResultMessage.cs` | Strongly-typed message models   |
| Content Blocks | `TextBlock.cs`, `ToolUseBlock.cs`, `ToolResultBlock.cs`                         | Content block types             |
| Exceptions     | `CliNotFoundException.cs`, `ProcessException.cs`, `JsonDecodeException.cs`      | Error handling                  |
| Internal       | `CliProcess.cs`, `MessageParser.cs`, `CommandBuilder.cs`, `CliLocator.cs`       | CLI process management          |

### Frontend (Partially Implemented)

See `src/Forge.Ui/README.md` for complete component inventory.

**Note:** Scheduler UI components exist but are not fully integrated:
- SchedulerService, SchedulerStore, SchedulerStatusComponent, PausedBadgeComponent created
- Components display with mock data but SSE event handling doesn't update SchedulerStore
- Pause/resume UI works via TaskStore, but scheduler enable/disable toggle needs real API wiring

## Tech Stack

| Component         | Technology                       | Version  |
|-------------------|----------------------------------|----------|
| Frontend          | Angular                          | 21.x     |
| State Management  | Angular Signals                  | Built-in |
| UI Components     | Angular CDK                      | Latest   |
| Styling           | Tailwind CSS                     | 4.x      |
| Backend           | .NET                             | 10.x     |
| Backend Framework | ASP.NET Core Minimal APIs        | 10.x     |
| Real-time         | EventSource/SSE                  | Native   |
| Database          | SQLite (dev) / PostgreSQL (prod) | -        |
| ORM               | Entity Framework Core            | 10.x     |
| Agent Execution   | Claude Code CLI                  | Latest   |
| Frontend Testing  | Vitest                           | 4.x      |

## Documentation Sources (Context7)

When querying documentation via Context7 MCP, use these library IDs:

| Technology          | Context7 Library ID                      |
|---------------------|------------------------------------------|
| .NET / ASP.NET Core | `/websites/learn_microsoft_en-us_dotnet` |

## Internal Library Documentation

When working with internal libraries, read their documentation files:

| Library        | Documentation Path                       |
|----------------|------------------------------------------|
| Claude.CodeSdk | `src/Forge.Api/Claude.CodeSdk/README.md` |

**Claude.CodeSdk**: C# SDK for programmatic interaction with Claude Code CLI. Provides `ClaudeAgentClient` for spawning CLI processes, streaming NDJSON responses, and parsing messages into strongly-typed objects. Read the documentation before implementing agent execution features.

Key classes:
- `ClaudeAgentClient` - Main client for spawning and managing CLI processes
- `ClaudeAgentOptions` - Configuration (working directory, permission mode, MCP servers)
- `IMessage` - Base interface for `SystemMessage`, `UserMessage`, `AssistantMessage`, `ResultMessage`
- `IContentBlock` - Base interface for `TextBlock`, `ToolUseBlock`, `ToolResultBlock`

Typical usage in `AgentRunnerService`:
```csharp
var client = new ClaudeAgentClient(new ClaudeAgentOptions { WorkingDirectory = repoPath });
await foreach (var message in client.QueryStreamAsync(new QueryRequest { Prompt = taskDescription }))
{
    // Process streaming messages (AssistantMessage, ToolUseBlock, etc.)
}
```

## Frontend Implementation Reference

For detailed Forge.Ui implementation documentation, see:

| Documentation          | Path                              | Content                                         |
|------------------------|-----------------------------------|-------------------------------------------------|
| UI Implementation      | `src/Forge.Ui/README.md`          | Component inventory, stores, services, patterns |
| Angular Best Practices | `src/Forge.Ui/CLAUDE.md`          | Coding standards and conventions                |
| API Integration        | `src/Forge.Ui/API-INTEGRATION.md` | Endpoints, SSE events, data models              |

**Quick Reference:**
- **7 feature components**: BoardComponent, TaskColumnComponent, TaskCardComponent, CreateTaskDialogComponent, TaskDetailComponent, AgentOutputComponent, NotificationPanelComponent
- **7 shared components**: StateBadge, PriorityBadge, AgentIndicator, ErrorAlert, LoadingSpinner, PausedBadge, SchedulerStatus
- **5 signal stores**: TaskStore, AgentStore, LogStore, NotificationStore, SchedulerStore
- **4 services**: TaskService, AgentService, SseService, SchedulerService (all with mock mode)

## Backend Patterns

### Minimal API Endpoint Pattern

Each feature defines endpoints as a static class with a `Map*` extension method:

```csharp
// Features/Tasks/TaskEndpoints.cs
public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks");

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
app.MapTaskEndpoints();
app.MapAgentEndpoints();
app.MapEventEndpoints();
```

### Feature Folder Structure

```
Features/
├── Tasks/
│   ├── TaskEndpoints.cs        # 11 endpoints (CRUD, transition, logs, abort, start-agent, pause, resume)
│   ├── TaskService.cs          # Business logic (scoped)
│   └── TaskModels.cs           # DTOs: TaskDto, CreateTaskDto, UpdateTaskDto, etc.
├── Agent/
│   ├── AgentEndpoints.cs       # GET /status
│   ├── AgentRunnerService.cs   # Claude Code process lifecycle (singleton)
│   └── AgentModels.cs          # AgentStatusDto
├── Scheduler/
│   ├── SchedulerEndpoints.cs   # 3 endpoints (status, enable, disable)
│   ├── SchedulerService.cs     # Task selection, completion handling, pause/resume (scoped)
│   ├── TaskSchedulerService.cs # Background service for automatic scheduling (hosted)
│   └── SchedulerModels.cs      # DTOs: SchedulerStatusDto, PauseTaskDto, AgentCompletionResult
├── Notifications/
│   ├── NotificationEndpoints.cs  # 4 endpoints (list, mark read, mark all read, unread count)
│   ├── NotificationService.cs    # Notification CRUD and task event helpers (scoped)
│   └── NotificationModels.cs     # DTOs: NotificationDto, CreateNotificationDto
└── Events/
    ├── EventEndpoints.cs       # GET /events (SSE)
    └── SseService.cs           # Channel-based event broadcasting (singleton)
```

### Service Organization

- **Scoped Services**: DbContext-dependent (TaskService)
- **Singleton Services**: Stateless or managing global state (SseService, AgentRunnerService)

### Integration Tests

Located in `src/Forge.Api/tests/Forge.Api.IntegrationTests/`. Uses `WebApplicationFactory` with SQLite in-memory database.

**Project Structure:**
```
Forge.Api.IntegrationTests/
├── Infrastructure/
│   ├── ForgeWebApplicationFactory.cs  # Test server with mocked services
│   └── ApiCollection.cs               # Shared fixture collection
├── Features/
│   └── Tasks/
│       ├── CreateTaskTests.cs         # Task creation tests
│       └── TransitionTaskTests.cs     # State transition tests
├── Helpers/
│   ├── HttpClientExtensions.cs        # JSON helpers with shared JsonOptions
│   ├── TestDatabaseHelper.cs          # Seed data utilities
│   └── Builders/                       # Test data builders
└── GlobalUsings.cs
```

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

### SSE Implementation

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

### SSE Event Architecture

**Event Types:**

| Event Type | Payload | Trigger |
|------------|---------|---------|
| `task:created` | `TaskDto` | Task creation |
| `task:updated` | `TaskDto` | Task modification, state transition, agent assignment |
| `task:deleted` | `{ id: Guid }` | Task deletion |
| `task:log` | `TaskLogDto` | Agent output during execution |
| `task:paused` | `TaskDto` | Task auto-paused after max retries or manual pause |
| `task:resumed` | `TaskDto` | Task resumed from paused state |
| `agent:statusChanged` | `AgentStatusDto` | Agent starts/stops |
| `scheduler:taskScheduled` | `TaskDto` | Scheduler picks next task |
| `notification:new` | `NotificationDto` | Notification created |

**Backend Emission Points:**

- **TaskService**: `task:created`, `task:updated`, `task:deleted`, `task:log`
- **AgentRunnerService**: `agent:statusChanged`, logs via TaskService
- **SchedulerService**: `task:paused`, `task:resumed`, auto-transitions
- **TaskSchedulerService**: `scheduler:taskScheduled`
- **NotificationService**: `notification:new`

**Frontend Consumption:**

- SseService connects to `/api/events`
- BoardComponent and TaskDetailComponent subscribe to events
- Signal stores (TaskStore, LogStore, NotificationStore) update from events

## Frontend Patterns

### Component Organization

Standalone components with lazy loading:

```typescript
// app.routes.ts
export const routes: Routes = [
  { path: '', loadComponent: () => import('./features/board/board.component').then(m => m.BoardComponent) },
  { path: 'tasks/:id', loadComponent: () => import('./features/task-detail/task-detail.component').then(m => m.TaskDetailComponent) },
];
```

### State Management with Signals

Use Angular Signals for reactive state:

```typescript
// core/stores/task.store.ts
@Injectable({ providedIn: 'root' })
export class TaskStore {
  private tasks = signal<Task[]>([]);

  readonly tasksByState = computed(() => {
    return this.tasks().reduce((acc, task) => {
      const state = task.state;
      if (!acc[state]) acc[state] = [];
      acc[state].push(task);
      return acc;
    }, {} as Record<string, Task[]>);
  });

  updateTask(updated: Task) {
    this.tasks.update(tasks =>
      tasks.map(t => t.id === updated.id ? updated : t)
    );
  }
}
```

### SSE Service

```typescript
// core/services/sse.service.ts
@Injectable({ providedIn: 'root' })
export class SseService {
  private eventSource: EventSource | null = null;

  connect(): Observable<ServerEvent> {
    return new Observable(observer => {
      this.eventSource = new EventSource('/api/events');

      this.eventSource.onmessage = (event) => {
        observer.next(JSON.parse(event.data));
      };

      return () => this.eventSource?.close();
    });
  }
}
```

### Angular 21 Conventions

- **Standalone Components**: All components standalone (no NgModules)
- **Signals**: Use signals for reactive state
- **Control Flow**: Use @if, @for, @switch (not *ngIf, *ngFor)
- **Zoneless**: Application runs in zoneless mode

### Frontend Testing

The frontend uses **Vitest 4.x** (NOT Jasmine, NOT Jest) for unit testing.

**Key Patterns:**
- Test files: `*.spec.ts` co-located with source files
- Use `vi.fn()` for mocks, NOT `jasmine.createSpy()`
- Use `vi.spyOn()` for spying, NOT `spyOn()`
- Import from `vitest` when explicit imports are needed
- Combine with Angular TestBed for component testing

**Example Component Test:**
```typescript
import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { MyComponent } from './my.component';
import { MyService } from '../../core/services/my.service';

describe('MyComponent', () => {
  let component: MyComponent;
  let fixture: ComponentFixture<MyComponent>;
  let serviceMock: { getData: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    serviceMock = {
      getData: vi.fn().mockReturnValue(of('result')),
    };

    await TestBed.configureTestingModule({
      imports: [MyComponent],
      providers: [{ provide: MyService, useValue: serviceMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(MyComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
```

**Vitest vs Jasmine Quick Reference:**

| DO NOT USE (Jasmine)       | USE THIS (Vitest)              |
|----------------------------|--------------------------------|
| `jasmine.createSpy()`      | `vi.fn()`                      |
| `spyOn(obj, 'method')`     | `vi.spyOn(obj, 'method')`      |
| `jasmine.createSpyObj()`   | Manual mock object with `vi.fn()` |
| No explicit imports needed | `import { vi } from 'vitest'` |

## Development Commands

### Backend

```bash
# Navigate to backend
cd src/Forge.Api/Forge.Api

# Restore and run
dotnet restore
dotnet run

# Run with watch
dotnet watch run


# Run integration tests (from solution directory)
cd src/Forge.Api
dotnet test tests/Forge.Api.IntegrationTests

# Database migrations (from Forge.Api project)
cd src/Forge.Api/Forge.Api
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Frontend

```bash
# Navigate to frontend
cd src/Forge.Ui

# Install dependencies
npm install

# Run development server
ng serve

# Build for production
ng build --configuration production

# Run tests
ng test
```

## API Endpoints

### Tasks
```
GET    /api/tasks                   # List all tasks
GET    /api/tasks/{id}              # Get task details
POST   /api/tasks                   # Create new task
PATCH  /api/tasks/{id}              # Update task
DELETE /api/tasks/{id}              # Delete task
POST   /api/tasks/{id}/transition   # Transition to new state
GET    /api/tasks/{id}/logs         # Get task logs
POST   /api/tasks/{id}/abort        # Abort assigned agent
POST   /api/tasks/{id}/start-agent  # Start agent execution for task
POST   /api/tasks/{id}/pause        # Pause task from automatic scheduling
POST   /api/tasks/{id}/resume       # Resume paused task
```

### Agent
```
GET    /api/agent/status          # Get current agent status
```

### Scheduler
```
GET    /api/scheduler/status      # Get scheduler status (enabled, agent running, pending/paused counts)
POST   /api/scheduler/enable      # Enable automatic task scheduling
POST   /api/scheduler/disable     # Disable automatic task scheduling
```

### Events
```
GET    /api/events                # EventSource/SSE connection
```

### Notifications
```
GET    /api/notifications              # Get recent notifications (?limit=N)
PATCH  /api/notifications/{id}/read    # Mark as read
POST   /api/notifications/mark-all-read # Mark all as read
GET    /api/notifications/unread-count  # Get unread count
```

## Data Models

### Task States
```
Backlog → Planning → Implementing → Reviewing → Testing → PrReady → Done
```

### Priority Levels
```
Low | Medium | High | Critical
```

## Important Notes

### Claude Code CLI Integration
- Backend spawns Claude Code as a child process
- Communication via stdin/stdout with `--print --output-format stream-json`
- Single agent at a time
- Agent output streamed to clients via SSE

### Real-time Updates
- Protocol: EventSource/SSE (not WebSocket)
- Payload: Full state on each event (not deltas)
- Event types: task:created, task:updated, task:deleted, task:log, task:paused, task:resumed, agent:statusChanged, scheduler:taskScheduled, notification:new

### Task Scheduling
The scheduler automatically picks the highest-priority ready task and starts the agent:
- **Schedulable States**: Planning, Implementing, Reviewing, Testing
- **Task Selection**: Priority DESC, then State (Planning first), then CreatedAt ASC
- **Auto-transition**: On successful agent completion, task moves to next state
- **Error Handling**: Tasks are auto-paused after 3 failed retry attempts
- **Human Intervention**: Pause/resume endpoints allow manual control

### Environment Variables
```env
DATABASE_PATH="forge.db"
CLAUDE_CODE_PATH="claude"
REPOSITORY_PATH="/path/to/your/repo"
ASPNETCORE_URLS="http://localhost:5000"
```
