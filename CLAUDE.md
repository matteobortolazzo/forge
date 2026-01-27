# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Forge is an AI Agent Dashboard for orchestrating and monitoring AI coding agents powered by Claude Code CLI. The system implements a Kanban-style pipeline where tasks flow through stages (Backlog → Split → Research → Planning → Implementing → Simplifying → Verifying → Reviewing → PrReady → Done), with agents executed via stdin/stdout communication with the Claude Code process. The pipeline supports human-in-the-loop oversight through conditional and mandatory approval gates, confidence-based escalation, and git worktree isolation for subtasks.

## Repository Structure

```
forge/
├── agents/                         # Agent configuration (YAML)
│   ├── defaults/                   # Default agents for each pipeline state
│   │   ├── split.yml               # Split agent (task decomposition into subtasks)
│   │   ├── research.yml            # Research agent (codebase analysis)
│   │   ├── planning.yml            # Planning agent (test-first design)
│   │   ├── implementing.yml        # Implementation agent (code generation)
│   │   ├── simplifying.yml         # Simplifying agent (over-engineering review)
│   │   ├── verifying.yml           # Verifying agent (comprehensive verification)
│   │   ├── reviewing.yml           # Review agent (code review)
│   │   └── testing.yml             # Testing agent (legacy - replaced by verifying)
│   └── variants/                   # Framework-specific variants
│       ├── implementing.angular.yml
│       ├── implementing.dotnet.yml
│       └── reviewing.angular.yml
├── src/
│   ├── Forge.Api/                  # .NET 10 API Solution
│   │   ├── Forge.Api/              # Main API project
│   │   │   ├── Features/           # Task, Agent, Events, Scheduler endpoints
│   │   │   │   ├── Tasks/          # Task CRUD, transitions, logs, agent start, pause/resume, artifacts
│   │   │   │   ├── Agents/         # Agent orchestration, config loading, context detection
│   │   │   │   ├── Agent/          # Agent status, runner service
│   │   │   │   ├── Scheduler/      # Automatic task scheduling with human gates
│   │   │   │   ├── Subtasks/       # Subtask CRUD and lifecycle management
│   │   │   │   ├── HumanGates/     # Human gate management and resolution
│   │   │   │   ├── Rollback/       # Rollback procedures and audit records
│   │   │   │   ├── Worktree/       # Git worktree isolation for subtasks
│   │   │   │   ├── Events/         # SSE endpoint
│   │   │   │   └── Mock/           # Mock control endpoints (E2E only)
│   │   │   ├── Data/               # EF Core DbContext, Entities
│   │   │   ├── Shared/             # Enums, common types
│   │   │   └── Program.cs          # Entry point
│   │   ├── Claude.CodeSdk/         # C# SDK for Claude Code CLI
│   │   │   └── Mock/               # Mock client for E2E testing
│   │   └── tests/                  # Test projects
│   └── Forge.Ui/                   # Angular 21 SPA
│       ├── e2e/                    # Playwright E2E tests
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
| Tasks         | `TaskEndpoints.cs`, `TaskService.cs`, `TaskModels.cs`                       | 13 endpoints for CRUD, transitions, logs, agent control, pause/resume, split |
| Artifacts     | `TaskArtifactEndpoints.cs`, `TaskArtifactModels.cs`, `AgentArtifactEntity.cs` | 4 endpoints for structured agent output storage and retrieval |
| Subtasks      | `SubtaskEndpoints.cs`, `SubtaskService.cs`, `SubtaskModels.cs`              | 5 endpoints for subtask CRUD and lifecycle management  |
| Human Gates   | `HumanGateEndpoints.cs`, `HumanGateService.cs`, `HumanGateModels.cs`        | 4 endpoints for human gate management and resolution   |
| Agents        | `OrchestratorService.cs`, `AgentConfigLoader.cs`, `ContextDetector.cs`, `PromptBuilder.cs`, `ArtifactParser.cs` | Agent orchestration, YAML config loading, context detection |
| Agent         | `AgentEndpoints.cs`, `AgentRunnerService.cs`, `AgentModels.cs`              | Agent status, process lifecycle management             |
| Scheduler     | `SchedulerEndpoints.cs`, `SchedulerService.cs`, `TaskSchedulerService.cs`, `SchedulerModels.cs` | Automatic task scheduling with human gates and confidence thresholds |
| Worktree      | `WorktreeService.cs`                                                        | Git worktree isolation for subtask execution           |
| Rollback      | `RollbackService.cs`, `RollbackRecordEntity.cs`                             | Rollback procedures and audit records                  |
| Events        | `EventEndpoints.cs`, `SseService.cs`, `EventDtos.cs`                        | SSE event broadcasting via channels                    |
| Notifications | `NotificationEndpoints.cs`, `NotificationService.cs`, `NotificationModels.cs` | Notification CRUD, SSE events                          |
| Data          | `ForgeDbContext.cs`, `TaskEntity.cs`, `TaskLogEntity.cs`, `SubtaskEntity.cs`, `HumanGateEntity.cs`, `RollbackRecordEntity.cs` | EF Core with SQLite |
| Shared        | `Enums.cs`, `PipelineConfiguration.cs`                                      | Enums, pipeline configuration                          |

### Claude.CodeSdk (Fully Implemented)

| Component      | Files                                                                           | Description                     |
|----------------|---------------------------------------------------------------------------------|---------------------------------|
| Client         | `ClaudeAgentClient.cs`, `ClaudeAgentOptions.cs`                                 | Main client for CLI interaction |
| Messages       | `SystemMessage.cs`, `UserMessage.cs`, `AssistantMessage.cs`, `ResultMessage.cs` | Strongly-typed message models   |
| Content Blocks | `TextBlock.cs`, `ToolUseBlock.cs`, `ToolResultBlock.cs`                         | Content block types             |
| Exceptions     | `CliNotFoundException.cs`, `ProcessException.cs`, `JsonDecodeException.cs`      | Error handling                  |
| Internal       | `CliProcess.cs`, `MessageParser.cs`, `CommandBuilder.cs`, `CliLocator.cs`       | CLI process management          |
| Mock           | `MockScenario.cs`, `MockScenarioProvider.cs`, `MockClaudeAgentClient.cs`, `MockClaudeAgentClientFactory.cs` | Mock client for E2E testing |

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
| E2E Testing       | Playwright                       | 1.x      |

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
- **9 shared components**: StateBadge, PriorityBadge, AgentIndicator, ErrorAlert, LoadingSpinner, PausedBadge, SchedulerStatus, ArtifactTypeBadge, ArtifactPanel
- **6 signal stores**: TaskStore, AgentStore, LogStore, NotificationStore, SchedulerStore, ArtifactStore
- **5 services**: TaskService, AgentService, SseService, SchedulerService, ArtifactService (all with mock mode)

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
└── Mock/
    └── MockEndpoints.cs        # 5 endpoints (status, scenarios, set, remove, reset) - E2E only
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

### E2E Testing with Playwright

Located in `src/Forge.Ui/e2e/`. Uses Playwright for browser automation with a mock Claude CLI backend.

**Mock Infrastructure:**

The mock system replaces the real Claude Code CLI with a configurable mock client that simulates agent behavior:

- **MockClaudeAgentClient**: Simulates CLI responses with configurable delays and outputs
- **MockScenarioProvider**: Manages scenario selection based on task title patterns
- **MockEndpoints**: API endpoints for controlling mock behavior during tests

**Pre-built Scenarios:**

| Scenario | Behavior | Use Case |
|----------|----------|----------|
| `Default` | 3-second delay, success response | Standard testing |
| `QuickSuccess` | Instant success | Fast test execution |
| `Error` | Simulated failure | Error handling tests |
| `LongRunning` | 30-second delay | Timeout/abort tests |

**Environment Toggle:**

Set `CLAUDE_MOCK_MODE=true` to enable mock mode. The `e2e` launch profile configures this automatically.

**Mock Control API:**

During E2E tests, use the mock control endpoints to configure behavior:
```typescript
// Set scenario for specific task pattern
await fetch('/api/mock/scenario', {
  method: 'POST',
  body: JSON.stringify({ pattern: 'error-task', scenarioName: 'Error' })
});

