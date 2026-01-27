# API Integration Guide

This document provides complete API documentation for connecting the Angular frontend to the .NET backend.

## Quick Start

### 1. Disable Mock Mode

Edit the following files and set `useMocks = false`:

**`src/app/core/services/task.service.ts`** (line 19):
```typescript
private readonly useMocks = false;
```

**`src/app/core/services/sse.service.ts`** (line 8):
```typescript
private readonly useMocks = false;
```

### 2. Start the Backend API

```bash
cd src/Forge.Api/Forge.Api
dotnet run
```

The API will start on `http://localhost:5000`.

### 3. Start the Angular UI

```bash
cd src/Forge.Ui
ng serve
```

The UI will start on `http://localhost:4200`. CORS is pre-configured in the API.

---

## API Reference

Base URL: `http://localhost:5000`

### Repositories

#### List All Repositories

```
GET /api/repositories
```

**Response:** `200 OK`
```typescript
RepositoryDto[]
```

---

#### Get Repository by ID

```
GET /api/repositories/{id}
```

**Parameters:**
- `id` (path, GUID) - Repository identifier

**Response:** `200 OK`
```typescript
RepositoryDto
```

**Errors:**
- `404 Not Found` - Repository does not exist

---

#### Create Repository

```
POST /api/repositories
```

**Request Body:**
```typescript
{
  name: string;
  path: string;
}
```

**Response:** `201 Created`
```typescript
RepositoryDto
```

---

#### Update Repository

```
PATCH /api/repositories/{id}
```

**Parameters:**
- `id` (path, GUID) - Repository identifier

**Request Body:**
```typescript
{
  name?: string;
}
```

**Response:** `200 OK`
```typescript
RepositoryDto
```

---

#### Delete Repository (Soft Delete)

```
DELETE /api/repositories/{id}
```

**Parameters:**
- `id` (path, GUID) - Repository identifier

**Response:** `204 No Content`

---

#### Refresh Repository Git Info

```
POST /api/repositories/{id}/refresh
```

**Parameters:**
- `id` (path, GUID) - Repository identifier

**Response:** `200 OK`
```typescript
RepositoryDto
```

---

#### Set Default Repository

```
POST /api/repositories/{id}/set-default
```

**Parameters:**
- `id` (path, GUID) - Repository identifier

**Response:** `200 OK`
```typescript
RepositoryDto
```

---

### Tasks (Scoped Under Repository)

All task endpoints are scoped under a repository.

#### Get All Tasks for Repository

```
GET /api/repositories/{repositoryId}/tasks
```

**Parameters:**
- `repositoryId` (path, GUID) - Repository identifier

**Response:** `200 OK`
```typescript
TaskDto[]
```

---

#### Get Task by ID

```
GET /api/repositories/{repositoryId}/tasks/{id}
```

**Parameters:**
- `repositoryId` (path, GUID) - Repository identifier
- `id` (path, GUID) - Task identifier

**Response:** `200 OK`
```typescript
TaskDto
```

**Errors:**
- `404 Not Found` - Task does not exist

---

#### Create Task

```
POST /api/repositories/{repositoryId}/tasks
```

**Parameters:**
- `repositoryId` (path, GUID) - Repository identifier

**Request Body:**
```typescript
{
  title: string;
  description: string;
  priority: 'Low' | 'Medium' | 'High' | 'Critical';
}
```

**Response:** `201 Created`
```typescript
TaskDto
```

---

#### Update Task

```
PATCH /api/repositories/{repositoryId}/tasks/{id}
```

**Parameters:**
- `repositoryId` (path, GUID) - Repository identifier
- `id` (path, GUID) - Task identifier

**Request Body:**
```typescript
{
  title?: string;
  description?: string;
  priority?: 'Low' | 'Medium' | 'High' | 'Critical';
}
```

**Response:** `200 OK`
```typescript
TaskDto
```

---

#### Delete Task

```
DELETE /api/repositories/{repositoryId}/tasks/{id}
```

**Parameters:**
- `repositoryId` (path, GUID) - Repository identifier
- `id` (path, GUID) - Task identifier

