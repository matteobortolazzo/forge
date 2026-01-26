# Forge.Ui - Angular Frontend Implementation

Angular 21 single-page application for the AI Agent Dashboard. Uses signals for state management, Tailwind CSS for styling, and runs in zoneless mode.

## Directory Structure

```
src/app/
├── features/                      # Feature modules (lazy-loaded)
│   ├── board/                     # Kanban board feature
│   │   ├── board.component.ts           # Main board view
│   │   ├── task-column.component.ts     # Column for pipeline state
│   │   ├── task-card.component.ts       # Task card display
│   │   └── create-task-dialog.component.ts  # Task creation modal
│   ├── task-detail/               # Task detail feature
│   │   ├── task-detail.component.ts     # Task detail view
│   │   └── agent-output.component.ts    # Agent log viewer
│   └── notifications/             # Notifications feature
│       └── notification-panel.component.ts  # Notification dropdown
├── core/                          # Singleton services and stores
│   ├── stores/                    # Signal-based state stores
│   │   ├── task.store.ts                # Task state management
│   │   ├── agent.store.ts               # Agent status management
│   │   ├── log.store.ts                 # Task log management
│   │   └── notification.store.ts        # Notification management
│   ├── services/                  # API and infrastructure services
│   │   ├── task.service.ts              # Task API operations
│   │   ├── agent.service.ts             # Agent status API
│   │   └── sse.service.ts               # Server-sent events
│   └── mocks/                     # Development mock data
│       └── mock-data.ts                 # 18 tasks, logs, notifications
├── shared/                        # Reusable components and models
│   ├── components/                # Presentation components
│   │   ├── state-badge.component.ts     # Pipeline state badge
│   │   ├── priority-badge.component.ts  # Priority level badge
│   │   ├── agent-indicator.component.ts # Agent running indicator
│   │   ├── error-alert.component.ts     # Error alert display
│   │   └── loading-spinner.component.ts # Loading spinner
│   └── models/                    # TypeScript interfaces
│       └── index.ts                     # All shared types
├── app.routes.ts                  # Route configuration
├── app.config.ts                  # App configuration
└── app.ts                         # Root component
```

## Component Inventory

### Feature Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `BoardComponent` | `features/board/` | Main Kanban board view with 7 columns, header, and SSE connection |
| `TaskColumnComponent` | `features/board/` | Single column displaying tasks for a pipeline state |
| `TaskCardComponent` | `features/board/` | Task card with title, description preview, badges |
| `CreateTaskDialogComponent` | `features/board/` | Modal dialog for creating new tasks |
| `TaskDetailComponent` | `features/task-detail/` | Task detail view with metadata, actions, and agent output |
| `AgentOutputComponent` | `features/task-detail/` | Real-time agent log viewer with color-coded entries |
| `NotificationPanelComponent` | `features/notifications/` | Dropdown panel showing recent notifications |

### Shared Components

| Component | Selector | Inputs | Outputs |
|-----------|----------|--------|---------|
| `StateBadgeComponent` | `app-state-badge` | `state: PipelineState` (required) | - |
| `PriorityBadgeComponent` | `app-priority-badge` | `priority: Priority` (required) | - |
| `AgentIndicatorComponent` | `app-agent-indicator` | `isRunning: boolean`, `showLabel: boolean` | - |
| `ErrorAlertComponent` | `app-error-alert` | `title?: string`, `message: string` (required), `dismissible: boolean` | `dismiss: void` |
| `LoadingSpinnerComponent` | `app-loading-spinner` | `size: 'sm'\|'md'\|'lg'`, `label?: string`, `inline: boolean` | - |

### Component Input/Output Reference

**TaskColumnComponent**
```typescript
readonly state = input.required<PipelineState>();
readonly tasks = input.required<Task[]>();
```

**TaskCardComponent**
```typescript
readonly task = input.required<Task>();
```

**CreateTaskDialogComponent**
```typescript
readonly isOpen = input(false);
readonly create = output<CreateTaskDto>();
readonly cancel = output<void>();
```

**AgentOutputComponent**
```typescript
readonly logs = input.required<TaskLog[]>();
readonly autoScroll = input(true);
```

## State Management

All stores use Angular signals and are provided at root level.

### TaskStore (`core/stores/task.store.ts`)

**Signals:**
- `isLoading: Signal<boolean>` - Loading state
- `errorMessage: Signal<string | null>` - Error message
- `allTasks: Signal<Task[]>` - All tasks
- `tasksByState: Signal<Record<PipelineState, Task[]>>` - Tasks grouped by state
- `taskCountByState: Signal<Record<PipelineState, number>>` - Count per state
- `totalTaskCount: Signal<number>` - Total task count
- `tasksWithErrors: Signal<Task[]>` - Tasks with errors
- `tasksWithAgents: Signal<Task[]>` - Tasks with active agents

**Actions:**
- `loadTasks()` - Load all tasks from API
- `createTask(dto)` - Create new task
- `updateTask(id, dto)` - Update existing task
- `deleteTask(id)` - Delete task
- `transitionTask(id, targetState)` - Move task to new state
- `startAgent(taskId)` - Start agent on task
- `abortAgent(taskId)` - Abort agent on task
- `updateTaskFromEvent(task)` - Update from SSE event
- `removeTaskFromEvent(taskId)` - Remove from SSE event

### AgentStore (`core/stores/agent.store.ts`)

**Signals:**
- `isLoading: Signal<boolean>` - Loading state
- `errorMessage: Signal<string | null>` - Error message
- `agentStatus: Signal<AgentStatus>` - Current agent status
- `isAgentRunning: Signal<boolean>` - Is agent running
- `currentTaskId: Signal<string | undefined>` - Current task ID