// Reset to defaults
await fetch('/api/mock/reset', { method: 'POST' });
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
| `artifact:created` | `ArtifactDto` | Agent produces structured output |
| `humanGate:requested` | `HumanGateDto` | Human gate triggered (confidence < threshold) |
| `humanGate:resolved` | `HumanGateDto` | Human gate approved/rejected |
| `subtask:created` | `SubtaskDto` | Subtask created from split |
| `subtask:started` | `SubtaskDto` | Subtask execution started |
| `subtask:completed` | `SubtaskDto` | Subtask completed successfully |
| `subtask:failed` | `SubtaskDto` | Subtask execution failed |
| `rollback:initiated` | `RollbackDto` | Rollback procedure started |
| `rollback:completed` | `RollbackDto` | Rollback procedure finished |
| `agent:statusChanged` | `AgentStatusDto` | Agent starts/stops |
| `scheduler:taskScheduled` | `TaskDto` | Scheduler picks next task |
| `notification:new` | `NotificationDto` | Notification created |

**Backend Emission Points:**

- **TaskService**: `task:created`, `task:updated`, `task:deleted`, `task:log`
- **AgentRunnerService**: `agent:statusChanged`, logs via TaskService
- **OrchestratorService**: `artifact:created` (via SseService)
- **SchedulerService**: `task:paused`, `task:resumed`, `humanGate:requested`, auto-transitions
- **HumanGateService**: `humanGate:resolved`
- **SubtaskService**: `subtask:*` events
- **RollbackService**: `rollback:*` events
- **TaskSchedulerService**: `scheduler:taskScheduled`
- **NotificationService**: `notification:new`

