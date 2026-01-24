# AI Agent Dashboard - Technical Specification

## Project Overview

A web-based dashboard for orchestrating and monitoring AI coding agents powered by Claude Code CLI. The system implements a Kanban-style pipeline where tasks flow through stages, with agents executed via stdin/stdout communication with the Claude Code process.

## Goals

### MVP Goals
1. Visual Kanban board to manage coding tasks through pipeline stages
2. Run one Claude Code agent at a time via stdin/stdout
3. Real-time visibility into agent activity and logs
4. Manual state transitions for task progression

### Future Goals
5. Budget tracking and cost management
6. Concurrent agent execution with queue management
7. Automatic delegation to specialized agents
8. Repository-specific knowledge base (CLAUDE.md)

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Angular 21 Frontend                              │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐      │
│  │ Backlog  │ │ Planning │ │Implement │ │  Review  │ │ PR Ready │      │
│  │          │→│          │→│          │→│          │→│          │      │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘      │
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                    Agent Activity Monitor                        │    │
│  │  [agent: implementing task #12]                                  │    │
│  └─────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────┘
                                   │
                            EventSource/SSE
                                   │
┌─────────────────────────────────────────────────────────────────────────┐
│                          .NET 10 Backend                                 │
│                                                                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                      │
│  │   Tasks     │  │   Agent     │  │     SSE     │                      │
│  │   API       │  │   Runner    │  │  Endpoint   │                      │
│  └─────────────┘  └─────────────┘  └─────────────┘                      │
│         │                │                                               │
│         └────────────────┼───────────────────────────────────────┐      │
│                          ▼                                       │      │
│  ┌───────────────────────────────────────────────────────────┐   │      │
│  │                  Agent Runner Service                      │   │      │
│  │  - Spawns Claude Code CLI as child process                │   │      │
│  │  - Communicates via stdin/stdout                          │   │      │
│  │  - One agent at a time (MVP)                              │   │      │
│  │  - Streams output to SSE clients                          │   │      │
│  └───────────────────────────────────────────────────────────┘   │      │
│                          │                                       │      │
│                    stdin/stdout                                  │      │
│                          ▼                                       │      │
│  ┌───────────────────────────────────────────────────────────┐   │      │
│  │                   Claude Code CLI                          │   │      │
│  │  - Executes prompts with tool access                      │   │      │
│  │  - Works in repository directory                          │   │      │
│  └───────────────────────────────────────────────────────────┘   │      │
│                                                                   │      │
└───────────────────────────────────────────────────────────────────┼──────┘
                                                                    │
                                                                    ▼
                                                            ┌──────────────┐
                                                            │  Repository  │
                                                            │  (Git)       │
                                                            │ + CLAUDE.md  │
                                                            └──────────────┘
```

---

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

---

## Data Models

### Task (MVP)

```csharp
public class Task
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PipelineState State { get; set; } = PipelineState.Backlog;
    public Priority Priority { get; set; } = Priority.Medium;

    // Agent tracking
    public string? AssignedAgentId { get; set; }

    // Error tracking
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum PipelineState
{
    Backlog,
    Planning,
    Implementing,
    Reviewing,
    Testing,
    PrReady,
    Done
}

public enum Priority
{
    Low,
    Medium,
    High,
    Critical
}
```

### Task (Phase 2 - Extended)

```csharp
public class Task
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;

    // Structured fields (Phase 2)
    public string Description { get; set; } = string.Empty;
    public List<string> AcceptanceCriteria { get; set; } = new();
    public List<string> EdgeCases { get; set; } = new();
    public List<string> TechnicalConstraints { get; set; } = new();
    public List<string> RelatedDocLinks { get; set; } = new();
    public string? ExpectedOutcome { get; set; }

    public PipelineState State { get; set; } = PipelineState.Backlog;
    public Priority Priority { get; set; } = Priority.Medium;

    // Relationships (Phase 2)
    public Guid? ParentTaskId { get; set; }
    public List<Guid> ChildTaskIds { get; set; } = new();
    public List<Guid> BlockedBy { get; set; } = new();

    // Agent tracking
    public string? AssignedAgentId { get; set; }
    public AgentType? AgentType { get; set; }

    // Git integration (Phase 2)
    public string? Branch { get; set; }
    public string? PrUrl { get; set; }

    // Budget tracking (Phase 2)
    public int TokensUsed { get; set; }

    // Error tracking
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
```

### TaskLog

```csharp
public class TaskLog
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string AgentId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public LogType Type { get; set; }
    public string Content { get; set; } = string.Empty;
}

public enum LogType
{
    Info,
    ToolUse,
    ToolResult,
    Error,
    Thinking
}
```

### Agent (Phase 2)

```csharp
public class AgentInstance
{
    public string Id { get; set; } = string.Empty;
    public AgentType Type { get; set; }
    public AgentStatus Status { get; set; }

    // Current work
    public Guid? TaskId { get; set; }
    public string? CurrentAction { get; set; }
    public DateTime? StartedAt { get; set; }

    // Stats
    public int TotalTasksCompleted { get; set; }
    public int TotalTokensUsed { get; set; }
    public int TokensThisTask { get; set; }
}

public enum AgentType
{
    Orchestrator,
    UiAgent,
    ApiAgent,
    Simplifier,
    CodeReviewer,
    TestAgent,
    PrAgent,
    CostEstimator
}

public enum AgentStatus
{
    Idle,
    Running,
    Paused,
    Error
}
```

### Configuration (Phase 2)

```csharp
public class DashboardConfig
{
    // Concurrency
    public int MaxConcurrentAgents { get; set; } = 3;

    // Budget limits
    public int DailyTokenBudget { get; set; } = 100000;
    public int MonthlyTokenBudget { get; set; } = 5000000;
    public int CurrentDailyUsage { get; set; }
    public int CurrentMonthlyUsage { get; set; }
    public bool BudgetExceeded { get; set; }

    // Repository settings
    public string RepositoryPath { get; set; } = string.Empty;
    public string DefaultBranch { get; set; } = "main";
    public string BranchPrefix { get; set; } = "feature/agent-";

    // Protected paths
    public List<string> ProtectedPaths { get; set; } = new() { ".git/", ".env", "node_modules/" };

    // Automation settings
    public bool AutoAdvance { get; set; } = true;
    public List<PipelineState> RequireHumanApproval { get; set; } = new();

    // Pattern learning
    public string PatternsFile { get; set; } = "CLAUDE.md";
}
```

### Notification (Phase 2)

```csharp
public class Notification
{
    public Guid Id { get; set; }
    public NotificationType Type { get; set; }
    public Guid? TaskId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Read { get; set; }
}