**Response:** `204 No Content`

---

#### Transition Task State

```
POST /api/repositories/{repositoryId}/tasks/{id}/transition
```

**Parameters:**
- `repositoryId` (path, GUID) - Repository identifier
- `id` (path, GUID) - Task identifier

**Request Body:**
```typescript
{
  targetState: PipelineState;
}
```

**Response:** `200 OK`
```typescript
TaskDto
```

**Errors:**
- `404 Not Found` - Task does not exist
- `400 Bad Request` - Invalid state transition or task has pending human gate

---

#### Get Task Logs

```
GET /api/repositories/{repositoryId}/tasks/{id}/logs
```

**Parameters:**
- `repositoryId` (path, GUID) - Repository identifier
- `id` (path, GUID) - Task identifier

**Response:** `200 OK`
```typescript
TaskLogDto[]
```

---

#### Start Agent on Task

```
POST /api/repositories/{repositoryId}/tasks/{id}/start-agent
```

**Parameters:**
- `repositoryId` (path, GUID) - Repository identifier
- `id` (path, GUID) - Task identifier

**Response:** `200 OK`
```typescript
TaskDto
```

**Errors:**
- `404 Not Found` - Task does not exist
- `400 Bad Request` - Agent is already running on another task

---

#### Abort Agent on Task

```
POST /api/repositories/{repositoryId}/tasks/{id}/abort
```

**Parameters:**
- `repositoryId` (path, GUID) - Repository identifier
- `id` (path, GUID) - Task identifier

**Response:** `200 OK`
```typescript
TaskDto
```

**Errors:**
- `404 Not Found` - Task does not exist
- `400 Bad Request` - No agent running for this task

---

#### Pause Task

```
POST /api/repositories/{repositoryId}/tasks/{id}/pause
```

**Parameters:**
- `repositoryId` (path, GUID) - Repository identifier
- `id` (path, GUID) - Task identifier

**Request Body:**
```typescript
{
  reason?: string;
}
```

**Response:** `200 OK`
```typescript
TaskDto
```

---

#### Resume Task

```
POST /api/repositories/{repositoryId}/tasks/{id}/resume
```

**Parameters:**
- `repositoryId` (path, GUID) - Repository identifier
- `id` (path, GUID) - Task identifier

**Response:** `200 OK`
```typescript
TaskDto
```

---

### Task Artifacts

#### List All Artifacts for Task

```
GET /api/repositories/{repositoryId}/tasks/{id}/artifacts
```

**Response:** `200 OK`
```typescript
ArtifactDto[]
```

---

#### Get Latest Artifact

```
GET /api/repositories/{repositoryId}/tasks/{id}/artifacts/latest
```

**Response:** `200 OK`
```typescript
ArtifactDto
```

---

#### Get Artifacts by State

```
GET /api/repositories/{repositoryId}/tasks/{id}/artifacts/by-state/{state}
```

**Parameters:**
- `state` (path) - Pipeline state to filter by

**Response:** `200 OK`
```typescript
ArtifactDto[]
```

---

### Subtasks

#### List Subtasks

```
GET /api/repositories/{repositoryId}/tasks/{id}/subtasks
```

**Response:** `200 OK`
```typescript
SubtaskDto[]
```

---

#### Create Subtask

```
POST /api/repositories/{repositoryId}/tasks/{id}/subtasks
```

**Request Body:**
```typescript
{
  title: string;
  description: string;
  priority: Priority;
}
```

**Response:** `201 Created`
```typescript
SubtaskDto
```

---

### Human Gates

#### Get All Pending Gates (Cross-Repository)

```
GET /api/gates/pending
```

**Response:** `200 OK`
```typescript
HumanGateDto[]
```

---

#### Resolve Gate

```
POST /api/gates/{id}/resolve
```

**Request Body:**
```typescript
{
  approved: boolean;
  feedback?: string;
}
```

**Response:** `200 OK`
```typescript
HumanGateDto
```

---

### Agent

#### Get Agent Status