**Frontend Consumption:**

- SseService connects to `/api/events`
- BoardComponent and TaskDetailComponent subscribe to events
- Signal stores (TaskStore, LogStore, NotificationStore) update from events

## Agent Pipeline Architecture

The agent pipeline uses state-specific agents with YAML configuration. Each pipeline state has a dedicated agent with specialized prompts and optional framework-specific variants. The pipeline supports human-in-the-loop oversight through confidence-based gates.

### Pipeline Flow

```
┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│   1. SPLIT   │───▶│  2. RESEARCH │───▶│   3. PLAN    │───▶│ 4. IMPLEMENT │
│   (± Human)  │    │              │    │   (± Human)  │    │  (retry loop)│
└──────────────┘    └──────────────┘    └──────────────┘    └──────────────┘
                                                                    │
┌──────────────┐    ┌──────────────┐    ┌──────────────┐            │
│   7. PR      │◀───│  6. VERIFY   │◀───│ 5. SIMPLIFY  │◀───────────┘
│   (Human)    │    │              │    │ (sep. agent) │
└──────────────┘    └──────────────┘    └──────────────┘
```

### How It Works

1. **Task enters schedulable state** → Scheduler picks highest-priority leaf task
2. **Human gate check** → If task has pending gate, wait for approval
3. **Orchestrator selects agent** → Matches task state and detects repository context
4. **Variant selection** → If a framework-specific variant exists (e.g., Angular), it's used
5. **Prompt assembly** → Template variables filled with task data, subtask context, and previous artifacts
6. **Agent execution** → Claude Code CLI runs in worktree (for subtasks) with assembled prompt
7. **Artifact parsing** → Extract confidence score, structured output, human input requests
8. **Human gate trigger** → If confidence < threshold, create gate and pause task
9. **State transition** → Task moves to next state based on agent recommendation
10. **Parent state update** → If subtask, parent's derived state is recomputed

### YAML Configuration Schema

Agent configurations are stored in `agents/defaults/` (required) and `agents/variants/` (optional).

```yaml
# agents/defaults/planning.yml
id: planning-default
name: Planning Agent
state: Planning
description: Breaks down tasks into actionable implementation steps

prompt: |
  You are a planning agent. Your goal is to analyze the task and create
  a detailed implementation plan.

  ## Task
  **Title:** {task.title}
  **Description:** {task.description}

  ## Previous Artifacts
  {artifacts}

  ## Output Format
  Provide your plan in structured markdown...

output:
  type: plan
  schema: |
    # Implementation Plan
    ## Summary
    ## Affected Files
    ## Implementation Steps

mcp_servers:
  - context7

max_turns: 30
```

### Variant Configuration

Variants extend default agents with framework-specific prompts and matching rules:

```yaml
# agents/variants/implementing.angular.yml
id: implementing-angular
name: Angular Implementation Agent
state: Implementing
extends: implementing-default
description: Implements features using Angular best practices

match:
  framework: angular        # Match by detected framework
  # OR
  language: typescript      # Match by detected language
  # OR
  files:                    # Match by file presence
    - angular.json
    - package.json

prompt: |
  You are an Angular implementation agent...
  [Angular-specific instructions]

mcp_servers:
  - angular-cli
  - primeng
```

### Template Variables

Available placeholders in prompts:

| Variable | Description |
|----------|-------------|
| `{task.title}` | Task title |
| `{task.description}` | Task description |
| `{task.state}` | Current pipeline state |
| `{task.priority}` | Task priority |
| `{task.acceptanceCriteria}` | Task acceptance criteria |
| `{subtask.title}` | Current subtask title |
| `{subtask.description}` | Current subtask description |
| `{subtask.acceptanceCriteria}` | Subtask acceptance criteria |
| `{context.language}` | Detected repository language |
| `{context.framework}` | Detected framework |
| `{context.repoPath}` | Repository path |
| `{artifacts}` | Formatted list of previous artifacts |
| `{artifacts.split}` | Most recent task split artifact content |
| `{artifacts.research}` | Most recent research findings content |
| `{artifacts.plan}` | Most recent plan artifact content |
| `{artifacts.implementation}` | Most recent implementation artifact content |
| `{artifacts.simplification}` | Most recent simplification review content |
| `{artifacts.verification}` | Most recent verification report content |
| `{artifacts.review}` | Most recent review artifact content |