public enum NotificationType
{
    TaskStateChange,
    PrCreated,
    PrMerged,
    BudgetWarning,
    BudgetExceeded
}
```

---

## Agent Execution

### Claude Code CLI Integration

The backend spawns Claude Code as a child process and communicates via stdin/stdout. This approach:
- Leverages Claude Code's existing tool implementations
- Provides consistent behavior with CLI usage
- Simplifies the backend to process orchestration

### MVP: Single Agent Model

For MVP, only one agent runs at a time. The agent receives a prompt based on the task and executes it via Claude Code CLI.

```csharp
public class AgentRunnerService
{
    private Process? _activeProcess;
    private readonly string _claudeCodePath;
    private readonly string _repositoryPath;
    private readonly ILogger<AgentRunnerService> _logger;

    public AgentRunnerService(
        IConfiguration configuration,
        ILogger<AgentRunnerService> logger)
    {
        _claudeCodePath = configuration["ClaudeCode:Path"] ?? "claude";
        _repositoryPath = configuration["Repository:Path"] ?? ".";
        _logger = logger;
    }

    public async Task<AgentResult> RunAgentAsync(
        AgentTask task,
        Action<string> onOutput,
        CancellationToken cancellationToken)
    {
        if (_activeProcess != null)
            throw new InvalidOperationException("Another agent is already running");

        var prompt = BuildPromptForTask(task);

        _activeProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _claudeCodePath,
                Arguments = "--print --output-format stream-json",
                WorkingDirectory = _repositoryPath,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        _activeProcess.Start();

        // Send prompt via stdin
        await _activeProcess.StandardInput.WriteLineAsync(prompt);
        _activeProcess.StandardInput.Close();

        // Stream output
        while (!_activeProcess.StandardOutput.EndOfStream)
        {
            var line = await _activeProcess.StandardOutput.ReadLineAsync(cancellationToken);
            if (line != null)
            {
                onOutput(line);
            }
        }

        await _activeProcess.WaitForExitAsync(cancellationToken);

        var result = new AgentResult
        {
            ExitCode = _activeProcess.ExitCode,
            Success = _activeProcess.ExitCode == 0
        };

        _activeProcess = null;
        return result;
    }

    public void AbortAgent()
    {
        _activeProcess?.Kill();
        _activeProcess = null;
    }

    private string BuildPromptForTask(AgentTask task)
    {
        return $"""
            You are working on the following task:

            Title: {task.Title}
            Description: {task.Description}

            Please implement this task. When done, commit your changes with a clear message.
            """;
    }
}

public class AgentTask
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class AgentResult
{
    public int ExitCode { get; set; }
    public bool Success { get; set; }
}
```

### Future: Specialized Agents (Phase 2)

In future phases, the system will support specialized agent prompts:
- **Orchestrator**: Plans and breaks down complex tasks (Opus model)
- **UI Agent**: Angular/frontend specialist (Sonnet model)
- **API Agent**: Backend specialist (Sonnet model)
- **Reviewer**: Code review specialist (Sonnet model)
- **Simplifier**: Formatting and linting (Haiku model)

---

## API Specification

### REST Endpoints (MVP)

#### Tasks

```
GET    /api/tasks                 # List all tasks
GET    /api/tasks/{id}            # Get task details
POST   /api/tasks                 # Create new task
PATCH  /api/tasks/{id}            # Update task
DELETE /api/tasks/{id}            # Delete task (only if no active agent)
POST   /api/tasks/{id}/transition # Transition to new state
GET    /api/tasks/{id}/logs       # Get task logs
POST   /api/tasks/{id}/abort      # Abort assigned agent
```

#### Agent

```
GET    /api/agent/status          # Get current agent status
```

### REST Endpoints (Phase 2)

#### Agents

```
GET    /api/agents                # List agent instances and status
GET    /api/agents/{id}           # Get agent details
POST   /api/agents/pause-all      # Pause entire pipeline
POST   /api/agents/resume-all     # Resume entire pipeline
POST   /api/tasks/{id}/pause      # Pause specific task and subtasks
POST   /api/tasks/{id}/resume     # Resume specific task
```

#### Configuration

```
GET    /api/config                # Get current configuration
PATCH  /api/config                # Update configuration (requires restart)
```

#### Patterns

```
POST   /api/patterns/update       # Manual trigger to update CLAUDE.md from recent PRs
```

#### Notifications

```
GET    /api/notifications         # Get recent notifications
PATCH  /api/notifications/{id}/read # Mark as read
```

### Server-Sent Events (SSE)

#### Connection

```
GET    /api/events                # EventSource connection
```

#### Event Types (MVP)

```csharp
public interface IServerEvents
{
    // Task updates
    void TaskUpdated(Task task);
    void TaskCreated(Task task);
    void TaskDeleted(Guid taskId);

    // Log streaming
    void TaskLog(TaskLog log);

    // Agent updates
    void AgentStatusChanged(string agentId, string status, string? currentAction);
}
```

#### Event Types (Phase 2)

```csharp
public interface IServerEventsPhase2
{
    // Task updates
    void TaskUpdated(Task task);
    void TaskCreated(Task task);
    void TaskDeleted(Guid taskId);

    // Log streaming
    void TaskLog(TaskLog log);

    // Agent updates
    void AgentStatusChanged(AgentInstance agent);
    void AgentAssigned(string agentId, Guid taskId);
    void AgentCompleted(string agentId, Guid taskId, object result);
    void AgentError(string agentId, Guid taskId, string error);

    // Pool status
    void PoolStatus(PoolStatusDto status);

    // Budget updates
    void BudgetUpdated(BudgetStatusDto status);

    // Notifications
    void NotificationNew(Notification notification);
}
```

---

## Frontend Components

### Page Structure (MVP)

```
/                           → Dashboard (Kanban board)
/tasks/{id}                 → Task detail view with logs and transition buttons
```

### Page Structure (Phase 2)

```
/                           → Dashboard (Kanban board + budget widget + notifications)
/tasks/{id}                 → Task detail view with logs and transition buttons
/settings                   → Configuration page (changes require restart)
```

### Core Components (MVP)

#### TaskBoardComponent

Main Kanban board. Fixed 7-state pipeline. No drag-and-drop.

```typescript
@Component({
  selector: 'app-task-board',
  standalone: true,
  imports: [TaskColumnComponent],
  template: `
    <div class="dashboard-layout">
      <!-- Kanban Board -->
      <div class="flex gap-4 p-4 h-full overflow-x-auto">
        @for (column of columns; track column.state) {
          <app-task-column
            [state]="column.state"
            [title]="column.title"
            [tasks]="tasksByState()[column.state] ?? []"
            (taskClicked)="navigateToDetail($event)"
          />
        }
      </div>
    </div>
  `
})
export class TaskBoardComponent {
  private taskStore = inject(TaskStore);
  private router = inject(Router);