```
GET /api/agent/status
```

**Response:** `200 OK`
```typescript
{
  isRunning: boolean;
  currentTaskId?: string;  // GUID as string
  startedAt?: string;      // ISO 8601 datetime
}
```

---

### Scheduler

#### Get Scheduler Status

```
GET /api/scheduler/status
```

**Response:** `200 OK`
```typescript
{
  isEnabled: boolean;
  isAgentRunning: boolean;
  currentTaskId?: string;
  pendingTaskCount: number;
  pausedTaskCount: number;
}
```

---

#### Enable Scheduler

```
POST /api/scheduler/enable
```

**Response:** `200 OK`

---

#### Disable Scheduler

```
POST /api/scheduler/disable
```

**Response:** `200 OK`

---

### Notifications

#### Get Notifications

```
GET /api/notifications?limit=50
```

**Response:** `200 OK`
```typescript
NotificationDto[]
```

---

#### Mark Notification as Read

```
PATCH /api/notifications/{id}/read
```

**Response:** `200 OK`

---

#### Mark All as Read

```
POST /api/notifications/mark-all-read
```

**Response:** `200 OK`
```typescript
{ markedCount: number }
```

---

#### Get Unread Count

```
GET /api/notifications/unread-count
```

**Response:** `200 OK`
```typescript
{ count: number }
```

---

### Events (SSE)

#### Connect to Event Stream

```
GET /api/events
```

**Content-Type:** `text/event-stream`

Establishes a Server-Sent Events connection for real-time updates.

---

## SSE Event Types

The SSE endpoint emits events in the following format:

```
data: {"type":"<event-type>","payload":<payload>,"timestamp":"<ISO-8601>"}
```

### Event Types

| Event Type | Payload | Description |
|------------|---------|-------------|
| `task:created` | `TaskDto` | New task was created |
| `task:updated` | `TaskDto` | Task was modified (state, priority, etc.) |
| `task:deleted` | `{ id: string }` | Task was deleted |
| `task:log` | `TaskLogDto` | New log entry from agent |
| `task:paused` | `TaskDto` | Task was paused (manual or max retries) |
| `task:resumed` | `TaskDto` | Task was resumed |
| `task:split` | `TaskSplitPayload` | Task was split into subtasks |
| `artifact:created` | `ArtifactDto` | Agent produced structured output |
| `humanGate:requested` | `HumanGateDto` | Human gate triggered (low confidence) |
| `humanGate:resolved` | `HumanGateDto` | Human gate approved/rejected |
| `subtask:created` | `SubtaskDto` | Subtask created from split |
| `subtask:started` | `SubtaskDto` | Subtask execution started |
| `subtask:completed` | `SubtaskDto` | Subtask completed successfully |
| `subtask:failed` | `SubtaskDto` | Subtask execution failed |
| `rollback:initiated` | `RollbackDto` | Rollback procedure started |
| `rollback:completed` | `RollbackDto` | Rollback procedure finished |
| `agent:statusChanged` | `AgentStatusDto` | Agent started/stopped |
| `scheduler:taskScheduled` | `TaskDto` | Scheduler picked next task |
| `notification:new` | `NotificationDto` | Notification created |
| `repository:created` | `RepositoryDto` | Repository added |
| `repository:updated` | `RepositoryDto` | Repository modified |
| `repository:deleted` | `{ id: string }` | Repository soft-deleted |

### Example Events

**Task Created:**
```json
{
  "type": "task:created",
  "payload": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "repositoryId": "repo-uuid-here",
    "title": "Implement feature X",
    "description": "Add new functionality...",
    "state": "Backlog",
    "priority": "Medium",
    "assignedAgentId": null,
    "hasError": false,
    "errorMessage": null,
    "isPaused": false,
    "pauseReason": null,
    "retryCount": 0,
    "maxRetries": 3,
    "parentId": null,
    "childCount": 0,
    "derivedState": null,
    "detectedLanguage": null,
    "detectedFramework": null,
    "createdAt": "2025-01-25T10:30:00Z",
    "updatedAt": "2025-01-25T10:30:00Z"
  },
  "timestamp": "2025-01-25T10:30:00Z"
}
```

