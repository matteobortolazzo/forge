# Forge.Ui - Angular Frontend Implementation

Angular 21 single-page application for the AI Agent Dashboard. Uses signals for state management, Tailwind CSS for styling, and runs in zoneless mode.

## Directory Structure

```
src/app/
├── features/                      # Feature modules (lazy-loaded)
│   ├── queue/                     # Task queue feature (primary view)
│   │   ├── task-queue.component.ts      # Main queue view with table, filters, sorting
│   │   ├── task-row.component.ts        # Individual task row with hierarchy support
│   │   └── split-task-dialog.component.ts  # Dialog for splitting tasks into subtasks
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
│   │   ├── notification.store.ts        # Notification management
│   │   ├── scheduler.store.ts           # Scheduler state management
│   │   └── artifact.store.ts            # Artifact management
│   ├── services/                  # API and infrastructure services
│   │   ├── task.service.ts              # Task API operations
│   │   ├── agent.service.ts             # Agent status API
│   │   ├── sse.service.ts               # Server-sent events
│   │   ├── scheduler.service.ts         # Scheduler API
│   │   └── artifact.service.ts          # Artifact API
│   └── mocks/                     # Development mock data
│       └── mock-data.ts                 # Tasks, logs, notifications
├── shared/                        # Reusable components and models
│   ├── components/                # Presentation components
│   │   ├── state-badge.component.ts     # Pipeline state badge
│   │   ├── priority-badge.component.ts  # Priority level badge
│   │   ├── agent-indicator.component.ts # Agent running indicator
│   │   ├── paused-badge.component.ts    # Paused state indicator
│   │   ├── scheduler-status.component.ts # Scheduler status display
│   │   ├── artifact-type-badge.component.ts # Artifact type badge
│   │   ├── artifact-panel.component.ts  # Artifact display panel
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
| `TaskQueueComponent` | `features/queue/` | Main task queue view with table, state/priority filters, sorting, parent/child hierarchy |
| `TaskRowComponent` | `features/queue/` | Task row displaying title, state, priority, progress, with expand/collapse for subtasks |
| `SplitTaskDialogComponent` | `features/queue/` | Modal dialog for splitting a task into subtasks |
| `TaskDetailComponent` | `features/task-detail/` | Task detail view with metadata, actions, and agent output |
| `AgentOutputComponent` | `features/task-detail/` | Real-time agent log viewer with color-coded entries |
| `NotificationPanelComponent` | `features/notifications/` | Dropdown panel showing recent notifications |

### Shared Components

| Component | Selector | Inputs | Outputs |
|-----------|----------|--------|---------|
| `StateBadgeComponent` | `app-state-badge` | `state: PipelineState` (required) | - |
| `PriorityBadgeComponent` | `app-priority-badge` | `priority: Priority` (required) | - |
| `AgentIndicatorComponent` | `app-agent-indicator` | `isRunning: boolean`, `showLabel: boolean` | - |
| `PausedBadgeComponent` | `app-paused-badge` | `isPaused: boolean` | - |
| `SchedulerStatusComponent` | `app-scheduler-status` | - | - |
| `ArtifactTypeBadgeComponent` | `app-artifact-type-badge` | `type: ArtifactType` | - |
| `ArtifactPanelComponent` | `app-artifact-panel` | `artifact: Artifact` | - |
| `ErrorAlertComponent` | `app-error-alert` | `title?: string`, `message: string` (required), `dismissible: boolean` | `dismiss: void` |
| `LoadingSpinnerComponent` | `app-loading-spinner` | `size: 'sm'\|'md'\|'lg'`, `label?: string`, `inline: boolean` | - |

### Queue Features

The task queue provides:
- **Table view**: Tasks displayed in a sortable, filterable table
- **State filter**: Filter by pipeline state (Backlog, Research, Planning, etc.)
- **Priority filter**: Filter by priority level (Low, Medium, High, Critical)
- **Sorting**: Sort by title, state, priority, or updated date
- **Hierarchy display**: Parent tasks expandable to show subtasks
- **Progress indicators**: Visual progress bars for parent task completion
- **Quick actions**: Start agent, pause/resume, split task actions in each row

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

### SchedulerStore (`core/stores/scheduler.store.ts`)

**Signals:**
- `isEnabled: Signal<boolean>` - Scheduler enabled state
- `isAgentRunning: Signal<boolean>` - Agent running state
- `pendingCount: Signal<number>` - Pending tasks count
- `pausedCount: Signal<number>` - Paused tasks count

**Actions:**
- `loadStatus()` - Load scheduler status
- `enable()` - Enable scheduler
- `disable()` - Disable scheduler
- `updateFromEvent(status)` - Update from SSE event

### ArtifactStore (`core/stores/artifact.store.ts`)

**Signals:**
- `isLoading: Signal<boolean>` - Loading state
- `artifacts: Signal<Artifact[]>` - Artifacts for current task

**Actions:**
- `loadForTask(taskId)` - Load artifacts for task
- `addArtifact(artifact)` - Add artifact from SSE event

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
- `pauseTask(taskId): Observable<Task>`
- `resumeTask(taskId): Observable<Task>`

### AgentService (`core/services/agent.service.ts`)

HTTP service for agent status.

**Methods:**
- `getStatus(): Observable<AgentStatus>`

### SchedulerService (`core/services/scheduler.service.ts`)

HTTP service for scheduler control.

**Methods:**
- `getStatus(): Observable<SchedulerStatus>`
- `enable(): Observable<void>`
- `disable(): Observable<void>`

### ArtifactService (`core/services/artifact.service.ts`)

HTTP service for task artifacts.

**Methods:**
- `getArtifacts(taskId): Observable<Artifact[]>`
- `getLatestArtifact(taskId): Observable<Artifact>`
- `getArtifactsByState(taskId, state): Observable<Artifact[]>`

### SseService (`core/services/sse.service.ts`)

Server-sent events connection management.

**Methods:**
- `connect(): Observable<ServerEvent>` - Connect to SSE stream
- `disconnect(): void` - Disconnect from SSE stream

## Routes

Defined in `app.routes.ts`:

| Path | Component | Load |
|------|-----------|------|
| `/` | `TaskQueueComponent` | Lazy |
| `/tasks/:id` | `TaskDetailComponent` | Lazy |
| `**` | Redirect to `/` | - |

## Models

Defined in `shared/models/index.ts`:

**Pipeline States:**
```typescript
const PIPELINE_STATES = ['Backlog', 'Split', 'Research', 'Planning', 'Implementing', 'Simplifying', 'Verifying', 'Reviewing', 'PrReady', 'Done'] as const;
type PipelineState = (typeof PIPELINE_STATES)[number];
```

**Priority Levels:**
```typescript
const PRIORITIES = ['Low', 'Medium', 'High', 'Critical'] as const;
type Priority = (typeof PRIORITIES)[number];
```

**Log Types:**
```typescript
const LOG_TYPES = ['info', 'toolUse', 'toolResult', 'error', 'thinking'] as const;
type LogType = (typeof LOG_TYPES)[number];
```

**Interfaces:** `Task`, `TaskLog`, `Notification`, `Artifact`, `CreateTaskDto`, `UpdateTaskDto`, `TransitionTaskDto`, `ServerEvent`, `AgentStatus`, `SchedulerStatus`

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

## Testing

### Framework: Vitest 4.x

This project uses **Vitest** (NOT Jasmine, NOT Jest) for unit testing.

**Test file location:** `*.spec.ts` files co-located with source files.

**Run tests:**
```bash
ng test           # Run tests via Angular CLI (uses Vitest)
npm run test      # Alternative
```

**Quick Reference:**
| Pattern | Vitest Syntax |
|---------|---------------|
| Create mock function | `vi.fn()` |
| Spy on method | `vi.spyOn(obj, 'method')` |
| Mock return value | `vi.fn().mockReturnValue(value)` |
| Mock resolved promise | `vi.fn().mockResolvedValue(value)` |
| Mock observable | `vi.fn().mockReturnValue(of(value))` |
| Assert called | `expect(mock).toHaveBeenCalled()` |

**Example:**
```typescript
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

const serviceMock = {
  getData: vi.fn().mockReturnValue(of(['item1', 'item2'])),
};
```

See `src/Forge.Ui/CLAUDE.md` for complete testing patterns.

## Related Documentation

- [API Integration Guide](./API-INTEGRATION.md) - API endpoints, SSE events, data models
- [Angular Coding Standards](./CLAUDE.md) - TypeScript and Angular conventions