  columns = [
    { state: 'backlog', title: 'Backlog' },
    { state: 'planning', title: 'Planning' },
    { state: 'implementing', title: 'Implementing' },
    { state: 'reviewing', title: 'Reviewing' },
    { state: 'testing', title: 'Testing' },
    { state: 'prReady', title: 'PR Ready' },
    { state: 'done', title: 'Done' }
  ];

  tasksByState = this.taskStore.tasksByState;

  navigateToDetail(taskId: string) {
    this.router.navigate(['/tasks', taskId]);
  }
}
```

#### TaskColumnComponent

Column of task cards for a given state.

```typescript
@Component({
  selector: 'app-task-column',
  standalone: true,
  imports: [TaskCardComponent],
  template: `
    <div class="w-72 flex-shrink-0 bg-gray-100 rounded-lg p-2">
      <h3 class="font-semibold text-gray-700 mb-2 px-2">
        {{ title() }}
        <span class="text-gray-400 text-sm">({{ tasks().length }})</span>
      </h3>
      <div class="space-y-2">
        @for (task of tasks(); track task.id) {
          <app-task-card
            [task]="task"
            (click)="taskClicked.emit(task.id)"
          />
        }
      </div>
    </div>
  `
})
export class TaskColumnComponent {
  state = input.required<string>();
  title = input.required<string>();
  tasks = input.required<Task[]>();
  taskClicked = output<string>();
}
```

#### TaskCardComponent

Individual task card with error indication.

```typescript
@Component({
  selector: 'app-task-card',
  standalone: true,
  template: `
    <div class="bg-white rounded-lg shadow p-3 cursor-pointer hover:shadow-md transition-shadow"
         [class.border-l-4]="task().assignedAgentId"
         [class.border-blue-500]="task().assignedAgentId && !task().hasError"
         [class.border-red-500]="task().hasError"
         [class.bg-red-50]="task().hasError">

      @if (task().hasError) {
        <div class="flex items-center gap-1 text-red-600 text-xs mb-2">
          <span>Error</span>
        </div>
      }

      <h4 class="font-medium text-sm">{{ task().title }}</h4>
      <p class="text-xs text-gray-500 mt-1 line-clamp-2">{{ task().description }}</p>

      @if (task().assignedAgentId) {
        <div class="mt-2 flex items-center gap-1 text-xs">
          <span class="w-2 h-2 rounded-full bg-green-500 animate-pulse"></span>
          <span>Agent running</span>
        </div>
      }

      <div class="mt-2">
        <span class="text-xs px-2 py-0.5 rounded-full"
              [class]="priorityClass()">
          {{ task().priority }}
        </span>
      </div>
    </div>
  `
})
export class TaskCardComponent {
  task = input.required<Task>();

  priorityClass = computed(() => {
    const classes: Record<string, string> = {
      critical: 'bg-red-100 text-red-800',
      high: 'bg-orange-100 text-orange-800',
      medium: 'bg-yellow-100 text-yellow-800',
      low: 'bg-gray-100 text-gray-800'
    };
    return classes[this.task().priority];
  });
}
```

#### TaskDetailComponent

Full task view with logs and transition buttons.

```typescript
@Component({
  selector: 'app-task-detail',
  standalone: true,
  imports: [AgentOutputComponent],
  template: `
    <div class="h-full flex flex-col">
      <!-- Header -->
      <header class="p-4 border-b">
        <h1 class="text-xl font-bold">{{ task()?.title }}</h1>
        <div class="flex gap-2 mt-2">
          <span class="badge">{{ task()?.state }}</span>
          <span class="badge">{{ task()?.priority }}</span>
          @if (task()?.hasError) {
            <span class="badge bg-red-100 text-red-800">Error</span>
          }
        </div>
      </header>

      <!-- Content -->
      <div class="flex-1 flex overflow-hidden">
        <!-- Details panel -->
        <div class="w-1/2 p-4 overflow-y-auto border-r">
          <h2 class="font-semibold mb-2">Description</h2>
          <p class="text-gray-600">{{ task()?.description }}</p>

          @if (task()?.errorMessage) {
            <h2 class="font-semibold mt-4 mb-2 text-red-600">Error</h2>
            <p class="text-red-600">{{ task()?.errorMessage }}</p>
          }
        </div>

        <!-- Logs panel -->
        <app-agent-output
          [taskId]="taskId()"
          class="w-1/2"
        />
      </div>

      <!-- Actions -->
      <footer class="p-4 border-t flex gap-2">
        @if (canAdvance()) {
          <button (click)="advanceState()" class="btn-primary">
            {{ nextStateLabel() }}
          </button>
        }
        @if (canRevert()) {
          <button (click)="revertState()" class="btn-secondary">
            {{ previousStateLabel() }}
          </button>
        }
        @if (task()?.assignedAgentId) {
          <button (click)="abortAgent()" class="btn-danger">
            Abort Agent
          </button>
        }
      </footer>
    </div>
  `
})
export class TaskDetailComponent {
  taskId = input.required<string>();
  // ... implementation
}
```

#### AgentOutputComponent

Simple streaming log viewer (no virtual scrolling, no filters for MVP).

```typescript
@Component({
  selector: 'app-agent-output',
  standalone: true,
  template: `
    <div class="flex flex-col h-full bg-gray-900 text-white p-4">
      <h2 class="font-semibold text-gray-300 mb-2">Agent Output</h2>

      <div class="flex-1 overflow-y-auto font-mono text-xs space-y-1">
        @for (log of logs(); track log.id) {
          <div [class]="logClass(log.type)">
            <span class="text-gray-500">{{ log.timestamp | date:'HH:mm:ss' }}</span>
            <span class="ml-2">{{ log.content }}</span>
          </div>
        }

        @if (logs().length === 0) {
          <div class="text-gray-500">No logs yet</div>
        }
      </div>
    </div>
  `
})
export class AgentOutputComponent {
  taskId = input.required<string>();

  private logStore = inject(LogStore);
  logs = computed(() => this.logStore.getLogsForTask(this.taskId()));