**Task Log:**
```json
{
  "type": "task:log",
  "payload": {
    "id": "log-uuid",
    "taskId": "task-uuid",
    "type": "toolUse",
    "content": "Reading file: src/main.ts",
    "toolName": "Read",
    "timestamp": "2025-01-25T10:31:00Z"
  },
  "timestamp": "2025-01-25T10:31:00Z"
}
```

**Task Paused:**
```json
{
  "type": "task:paused",
  "payload": {
    "id": "task-uuid",
    "repositoryId": "repo-uuid",
    "title": "Implement feature X",
    "state": "Implementing",
    "isPaused": true,
    "pauseReason": "Maximum retries exceeded",
    "pausedAt": "2025-01-25T10:35:00Z",
    "retryCount": 3,
    "maxRetries": 3
  },
  "timestamp": "2025-01-25T10:35:00Z"
}
```

**Agent Status Changed:**
```json
{
  "type": "agent:statusChanged",
  "payload": {
    "isRunning": true,
    "currentTaskId": "task-uuid",
    "startedAt": "2025-01-25T10:30:00Z"
  },
  "timestamp": "2025-01-25T10:30:00Z"
}
```

**Repository Created:**
```json
{
  "type": "repository:created",
  "payload": {
    "id": "repo-uuid",
    "name": "My Project",
    "path": "/home/user/projects/my-project",
    "isActive": true,
    "branch": "main",
    "commitHash": "abc123",
    "remoteUrl": "https://github.com/user/my-project.git",
    "isDirty": false,
    "isGitRepository": true,
    "lastRefreshedAt": "2025-01-25T10:30:00Z",
    "createdAt": "2025-01-25T10:30:00Z",
    "updatedAt": "2025-01-25T10:30:00Z"
  },
  "timestamp": "2025-01-25T10:30:00Z"
}
```

---

## Data Models

### RepositoryDto

```typescript
interface RepositoryDto {
  id: string;                          // GUID
  name: string;
  path: string;
  isActive: boolean;
  branch?: string;
  commitHash?: string;
  remoteUrl?: string;
  isDirty?: boolean;
  isGitRepository: boolean;
  lastRefreshedAt?: string;            // ISO 8601
  createdAt: string;                   // ISO 8601
  updatedAt: string;                   // ISO 8601
  taskCount: number;
}
```

### TaskDto

```typescript
interface TaskDto {
  id: string;                          // GUID
  repositoryId: string;                // GUID - required association
  title: string;
  description: string;
  state: PipelineState;
  priority: Priority;
  assignedAgentId?: string;
  hasError: boolean;
  errorMessage?: string;
  isPaused: boolean;
  pauseReason?: string;
  pausedAt?: string;                   // ISO 8601
  retryCount: number;
  maxRetries: number;
  createdAt: string;                   // ISO 8601
  updatedAt: string;                   // ISO 8601
  // Hierarchy fields
  parentId?: string;                   // GUID - parent task for subtasks
  childCount: number;
  derivedState?: PipelineState;        // Computed from children's states
  children?: TaskDto[];                // Populated when fetching with hierarchy
  progress?: TaskProgress;             // Completion progress for parent tasks
  // Agent context detection
  detectedLanguage?: string;           // e.g., "csharp", "typescript"
  detectedFramework?: string;          // e.g., "angular", "dotnet"
  recommendedNextState?: PipelineState; // Agent's recommendation
  // Human gate status
  hasPendingGate?: boolean;            // Task blocked by pending human gate
  confidenceScore?: number;            // Agent-reported confidence (0.0-1.0)
  humanInputRequested?: boolean;       // Agent explicitly requested human input
  humanInputReason?: string;           // Reason for human input request
}

interface TaskProgress {
  completed: number;
  total: number;
  percent: number;
}
```

### TaskLogDto

