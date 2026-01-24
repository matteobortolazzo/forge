# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Forge is an AI Agent Dashboard for orchestrating and monitoring AI coding agents powered by Claude Code CLI. The system implements a Kanban-style pipeline where tasks flow through stages (Backlog → Planning → Implementing → Reviewing → Testing → PR Ready → Done), with agents executed via stdin/stdout communication with the Claude Code process.

## Repository Structure

```
forge/
├── backend/                    # .NET 10 Minimal API
│   ├── Forge.Api/              # Main API entry point
│   │   ├── Features/           # Feature folders
│   │   │   ├── Tasks/          # Task CRUD, transitions, logs
│   │   │   ├── Agent/          # Agent status, runner service
│   │   │   └── Events/         # SSE endpoint
│   │   ├── Data/               # DbContext, migrations
│   │   ├── Services/           # Cross-cutting services
│   │   └── Program.cs          # Entry point
│   └── tests/                  # Unit and integration tests
└── frontend/                   # Angular 21 SPA
    └── src/app/
        ├── features/           # Feature folders
        │   ├── board/          # Kanban board components
        │   ├── task-detail/    # Task detail view with logs
        │   └── settings/       # Configuration (Phase 2)
        ├── core/               # Services, interceptors, guards
        ├── shared/             # Reusable components, models
        └── app.routes.ts       # Route configuration
```

**Code Organization**: Both API and UI use feature folder organization. Each feature contains all necessary files (endpoints, services, components, models).

## Tech Stack

| Component | Technology | Version |
|-----------|------------|---------|
| Frontend | Angular | 21.x |
| State Management | Angular Signals | Built-in |
| UI Components | Angular CDK | Latest |
| Styling | Tailwind CSS | 4.x |
| Backend | .NET | 10.x |
| Backend Framework | ASP.NET Core Minimal APIs | 10.x |
| Real-time | EventSource/SSE | Native |
| Database | SQLite (dev) / PostgreSQL (prod) | - |
| ORM | Entity Framework Core | 10.x |
| Agent Execution | Claude Code CLI | Latest |

## Documentation Sources (Context7)

When querying documentation via Context7 MCP, use these library IDs:

| Technology | Context7 Library ID |
|------------|---------------------|
| .NET / ASP.NET Core | `/websites/learn_microsoft_en-us_dotnet` |

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
│   ├── TaskEndpoints.cs        # Endpoint definitions
│   ├── TaskService.cs          # Business logic (scoped)
│   ├── TaskModels.cs           # DTOs and entities
│   └── TaskValidation.cs       # Validation logic
├── Agent/
│   ├── AgentEndpoints.cs
│   ├── AgentRunnerService.cs   # Claude Code process management (singleton)
│   └── AgentModels.cs
└── Events/
    ├── EventEndpoints.cs       # SSE endpoint
    └── SseService.cs           # Event broadcasting (singleton)
```

### Service Organization

- **Scoped Services**: DbContext-dependent (TaskService)
- **Singleton Services**: Stateless or managing global state (SseService, AgentRunnerService)

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

Use Angular Signals for reactive state (no NgRx for MVP simplicity):

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

## Development Commands

### Backend

```bash
# Navigate to backend
cd backend/Forge.Api

# Restore and run
dotnet restore
dotnet run

# Run with watch
dotnet watch run

# Run tests
dotnet test ../tests/Forge.Api.Tests

# Database migrations
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Frontend

```bash
# Navigate to frontend
cd frontend

# Install dependencies
npm install

# Run development server
ng serve

# Build for production
ng build --configuration production

# Run tests
ng test
```

## API Endpoints (MVP)

### Tasks
```
GET    /api/tasks                 # List all tasks
GET    /api/tasks/{id}            # Get task details
POST   /api/tasks                 # Create new task
PATCH  /api/tasks/{id}            # Update task
DELETE /api/tasks/{id}            # Delete task
POST   /api/tasks/{id}/transition # Transition to new state
GET    /api/tasks/{id}/logs       # Get task logs
POST   /api/tasks/{id}/abort      # Abort assigned agent
```

### Agent
```
GET    /api/agent/status          # Get current agent status
```

### Events
```
GET    /api/events                # EventSource/SSE connection
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
- MVP: Single agent at a time
- Agent output streamed to clients via SSE

### Real-time Updates
- Protocol: EventSource/SSE (not WebSocket)
- Payload: Full state on each event (not deltas)
- Event types: task:created, task:updated, task:deleted, task:log, agent:statusChanged

### MVP Scope
- Single agent execution
- Manual state transitions via buttons
- Basic CRUD for tasks
- Simple log viewer (no virtual scrolling)
- No drag-and-drop on Kanban board

### Environment Variables
```env
DATABASE_URL="Data Source=forge.db"
CLAUDE_CODE_PATH="claude"
REPOSITORY_PATH="/path/to/your/repo"
ASPNETCORE_URLS="http://localhost:5000"
```