  logClass(type: string): string {
    const classes: Record<string, string> = {
      info: 'text-gray-300',
      tool_use: 'text-blue-400',
      tool_result: 'text-green-400',
      error: 'text-red-400',
      thinking: 'text-yellow-400'
    };
    return classes[type] ?? 'text-gray-300';
  }
}
```

### Phase 2 Components

#### BudgetWidgetComponent

Dashboard widget showing token usage.

```typescript
@Component({
  selector: 'app-budget-widget',
  standalone: true,
  template: `
    <div class="budget-widget p-4 bg-white rounded-lg shadow">
      <h3 class="font-semibold mb-2">Budget</h3>

      <div class="space-y-2">
        <div>
          <div class="flex justify-between text-sm">
            <span>Daily</span>
            <span>{{ dailyUsage() | number }}/{{ dailyLimit() | number }}</span>
          </div>
          <div class="w-full bg-gray-200 rounded-full h-2">
            <div class="bg-blue-500 h-2 rounded-full transition-all"
                 [style.width.%]="dailyPercentage()"
                 [class.bg-red-500]="dailyPercentage() >= 80">
            </div>
          </div>
        </div>

        <div>
          <div class="flex justify-between text-sm">
            <span>Monthly</span>
            <span>{{ monthlyUsage() | number }}/{{ monthlyLimit() | number }}</span>
          </div>
          <div class="w-full bg-gray-200 rounded-full h-2">
            <div class="bg-green-500 h-2 rounded-full transition-all"
                 [style.width.%]="monthlyPercentage()"
                 [class.bg-red-500]="monthlyPercentage() >= 80">
            </div>
          </div>
        </div>
      </div>

      @if (exceeded()) {
        <div class="mt-2 text-xs text-red-600 font-medium">
          Budget exceeded - agents paused
        </div>
      }
    </div>
  `
})
export class BudgetWidgetComponent {
  private budgetStore = inject(BudgetStore);

  dailyUsage = this.budgetStore.dailyUsage;
  dailyLimit = this.budgetStore.dailyLimit;
  monthlyUsage = this.budgetStore.monthlyUsage;
  monthlyLimit = this.budgetStore.monthlyLimit;
  exceeded = this.budgetStore.exceeded;

  dailyPercentage = computed(() => (this.dailyUsage() / this.dailyLimit()) * 100);
  monthlyPercentage = computed(() => (this.monthlyUsage() / this.monthlyLimit()) * 100);
}
```

#### NotificationsPanelComponent

Shows recent notifications.

```typescript
@Component({
  selector: 'app-notifications-panel',
  standalone: true,
  template: `
    <div class="notifications-panel p-4 bg-white rounded-lg shadow">
      <h3 class="font-semibold mb-2">Notifications</h3>

      <div class="space-y-1 max-h-32 overflow-y-auto">
        @for (notification of recentNotifications(); track notification.id) {
          <div class="text-sm flex items-center gap-2 p-1 hover:bg-gray-50 rounded"
               [class.font-semibold]="!notification.read"
               (click)="markAsRead(notification.id)">
            <span class="text-xs text-gray-500">
              {{ notification.timestamp | date:'short' }}
            </span>
            <span>{{ notification.message }}</span>
          </div>
        }

        @if (recentNotifications().length === 0) {
          <div class="text-sm text-gray-400">No recent notifications</div>
        }
      </div>
    </div>
  `
})
export class NotificationsPanelComponent {
  private notificationStore = inject(NotificationStore);

  recentNotifications = this.notificationStore.recent;

  markAsRead(id: string) {
    this.notificationStore.markAsRead(id);
  }
}
```

#### TaskLogsComponent (Phase 2)

Virtual scrolling log viewer with search and filters.

```typescript
@Component({
  selector: 'app-task-logs',
  standalone: true,
  imports: [CdkVirtualScrollViewport, CdkVirtualForOf],
  template: `
    <div class="flex flex-col h-full bg-gray-900 text-white p-4">
      <div class="mb-2 space-y-2">
        <h2 class="font-semibold text-gray-300">Agent Logs</h2>

        <!-- Search -->
        <input
          type="text"
          placeholder="Search logs..."
          [(ngModel)]="searchQuery"
          (ngModelChange)="onSearchChange()"
          class="w-full px-2 py-1 text-sm bg-gray-800 border border-gray-700 rounded"
        >

        <!-- Filters -->
        <div class="flex gap-2 flex-wrap">
          @for (type of logTypes; track type) {
            <button
              (click)="toggleLogTypeFilter(type)"
              [class.bg-blue-600]="logTypeFilters().includes(type)"
              class="px-2 py-1 text-xs rounded bg-gray-800 hover:bg-gray-700">
              {{ type }}
            </button>
          }
        </div>
      </div>

      <!-- Virtual scroll logs -->
      <cdk-virtual-scroll-viewport
        itemSize="20"
        class="flex-1 font-mono text-xs">
        <div *cdkVirtualFor="let log of filteredLogs(); trackBy: trackByLogId"
             class="mb-1"
             [class]="logClass(log.type)">
          <span class="text-gray-500">{{ log.timestamp | date:'HH:mm:ss' }}</span>
          <span class="ml-2" [innerHTML]="highlightSearch(log.content)"></span>
        </div>
      </cdk-virtual-scroll-viewport>
    </div>
  `
})
export class TaskLogsComponent {
  taskId = input.required<string>();

  searchQuery = '';
  logTypeFilters = signal<string[]>([]);

  logs = computed(() => this.logStore.getLogsForTask(this.taskId()));

  filteredLogs = computed(() => {
    let filtered = this.logs();

    if (this.logTypeFilters().length > 0) {
      filtered = filtered.filter(log =>
        this.logTypeFilters().includes(log.type)
      );
    }

    if (this.searchQuery) {
      const query = this.searchQuery.toLowerCase();
      filtered = filtered.filter(log =>
        log.content.toLowerCase().includes(query)
      );
    }

    return filtered;
  });