### Artifact Types

| Type | Produced By | Contains |
|------|-------------|----------|
| `task_split` | Split agent | Subtask decomposition, execution order, dependencies |
| `research_findings` | Research agent | Existing code, patterns, affected files, external references |
| `plan` | Planning agent | Test specifications, implementation steps, verification commands |
| `implementation` | Implementing agent | Files changed, test results, verification log |
| `simplification_review` | Simplifying agent | Verdict, findings, scope assessment |
| `verification_report` | Verifying agent | Full test suite results, build status, regression check |
| `review` | Reviewing agent | Review findings, suggested changes, approval status |
| `test` | Testing agent (legacy) | Test results, coverage report |
| `general` | Any agent | Unstructured output |

### Context Detection

The `ContextDetector` service automatically identifies:

- **Language**: Analyzes file extensions in repository (e.g., `.ts` → `typescript`, `.cs` → `csharp`)
- **Framework**: Checks for framework markers (`angular.json` → `angular`, `*.csproj` → `dotnet`)

Detection results are cached on the task entity and used for variant selection.

### Creating New Agents

1. **Default agent**: Create `agents/defaults/{state}.yml` with required fields
2. **Variant**: Create `agents/variants/{state}.{framework}.yml` with `extends` and `match` fields
3. **Register MCP servers**: Add server names to `mcp_servers` array if needed
4. **Restart API**: Configurations are loaded at startup

### Key Classes

| Class | Purpose |
|-------|---------|
| `IOrchestratorService` | Agent selection, prompt assembly, artifact management, confidence tracking |
| `IAgentConfigLoader` | Loads and caches YAML configurations |
| `IContextDetector` | Repository language/framework detection |
| `IPromptBuilder` | Template variable substitution with subtask and artifact context |
| `IArtifactParser` | Extracts structured content, confidence scores, human input requests, verdicts |
| `IWorktreeService` | Git worktree creation/removal for subtask isolation |
| `IRollbackService` | Subtask and task rollback with artifact preservation |
| `HumanGateService` | Human gate CRUD and resolution |
| `SubtaskService` | Subtask lifecycle management |
| `AgentConfig` | YAML configuration model |
| `ResolvedAgentConfig` | Fully resolved config with assembled prompt |
| `PipelineConfiguration` | Retry limits, confidence thresholds, gate configuration |

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

### E2E Testing

```bash
# Start backend in mock mode (from Forge.Api project)
cd src/Forge.Api/Forge.Api
dotnet run --launch-profile e2e

# Run Playwright tests (from Forge.Ui)
cd src/Forge.Ui
npm run e2e

# Run with interactive UI
npm run e2e:ui

# Run with visible browser
npm run e2e:headed
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

### Task Artifacts
```
GET    /api/tasks/{id}/artifacts              # List all artifacts for task
GET    /api/tasks/{id}/artifacts/{aid}        # Get specific artifact by ID
GET    /api/tasks/{id}/artifacts/latest       # Get most recent artifact
GET    /api/tasks/{id}/artifacts/by-state/{state}  # Filter artifacts by pipeline state
```

### Subtasks
```
GET    /api/tasks/{id}/subtasks               # List all subtasks for a task
GET    /api/tasks/{id}/subtasks/{sid}         # Get specific subtask
POST   /api/tasks/{id}/subtasks               # Create subtask
PATCH  /api/tasks/{id}/subtasks/{sid}/status  # Update subtask status
DELETE /api/tasks/{id}/subtasks/{sid}         # Delete subtask
```

### Human Gates
```
GET    /api/gates/pending                     # Get all pending human gates
GET    /api/gates/{id}                        # Get specific gate
POST   /api/gates/{id}/resolve                # Resolve gate (approve/reject)
GET    /api/tasks/{id}/gates                  # Get all gates for a task
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

### Mock (E2E Only - when CLAUDE_MOCK_MODE=true)
```
GET    /api/mock/status              # Get mock configuration status
GET    /api/mock/scenarios           # List available scenarios
POST   /api/mock/scenario            # Set default or pattern-specific scenario
DELETE /api/mock/scenario/{pattern}  # Remove pattern mapping
POST   /api/mock/reset               # Reset to defaults
```