**Actions:**
- `loadStatus()` - Load agent status from API
- `startAgent(taskId)` - Start agent on task
- `updateStatusFromEvent(status)` - Update from SSE event
- `clearStatus()` - Clear agent status

### LogStore (`core/stores/log.store.ts`)

**Signals:**
- `isLoading: Signal<boolean>` - Loading state
- `errorMessage: Signal<string | null>` - Error message

**Actions:**
- `loadLogsForTask(taskId)` - Load logs for specific task
- `getLogsForTask(taskId): TaskLog[]` - Get cached logs
- `getLogCount(taskId): number` - Get log count
- `addLog(log)` - Add log from SSE event
- `clearLogsForTask(taskId)` - Clear logs for task
- `clearAllLogs()` - Clear all logs

### NotificationStore (`core/stores/notification.store.ts`)

**Signals:**
- `allNotifications: Signal<Notification[]>` - All notifications
- `unreadNotifications: Signal<Notification[]>` - Unread only
- `unreadCount: Signal<number>` - Unread count
- `recentNotifications: Signal<Notification[]>` - Last 10 notifications

**Actions:**
- `addNotification(notification)` - Add notification
- `markAsRead(id)` - Mark single as read
- `markAllAsRead()` - Mark all as read
- `removeNotification(id)` - Remove notification
- `clearAll()` - Clear all notifications
- `notifyTaskCreated(title, taskId)` - Helper for task created
- `notifyTaskCompleted(title, taskId)` - Helper for task completed
- `notifyAgentStarted(title, taskId)` - Helper for agent started
- `notifyAgentError(title, taskId, error)` - Helper for agent error

## Services

### TaskService (`core/services/task.service.ts`)

HTTP service for task CRUD operations.

**Mock Mode:** Enabled by default (`useMocks = true`). Uses in-memory task store with simulated delays.

**Methods:**
- `getTasks(): Observable<Task[]>`
- `getTask(id): Observable<Task>`
- `createTask(dto): Observable<Task>`
- `updateTask(id, dto): Observable<Task>`
- `deleteTask(id): Observable<void>`
- `transitionTask(id, dto): Observable<Task>`
- `getTaskLogs(taskId): Observable<TaskLog[]>`
- `startAgent(taskId): Observable<Task>`
- `abortAgent(taskId): Observable<Task>`
- `getNextState(currentState): PipelineState | null`
- `getPreviousState(currentState): PipelineState | null`

### AgentService (`core/services/agent.service.ts`)

HTTP service for agent status.

**Mock Mode:** Enabled by default. Returns simulated agent status.

**Methods:**
- `getStatus(): Observable<AgentStatus>`

### SseService (`core/services/sse.service.ts`)

Server-sent events connection management.

**Mock Mode:** Enabled by default. Generates simulated log events every 3 seconds.

**Methods:**
- `connect(): Observable<ServerEvent>` - Connect to SSE stream
- `disconnect(): void` - Disconnect from SSE stream

## Routes

Defined in `app.routes.ts`:

| Path | Component | Load |
|------|-----------|------|
| `/` | `BoardComponent` | Lazy |
| `/tasks/:id` | `TaskDetailComponent` | Lazy |
| `**` | Redirect to `/` | - |

## Models

Defined in `shared/models/index.ts`:

**Pipeline States:**
```typescript
const PIPELINE_STATES = ['Backlog', 'Planning', 'Implementing', 'Reviewing', 'Testing', 'PrReady', 'Done'] as const;
type PipelineState = (typeof PIPELINE_STATES)[number];
```

**Priority Levels:**
```typescript
const PRIORITIES = ['low', 'medium', 'high', 'critical'] as const;
type Priority = (typeof PRIORITIES)[number];
```

**Log Types:**
```typescript
const LOG_TYPES = ['info', 'toolUse', 'toolResult', 'error', 'thinking'] as const;
type LogType = (typeof LOG_TYPES)[number];
```

**Interfaces:** `Task`, `TaskLog`, `Notification`, `CreateTaskDto`, `UpdateTaskDto`, `TransitionTaskDto`, `ServerEvent`, `AgentStatus`

## Mock Data

Located in `core/mocks/mock-data.ts`:

- **18 tasks** distributed across all pipeline states
- **12 log entries** for active task (task-006)
- **3 error logs** for task-008
- **5 notifications** with various types

Helper functions:
- `getLogsForTask(taskId): TaskLog[]`
- `getTaskById(taskId): Task | undefined`
- `getTasksByState(state): Task[]`

## Development Commands

```bash
# Install dependencies
npm install

# Start dev server (http://localhost:4200)
ng serve

# Build for production
ng build --configuration production

# Run unit tests
ng test

# Lint code
ng lint
```

## Key Implementation Patterns

### Standalone Components
All components are standalone (Angular 21 default). No NgModules.

### Signal-Based Reactivity
- Use `signal()` for mutable state
- Use `computed()` for derived state
- Use `input()` and `output()` instead of decorators

### OnPush Change Detection
All components use `ChangeDetectionStrategy.OnPush`.

### Modern Control Flow
Templates use `@if`, `@for`, `@switch` syntax (not `*ngIf`, `*ngFor`).

### Zoneless Mode
Application runs without Zone.js for better performance.

## Related Documentation

- [API Integration Guide](./API-INTEGRATION.md) - API endpoints, SSE events, data models
- [Angular Coding Standards](./CLAUDE.md) - TypeScript and Angular conventions