  highlightSearch(content: string): string {
    if (!this.searchQuery) return content;
    const regex = new RegExp(`(${this.searchQuery})`, 'gi');
    return content.replace(regex, '<mark>$1</mark>');
  }
}
```

---

## Backend Services

### TaskService (MVP)

Manages task CRUD operations.

```csharp
public class TaskService
{
    private readonly AppDbContext _context;
    private readonly ISseService _sseService;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        AppDbContext context,
        ISseService sseService,
        ILogger<TaskService> logger)
    {
        _context = context;
        _sseService = sseService;
        _logger = logger;
    }

    public async Task<List<AgentTask>> GetAllAsync()
    {
        return await _context.Tasks.OrderBy(t => t.CreatedAt).ToListAsync();
    }

    public async Task<AgentTask?> GetByIdAsync(Guid id)
    {
        return await _context.Tasks.FindAsync(id);
    }

    public async Task<AgentTask> CreateAsync(CreateTaskDto dto)
    {
        var task = new AgentTask
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            State = PipelineState.Backlog,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        await _sseService.EmitTaskCreatedAsync(task);
        return task;
    }

    public async Task<AgentTask> UpdateAsync(Guid id, UpdateTaskDto dto)
    {
        var task = await _context.Tasks.FindAsync(id)
            ?? throw new NotFoundException($"Task {id} not found");

        if (dto.Title != null) task.Title = dto.Title;
        if (dto.Description != null) task.Description = dto.Description;
        if (dto.Priority.HasValue) task.Priority = dto.Priority.Value;

        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _sseService.EmitTaskUpdatedAsync(task);

        return task;
    }

    public async Task DeleteAsync(Guid id)
    {
        var task = await _context.Tasks.FindAsync(id)
            ?? throw new NotFoundException($"Task {id} not found");

        if (task.AssignedAgentId != null)
            throw new InvalidOperationException("Cannot delete task with active agent");

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        await _sseService.EmitTaskDeletedAsync(id);
    }

    public async Task<AgentTask> TransitionAsync(Guid id, PipelineState newState)
    {
        var task = await _context.Tasks.FindAsync(id)
            ?? throw new NotFoundException($"Task {id} not found");

        if (!IsValidTransition(task.State, newState))
            throw new InvalidOperationException($"Cannot transition from {task.State} to {newState}");

        task.State = newState;
        task.HasError = false;
        task.ErrorMessage = null;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _sseService.EmitTaskUpdatedAsync(task);

        return task;
    }

    private bool IsValidTransition(PipelineState from, PipelineState to)
    {
        // Allow moving forward or backward by one state
        var states = Enum.GetValues<PipelineState>();
        var fromIndex = Array.IndexOf(states, from);
        var toIndex = Array.IndexOf(states, to);

        return Math.Abs(toIndex - fromIndex) <= 1;
    }
}
```

### SseService (MVP)

Manages Server-Sent Events connections and broadcasts.

```csharp
public interface ISseService
{
    Task EmitTaskCreatedAsync(AgentTask task);
    Task EmitTaskUpdatedAsync(AgentTask task);
    Task EmitTaskDeletedAsync(Guid taskId);
    Task EmitTaskLogAsync(TaskLog log);
    Task EmitAgentStatusAsync(string agentId, string status, string? currentAction);
    IAsyncEnumerable<string> GetEventsAsync(CancellationToken cancellationToken);
}

public class SseService : ISseService
{
    private readonly Channel<string> _eventChannel;
    private readonly ILogger<SseService> _logger;

    public SseService(ILogger<SseService> logger)
    {
        _eventChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
        _logger = logger;
    }

    public async Task EmitTaskCreatedAsync(AgentTask task)
    {
        var data = JsonSerializer.Serialize(new { type = "task:created", data = task });
        await _eventChannel.Writer.WriteAsync($"data: {data}\n\n");
    }

    public async Task EmitTaskUpdatedAsync(AgentTask task)
    {
        var data = JsonSerializer.Serialize(new { type = "task:updated", data = task });
        await _eventChannel.Writer.WriteAsync($"data: {data}\n\n");
    }

    public async Task EmitTaskDeletedAsync(Guid taskId)
    {
        var data = JsonSerializer.Serialize(new { type = "task:deleted", data = new { taskId } });
        await _eventChannel.Writer.WriteAsync($"data: {data}\n\n");
    }

    public async Task EmitTaskLogAsync(TaskLog log)
    {
        var data = JsonSerializer.Serialize(new { type = "task:log", data = log });
        await _eventChannel.Writer.WriteAsync($"data: {data}\n\n");
    }

    public async Task EmitAgentStatusAsync(string agentId, string status, string? currentAction)
    {
        var data = JsonSerializer.Serialize(new
        {
            type = "agent:statusChanged",
            data = new { agentId, status, currentAction }
        });
        await _eventChannel.Writer.WriteAsync($"data: {data}\n\n");
    }

    public async IAsyncEnumerable<string> GetEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var evt in _eventChannel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return evt;
        }
    }
}
```

### Phase 2 Services

#### AgentPoolService

Manages concurrent agent execution with budget enforcement and priority queue.

```csharp
public class AgentPoolService
{
    private readonly int _maxConcurrent;
    private readonly ConcurrentDictionary<string, AgentInstance> _activeAgents = new();
    private readonly PriorityQueue<QueuedTask, int> _taskQueue = new();
    private readonly object _queueLock = new();
    private bool _paused;
    private bool _singleOrchestratorLock;

    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly ISseService _sseService;
    private readonly AgentRunnerService _agentRunner;
    private readonly BudgetService _budgetService;
    private readonly ILogger<AgentPoolService> _logger;

    public AgentPoolService(
        IConfiguration configuration,
        AppDbContext context,
        ISseService sseService,
        AgentRunnerService agentRunner,
        BudgetService budgetService,
        ILogger<AgentPoolService> logger)
    {
        _configuration = configuration;
        _context = context;
        _sseService = sseService;
        _agentRunner = agentRunner;
        _budgetService = budgetService;
        _logger = logger;
        _maxConcurrent = configuration.GetValue<int>("AgentPool:MaxConcurrent", 3);
    }