```typescript
interface TaskLogDto {
  id: string;                          // GUID
  taskId: string;                      // GUID
  type: LogType;
  content: string;
  toolName?: string;
  timestamp: string;                   // ISO 8601
}
```

### ArtifactDto

```typescript
interface ArtifactDto {
  id: string;                          // GUID
  taskId: string;                      // GUID
  producedInState: PipelineState;
  artifactType: ArtifactType;
  content: string;
  createdAt: string;                   // ISO 8601
  agentId?: string;
}
```

### HumanGateDto

```typescript
interface HumanGateDto {
  id: string;                          // GUID
  taskId: string;                      // GUID
  gateType: 'split' | 'planning' | 'pr';
  status: 'pending' | 'approved' | 'rejected';
  confidenceScore?: number;
  feedback?: string;
  createdAt: string;                   // ISO 8601
  resolvedAt?: string;                 // ISO 8601
}
```

### NotificationDto

```typescript
interface NotificationDto {
  id: string;                          // GUID
  title: string;
  message: string;
  type: 'info' | 'success' | 'warning' | 'error';
  taskId?: string;                     // GUID - related task
  read: boolean;
  createdAt: string;                   // ISO 8601
}
```

### AgentStatusDto

```typescript
interface AgentStatusDto {
  isRunning: boolean;
  currentTaskId?: string;              // GUID
  startedAt?: string;                  // ISO 8601
}
```

### SchedulerStatusDto

```typescript
interface SchedulerStatusDto {
  isEnabled: boolean;
  isAgentRunning: boolean;
  currentTaskId?: string;              // GUID
  pendingTaskCount: number;
  pausedTaskCount: number;
}
```

### Enums

```typescript
type PipelineState =
  | 'Backlog'
  | 'Split'
  | 'Research'
  | 'Planning'
  | 'Implementing'
  | 'Simplifying'
  | 'Verifying'
  | 'Reviewing'
  | 'PrReady'
  | 'Done';

type Priority = 'Low' | 'Medium' | 'High' | 'Critical';

type LogType = 'info' | 'toolUse' | 'toolResult' | 'error' | 'thinking';

type ArtifactType =
  | 'task_split'
  | 'research_findings'
  | 'plan'
  | 'implementation'
  | 'simplification_review'
  | 'verification_report'
  | 'review'
  | 'test'
  | 'general';
```

---

## Implementation Checklist

### Required Changes

- [ ] Set `useMocks = false` in `task.service.ts`
- [ ] Set `useMocks = false` in `sse.service.ts`
- [ ] Set `useMocks = false` in `repository.service.ts`
- [ ] Set `useMocks = false` in `notification.service.ts`

### Verified Functionality

All services already implement the multi-repository API structure:

- **TaskService**: All methods require `repositoryId` parameter
- **RepositoryService**: Full CRUD, refresh, set-default operations
- **NotificationService**: Get, mark read, mark all read, unread count
- **SchedulerService**: Status, enable, disable
- **ArtifactService**: List, latest, by-state
- **AgentService**: Status endpoint
- **SseService**: SSE connection with all event types

---

## Angular Proxy Configuration (Optional)

If you encounter CORS issues during development, create `proxy.conf.json`:

```json
{
  "/api": {
    "target": "http://localhost:5000",
    "secure": false,
    "changeOrigin": true
  }
}
```

Update `angular.json` serve configuration:

```json
{
  "serve": {
    "options": {
      "proxyConfig": "proxy.conf.json"
    }
  }
}
```

---

## Verification Steps

1. **Start API:**
   ```bash
   cd src/Forge.Api/Forge.Api && dotnet run
   ```

2. **Start UI:**
   ```bash
   cd src/Forge.Ui && ng serve
   ```

3. **Verify in Browser DevTools:**
   - Open Network tab
   - Check that API calls go to `localhost:5000/api/*`
   - Verify SSE connection is established (`/api/events`)
   - Confirm tasks load from the database

4. **Test Operations:**
   - Add a repository
   - Create a new task
   - Update task details
   - Transition task through states
   - Start agent on a task
   - View agent logs in real-time
   - Pause/resume a task
   - View notifications