## Data Models

### Task States
```
Backlog → Split → Research → Planning → Implementing → Simplifying → Verifying → Reviewing → PrReady → Done
```

**State Descriptions:**
| State | Description | Agent |
|-------|-------------|-------|
| Backlog | Waiting to be started | None |
| Split | Task decomposition into subtasks | Split agent |
| Research | Codebase analysis and pattern discovery | Research agent |
| Planning | Test-first implementation design | Planning agent |
| Implementing | Code generation (tests first, then code) | Implementing agent |
| Simplifying | Over-engineering review (YAGNI check) | Simplifying agent |
| Verifying | Comprehensive verification and regression testing | Verifying agent |
| Reviewing | Human code review | Review agent |
| PrReady | Ready for PR creation | None |
| Done | Completed | None |

### Priority Levels
```
Low | Medium | High | Critical
```

### Artifact Types
```
task_split | research_findings | plan | implementation | simplification_review | verification_report | review | test | general
```

### Human Gate Types
```
split (conditional) | planning (conditional) | pr (mandatory)
```

### Subtask Status
```
pending | in_progress | completed | failed | skipped
```

### TaskEntity Fields (Agent Context)
```csharp
// Auto-detected or user-specified context
public string? DetectedLanguage { get; set; }    // e.g., "csharp", "typescript"
public string? DetectedFramework { get; set; }   // e.g., "angular", "dotnet"
public PipelineState? RecommendedNextState { get; set; }  // Agent's recommendation

// Confidence and human gates
public decimal? ConfidenceScore { get; set; }    // Agent-reported confidence (0.0-1.0)
public bool HumanInputRequested { get; set; }    // Agent explicitly requested human input
public string? HumanInputReason { get; set; }    // Reason for human input request
public bool HasPendingGate { get; set; }         // Task blocked by pending human gate

// Simplification loop
public int SimplificationIterations { get; set; }  // Number of simplification loops

// Task hierarchy
public Guid? ParentId { get; set; }              // Parent task (for subtasks)
public int ChildCount { get; set; }              // Number of child subtasks
public PipelineState? DerivedState { get; set; } // Computed from children's states
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
- Event types: task:created, task:updated, task:deleted, task:log, task:paused, task:resumed, artifact:created, humanGate:requested, humanGate:resolved, subtask:created, subtask:started, subtask:completed, subtask:failed, rollback:initiated, rollback:completed, agent:statusChanged, scheduler:taskScheduled, notification:new

### Task Scheduling
The scheduler automatically picks the highest-priority ready task and starts the agent:
- **Schedulable States**: Split, Research, Planning, Implementing, Simplifying, Verifying, Reviewing
- **Task Selection**: Priority DESC, then State (Split first), then CreatedAt ASC
- **Leaf Tasks Only**: Only tasks without children are scheduled (parent state is derived)
- **Human Gate Check**: Tasks with `HasPendingGate = true` are not scheduled until gate is resolved
- **Auto-transition**: On successful agent completion, task moves to next state
- **Error Handling**: Tasks are auto-paused after max retries (default: 3)
- **Simplification Loop**: If simplifying agent requests changes, loops back to Implementing (max 2 iterations)
- **Confidence Threshold**: Agent confidence < 0.7 triggers human gate
- **Human Intervention**: Pause/resume endpoints allow manual control

### Human Gates
Human approval gates can be triggered at key pipeline stages:

| Gate Type | Trigger | Behavior |
|-----------|---------|----------|
| Split | Confidence < threshold OR mandatory config | Approval required before Research |
| Planning | Confidence < threshold OR high-risk OR mandatory config | Approval required before Implementing |
| PR | Always mandatory | Approval required before merge |

**Configuration** (`appsettings.json`):
```json
{
  "Pipeline": {
    "MaxImplementationRetries": 3,
    "MaxSimplificationIterations": 2,
    "ConfidenceThreshold": 0.7,
    "WorktreeIsolation": true,
    "SequentialSubtasks": true,
    "HumanGates": {
      "Split": "conditional",
      "Planning": "conditional",
      "Pr": "mandatory"
    }
  }
}
```

### Environment Variables
```env
DATABASE_PATH="forge.db"
CLAUDE_CODE_PATH="claude"
REPOSITORY_PATH="/path/to/your/repo"
ASPNETCORE_URLS="http://localhost:5000"
CLAUDE_MOCK_MODE="true"         # Enable mock Claude client for E2E testing
AGENTS_PATH="./agents"          # Optional: custom path to agents directory (default: ./agents)
```