    public async Task<string?> SpawnAgentAsync(Guid taskId, AgentType agentType)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null) return null;

        // Check budget
        if (_budgetService.IsExceeded())
        {
            EnqueueTask(taskId, agentType, task.Priority);
            return null;
        }

        // Single orchestrator check
        if (agentType == AgentType.Orchestrator && _singleOrchestratorLock)
        {
            EnqueueTask(taskId, agentType, task.Priority);
            return null;
        }

        // Concurrency limit check
        if (_activeAgents.Count >= _maxConcurrent)
        {
            EnqueueTask(taskId, agentType, task.Priority);
            return null;
        }

        var agentId = Guid.NewGuid().ToString();
        var agent = new AgentInstance
        {
            Id = agentId,
            Type = agentType,
            Status = AgentStatus.Running,
            TaskId = taskId,
            StartedAt = DateTime.UtcNow,
            CurrentAction = "Initializing..."
        };

        if (agentType == AgentType.Orchestrator)
        {
            _singleOrchestratorLock = true;
        }

        _activeAgents[agentId] = agent;
        await _sseService.EmitAgentStatusAsync(agentId, "running", "Initializing...");

        // Run agent in background
        _ = RunAgentAsync(agent);

        return agentId;
    }

    private void EnqueueTask(Guid taskId, AgentType agentType, Priority priority)
    {
        lock (_queueLock)
        {
            _taskQueue.Enqueue(new QueuedTask(taskId, agentType), GetPriorityValue(priority));
        }
    }

    private int GetPriorityValue(Priority priority) => priority switch
    {
        Priority.Critical => 0,
        Priority.High => 1,
        Priority.Medium => 2,
        Priority.Low => 3,
        _ => 2
    };

    private async Task RunAgentAsync(AgentInstance agent)
    {
        try
        {
            var task = await _context.Tasks.FindAsync(agent.TaskId);
            if (task == null) return;

            var agentTask = new AgentTask
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description
            };

            var result = await _agentRunner.RunAgentAsync(
                agentTask,
                async output =>
                {
                    var log = new TaskLog
                    {
                        Id = Guid.NewGuid(),
                        TaskId = task.Id,
                        AgentId = agent.Id,
                        Timestamp = DateTime.UtcNow,
                        Type = LogType.Info,
                        Content = output
                    };

                    _context.TaskLogs.Add(log);
                    await _context.SaveChangesAsync();
                    await _sseService.EmitTaskLogAsync(log);
                },
                CancellationToken.None
            );

            await CompleteAgentAsync(agent.Id, result.Success ? "success" : "error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent {AgentId} failed", agent.Id);
            await HandleAgentErrorAsync(agent, ex);
        }
    }

    private async Task HandleAgentErrorAsync(AgentInstance agent, Exception error)
    {
        var task = await _context.Tasks.FindAsync(agent.TaskId);
        if (task != null)
        {
            task.HasError = true;
            task.ErrorMessage = error.Message;
            await _context.SaveChangesAsync();
            await _sseService.EmitTaskUpdatedAsync(task);
        }

        await CompleteAgentAsync(agent.Id, "error");
    }

    private async Task CompleteAgentAsync(string agentId, string status)
    {
        if (_activeAgents.TryRemove(agentId, out var agent))
        {
            if (agent.Type == AgentType.Orchestrator)
            {
                _singleOrchestratorLock = false;
            }

            await _sseService.EmitAgentStatusAsync(agentId, status, null);

            // Process queue
            if (!_paused)
            {
                await ProcessQueueAsync();
            }
        }
    }

    private async Task ProcessQueueAsync()
    {
        QueuedTask? next = null;
        lock (_queueLock)
        {
            if (_taskQueue.Count > 0 && _activeAgents.Count < _maxConcurrent)
            {
                next = _taskQueue.Dequeue();
            }
        }

        if (next != null)
        {
            await SpawnAgentAsync(next.TaskId, next.AgentType);
        }
    }

    public async Task PauseAllAsync()
    {
        _paused = true;
        await _sseService.EmitAgentStatusAsync("pool", "paused", null);
    }

    public async Task ResumeAllAsync()
    {
        _paused = false;
        await ProcessQueueAsync();
    }

    public async Task AbortAgentAsync(string agentId)
    {
        if (_activeAgents.TryGetValue(agentId, out _))
        {
            _agentRunner.AbortAgent();
            await CompleteAgentAsync(agentId, "aborted");
        }
    }

    public PoolStatus GetStatus()
    {
        return new PoolStatus
        {
            Active = _activeAgents.Count,
            Max = _maxConcurrent,
            Paused = _paused,
            QueuedCount = _taskQueue.Count
        };
    }
}

public record QueuedTask(Guid TaskId, AgentType AgentType);

public class PoolStatus
{
    public int Active { get; set; }
    public int Max { get; set; }
    public bool Paused { get; set; }
    public int QueuedCount { get; set; }
}
```

#### BudgetService

Manages global daily/monthly token budgets.

```csharp
public class BudgetService
{
    private readonly AppDbContext _context;
    private readonly ISseService _sseService;
    private readonly ILogger<BudgetService> _logger;

    public BudgetService(
        AppDbContext context,
        ISseService sseService,
        ILogger<BudgetService> logger)
    {
        _context = context;
        _sseService = sseService;
        _logger = logger;
    }

    public async Task RecordUsageAsync(int tokens)
    {
        var config = await GetConfigAsync();

        config.CurrentDailyUsage += tokens;
        config.CurrentMonthlyUsage += tokens;

        var dailyPercent = (double)config.CurrentDailyUsage / config.DailyTokenBudget * 100;
        var monthlyPercent = (double)config.CurrentMonthlyUsage / config.MonthlyTokenBudget * 100;

        if (dailyPercent >= 80 && !config.DailyWarningShown)
        {
            _logger.LogWarning("Daily budget 80% used");
            config.DailyWarningShown = true;
        }

        if (config.CurrentDailyUsage >= config.DailyTokenBudget)
        {
            config.BudgetExceeded = true;
            _logger.LogWarning("Daily budget exceeded - agents paused");
        }

        await _context.SaveChangesAsync();
        await EmitBudgetUpdateAsync(config);
    }

    public bool IsExceeded()
    {
        var config = _context.Configs.FirstOrDefault();
        return config?.BudgetExceeded ?? false;
    }

    public bool WouldExceed(int tokens)
    {
        var config = _context.Configs.FirstOrDefault();
        if (config == null) return false;
        return (config.CurrentDailyUsage + tokens) > config.DailyTokenBudget;
    }

    public async Task ResetDailyAsync()
    {
        var config = await GetConfigAsync();
        config.CurrentDailyUsage = 0;
        config.DailyWarningShown = false;
        config.BudgetExceeded = false;
        await _context.SaveChangesAsync();
    }

    public async Task ResetMonthlyAsync()
    {
        var config = await GetConfigAsync();
        config.CurrentMonthlyUsage = 0;
        config.MonthlyWarningShown = false;
        await _context.SaveChangesAsync();
    }

    public BudgetStatus GetStatus()
    {
        var config = _context.Configs.FirstOrDefault() ?? new DashboardConfig();
        return new BudgetStatus
        {
            DailyUsage = config.CurrentDailyUsage,
            DailyLimit = config.DailyTokenBudget,
            MonthlyUsage = config.CurrentMonthlyUsage,
            MonthlyLimit = config.MonthlyTokenBudget,
            Exceeded = config.BudgetExceeded
        };
    }

    private async Task<DashboardConfig> GetConfigAsync()
    {
        var config = await _context.Configs.FirstOrDefaultAsync();
        if (config == null)
        {
            config = new DashboardConfig();
            _context.Configs.Add(config);
            await _context.SaveChangesAsync();
        }
        return config;
    }

    private async Task EmitBudgetUpdateAsync(DashboardConfig config)
    {
        // Implementation would emit budget update via SSE
    }
}

public class BudgetStatus
{
    public int DailyUsage { get; set; }
    public int DailyLimit { get; set; }
    public int MonthlyUsage { get; set; }
    public int MonthlyLimit { get; set; }
    public bool Exceeded { get; set; }
}
```

#### GitService

Manages git operations for feature branches.

```csharp
public class GitService
{
    private readonly string _repositoryPath;
    private readonly string _defaultBranch;
    private readonly string _branchPrefix;
    private string? _activeImplementationBranch;
    private readonly ILogger<GitService> _logger;

    public GitService(IConfiguration configuration, ILogger<GitService> logger)
    {
        _repositoryPath = configuration["Repository:Path"] ?? ".";
        _defaultBranch = configuration["Repository:DefaultBranch"] ?? "main";
        _branchPrefix = configuration["Repository:BranchPrefix"] ?? "feature/agent-";
        _logger = logger;
    }

    public async Task<string> CreateFeatureBranchAsync(Guid taskId, string title)
    {
        if (_activeImplementationBranch != null)
        {
            throw new InvalidOperationException(
                "Another task is currently in implementation. Please wait.");
        }

        var branchName = $"{_branchPrefix}{taskId:N}-{Slugify(title)}";

        await RunGitAsync($"checkout {_defaultBranch}");
        await RunGitAsync("pull");
        await RunGitAsync($"checkout -b {branchName}");

        _activeImplementationBranch = branchName;
        return branchName;
    }

    public async Task CommitChangesAsync(string message)
    {
        await RunGitAsync("add .");
        await RunGitAsync($"commit -m \"{message}\"");
    }

    public async Task PushBranchAsync(string branch)
    {
        await RunGitAsync($"push -u origin {branch}");
    }

    public void ReleaseBranch()
    {
        _activeImplementationBranch = null;
    }

    private async Task RunGitAsync(string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = _repositoryPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Git command failed: {error}");
        }
    }

    private static string Slugify(string text)
    {
        return System.Text.RegularExpressions.Regex
            .Replace(text.ToLower(), @"[^a-z0-9]+", "-")
            .Trim('-');
    }
}
```

#### PatternsService

Manages repository-specific knowledge base (CLAUDE.md).

```csharp
public class PatternsService
{
    private readonly string _repositoryPath;
    private readonly GitService _gitService;
    private readonly ILogger<PatternsService> _logger;

    public PatternsService(
        IConfiguration configuration,
        GitService gitService,
        ILogger<PatternsService> logger)
    {
        _repositoryPath = configuration["Repository:Path"] ?? ".";
        _gitService = gitService;
        _logger = logger;
    }

    public async Task UpdatePatternsAsync()
    {
        var claudePath = Path.Combine(_repositoryPath, "CLAUDE.md");

        // Get recent merged PRs (implementation would use GitHub API)
        var mergedPRs = await GetRecentMergedPRsAsync();

        // Extract patterns (would use AI to analyze)
        var patterns = await ExtractPatternsAsync(mergedPRs);

        // Update CLAUDE.md
        var existingContent = File.Exists(claudePath)
            ? await File.ReadAllTextAsync(claudePath)
            : "";
        var updatedContent = MergePatterns(existingContent, patterns);

        await File.WriteAllTextAsync(claudePath, updatedContent);

        // Commit the update
        await _gitService.CommitChangesAsync("Update CLAUDE.md with learned patterns");
    }

    private Task<List<PullRequest>> GetRecentMergedPRsAsync()
    {
        // Implementation would fetch from GitHub API
        return Task.FromResult(new List<PullRequest>());
    }

    private Task<List<Pattern>> ExtractPatternsAsync(List<PullRequest> prs)
    {
        // Implementation would use AI to analyze PRs
        return Task.FromResult(new List<Pattern>());
    }

    private string MergePatterns(string existing, List<Pattern> patterns)
    {
        // Implementation would merge new patterns into existing content
        return existing;
    }
}

public class PullRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Files { get; set; } = new();
}

public class Pattern
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Files { get; set; } = new();
}
```

---

## Database Schema (Entity Framework Core)

```csharp
public class AppDbContext : DbContext
{
    public DbSet<AgentTask> Tasks { get; set; }
    public DbSet<TaskLog> TaskLogs { get; set; }
    public DbSet<DashboardConfig> Configs { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(4000);
            entity.Property(e => e.State).HasConversion<string>();
            entity.Property(e => e.Priority).HasConversion<string>();
        });

        modelBuilder.Entity<TaskLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TaskId);
            entity.HasIndex(e => e.Timestamp);
            entity.Property(e => e.Type).HasConversion<string>();
        });

        modelBuilder.Entity<DashboardConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValue("default");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
            entity.Property(e => e.Type).HasConversion<string>();
        });
    }
}
```

---

## Implementation Phases

### Phase 1: Foundation (MVP)

**Goal**: Basic working pipeline with manual state transitions and single agent.

**Scope**:
1. Set up .NET 10 backend with Minimal APIs
2. Set up Angular 21 frontend
3. Implement Entity Framework Core with SQLite
4. Create basic REST API for tasks CRUD
5. Build Kanban board UI (no drag-and-drop, click to view details)
6. Add EventSource/SSE for real-time updates
7. Implement single agent execution via stdin/stdout
8. Task detail view with transition buttons
9. Simple log viewer (no virtual scrolling, no filters)

**Deliverables**:
- Working Kanban board with fixed 7 states
- Create/edit/delete tasks (title, description, state, priority)
- Manual state transitions via buttons in detail view
- Single agent can execute via Claude Code CLI
- Real-time log streaming
- Error display on task cards

### Phase 2: Extended Features

**Goal**: Multi-agent coordination, budget tracking, and advanced task management.

**Scope**:
1. Implement AgentPoolService with concurrency control (max N)
2. Add all agent definitions (UI, API, Simplifier, Reviewer, Test, PR)
3. Build TaskOrchestrationService for automatic transitions
4. Implement subtask creation from orchestrator (max 2 levels)
5. Dependency graph tracking with blocking
6. Priority queue without preemption
7. Error retry with configurable per-type logic
8. Budget tracking widget
9. Notifications panel
10. Cost estimation before task creation (Haiku)
11. Extended task fields (acceptanceCriteria, edgeCases, etc.)
12. Log filters and search
13. Virtual scrolling for logs
14. Git branch integration

**Deliverables**:
- Concurrent agent execution (configurable max, default 3)
- Priority-based task queue
- All specialist agents working
- Automatic flow: orchestrator → specialists → review → test → PR
- Budget widget with daily/monthly tracking
- Notifications for state changes, budget warnings
- Complex task fields
- Log search and filtering

### Phase 3: Advanced Features

**Goal**: Pattern learning, GitHub integration, polish.

**Scope**:
1. Repository patterns database (CLAUDE.md)
2. Manual pattern update from merged PRs
3. GitHub webhook for auto-DONE on PR merge
4. Protected file paths enforcement
5. Pause entire pipeline
6. Pause specific task and subtasks
7. Tool permission prompts
8. Startup orphaned branch cleanup
9. Log retention policy (30 days)
10. Daily/monthly budget reset cron jobs

**Deliverables**:
- CLAUDE.md learning from PRs
- Auto-complete tasks on PR merge
- Full pipeline controls
- Production-ready error handling
- Complete budget management

### Phase 4: Polish & Optimization

**Goal**: Production-ready with enhanced UX.

**Scope**:
1. Settings page for configuration (requires restart warning)
2. Graceful shutdown for migrations
3. Better error messages and user guidance
4. Performance optimization for large task lists
5. Comprehensive documentation
6. Docker compose for easy setup
7. Environment variable validation
8. Health checks

**Deliverables**:
- Complete settings management
- Production deployment guide
- Performance tested with 50+ tasks
- Full documentation

---

## Environment Variables

```env
# Database
DATABASE_URL="Data Source=app.db"  # SQLite for dev
# DATABASE_URL="Host=localhost;Database=agent_dashboard;Username=user;Password=pass"  # PostgreSQL for prod

# Claude Code
CLAUDE_CODE_PATH="claude"

# Git
REPOSITORY_PATH="/path/to/your/repo"
GITHUB_TOKEN="ghp_..."  # For PR creation (Phase 2)

# Server
ASPNETCORE_URLS="http://localhost:5000"
ASPNETCORE_ENVIRONMENT="Development"

# Budget (Phase 2)
DAILY_TOKEN_BUDGET=100000
MONTHLY_TOKEN_BUDGET=5000000

# Agent Pool (Phase 2)
MAX_CONCURRENT_AGENTS=3

# Protected Paths (Phase 2)
PROTECTED_PATHS=".git/,.env,node_modules/"
```

---

## Getting Started Commands

```bash
# Create backend
dotnet new webapi -n AgentDashboard.Api -o backend
cd backend
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design

# Create frontend
cd ..
ng new frontend --style=scss --routing=true --ssr=false
cd frontend
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init

# Initialize database
cd ../backend
dotnet ef migrations add InitialCreate
dotnet ef database update

# Start development
# Terminal 1: Backend
cd backend
dotnet run

# Terminal 2: Frontend
cd frontend
ng serve
```

---

## Success Criteria

1. **Functional**: Tasks flow from Backlog to Done with agent automation
2. **Observable**: All agent activity visible in real-time with logs
3. **Controllable**: Manual overrides at all levels
4. **Reliable**: Graceful error handling
5. **Simple (MVP)**: Single agent, basic CRUD, manual transitions

---

## Design Decisions Summary

### Error Handling
- **MVP**: Display error on task card, manual retry via transition
- **Phase 2**: Retry strategy configurable per error type

### State Management
- **Pipeline**: Fixed 7-state workflow, no customization
- **Transitions**: Buttons in task detail view only (no drag-and-drop)
- **Completion**: Manual (MVP) / Auto-DONE when PR merged (Phase 2)

### Concurrency
- **MVP**: One agent at a time
- **Phase 2**: Configurable pool (default 3), single orchestrator

### Budget
- **MVP**: Not tracked
- **Phase 2**: Global limits with daily/monthly budgets

### Git Workflow
- **MVP**: Not integrated
- **Phase 2**: Simple branches, one active at a time
- **Future**: Worktrees for true parallelism

### Real-time Updates
- **Protocol**: EventSource/SSE (not WebSocket)
- **Payload**: Full state every time (not deltas)

### Knowledge Base
- **Scope**: Per-repository (CLAUDE.md)
- **Updates**: Manual trigger only (Phase 3)

### UI/UX
- **Board**: Fixed states, no filtering
- **Logs**: Simple scrolling (MVP) / Virtual scrolling with filters (Phase 2)
- **Errors**: Red border on task card

---

## Open Questions / Future Considerations

1. **Multi-repo support**: Workspace concept for managing multiple repositories
2. **Worktrees**: Replace branch strategy for true parallel implementation
3. **Authentication**: OAuth/SSO for multi-user deployments
4. **Agent metrics**: Performance analytics and prompt tuning insights
5. **Custom agents**: UI for creating new agent definitions
6. **Rollback**: Undo agent changes via git operations
7. **Notifications**: External integrations (Slack, Discord)
8. **Cost optimization**: Automatic model selection based on task complexity
9. **Testing**: E2E testing strategy for agent workflows
10. **Observability**: Prometheus metrics, distributed tracing

---

## Risk Mitigation

### Risk: Git merge conflicts and data loss
**Mitigation**:
- MVP: No git integration
- Phase 2: Simple branches with one task at a time
- Protected paths prevent critical file modification
- All changes reviewed before PR
- Future: Worktrees provide isolation

### Risk: Runaway agent costs
**Mitigation**:
- MVP: Single agent limits exposure
- Phase 2: Hard budget limits pause agents
- Cost estimation before task creation
- Token tracking per task and globally
- Warnings at 80% threshold

### Risk: System state inconsistency
**Mitigation**:
- Graceful shutdown for migrations
- Error retry with exponential backoff
- Human intervention for unrecoverable errors

### Risk: Agent infinite loops
**Mitigation**:
- Max retry counts per error type
- Max subtask nesting (2 levels)
- Budget limits as circuit breaker
- Manual abort controls

### Risk: Poor agent decisions
**Mitigation**:
- High-quality orchestrator (Opus)
- Repository patterns database
- Human override via task transitions
- Revision cycles for failed reviews/tests

---

**Specification Version**: 3.0
**Last Updated**: 2026-01-24
**Status**: Ready for Implementation
