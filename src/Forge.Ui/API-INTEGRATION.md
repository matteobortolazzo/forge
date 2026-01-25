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

### Tasks

#### Get All Tasks

```
GET /api/tasks
```

**Response:** `200 OK`
```typescript
TaskDto[]
```

---

#### Get Task by ID

```
GET /api/tasks/{id}
```

**Parameters:**
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
POST /api/tasks
```

**Request Body:**
```typescript
{
  title: string;
  description: string;
  priority: 'low' | 'medium' | 'high' | 'critical';
}
```

**Response:** `201 Created`
```typescript
TaskDto
```

---

#### Update Task

```
PATCH /api/tasks/{id}
```

**Parameters:**
- `id` (path, GUID) - Task identifier

**Request Body:**
```typescript
{
  title?: string;
  description?: string;
  priority?: 'low' | 'medium' | 'high' | 'critical';
}
```

**Response:** `200 OK`
```typescript
TaskDto
```

**Errors:**
- `404 Not Found` - Task does not exist

---

#### Delete Task

```
DELETE /api/tasks/{id}
```

**Parameters:**
- `id` (path, GUID) - Task identifier

**Response:** `204 No Content`

**Errors:**
- `404 Not Found` - Task does not exist

---

#### Transition Task State

```
POST /api/tasks/{id}/transition
```

**Parameters:**
- `id` (path, GUID) - Task identifier

**Request Body:**
```typescript
{
  targetState: 'Backlog' | 'Planning' | 'Implementing' | 'Reviewing' | 'Testing' | 'PrReady' | 'Done';
}
```

**Response:** `200 OK`
```typescript
TaskDto
```

**Errors:**
- `404 Not Found` - Task does not exist
- `400 Bad Request` - Invalid state transition

---

#### Get Task Logs

```
GET /api/tasks/{id}/logs
```

**Parameters:**
- `id` (path, GUID) - Task identifier

**Response:** `200 OK`
```typescript
TaskLogDto[]
```

**Errors:**
- `404 Not Found` - Task does not exist

---

#### Start Agent on Task

```
POST /api/tasks/{id}/start-agent
```

**Parameters:**
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
POST /api/tasks/{id}/abort
```

**Parameters:**
- `id` (path, GUID) - Task identifier

**Response:** `200 OK`
```typescript
TaskDto
```

**Errors:**
- `404 Not Found` - Task does not exist
- `400 Bad Request` - No agent running for this task

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
| `task:updated` | `TaskDto` | Task was modified |
| `task:deleted` | `{ id: string }` | Task was deleted |
| `task:log` | `TaskLogDto` | New log entry from agent |
| `agent:statusChanged` | `AgentStatusDto` | Agent started/stopped |

### Example Events

**Task Created:**
```json
{
  "type": "task:created",
  "payload": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "title": "Implement feature X",
    "description": "Add new functionality...",
    "state": "Backlog",
    "priority": "medium",
    "assignedAgentId": null,
    "hasError": false,
    "errorMessage": null,
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

---

## Data Models

### TaskDto

```typescript
interface TaskDto {
  id: string;                          // GUID
  title: string;
  description: string;
  state: PipelineState;
  priority: Priority;
  assignedAgentId: string | null;
  hasError: boolean;
  errorMessage: string | null;
  createdAt: string;                   // ISO 8601
  updatedAt: string;                   // ISO 8601
}
```

### TaskLogDto

```typescript
interface TaskLogDto {
  id: string;                          // GUID
  taskId: string;                      // GUID
  type: LogType;
  content: string;
  toolName: string | null;
  timestamp: string;                   // ISO 8601
}
```

### AgentStatusDto

```typescript
interface AgentStatusDto {
  isRunning: boolean;
  currentTaskId: string | null;        // GUID
  startedAt: string | null;            // ISO 8601
}
```

### Enums

```typescript
type PipelineState =
  | 'Backlog'
  | 'Planning'
  | 'Implementing'
  | 'Reviewing'
  | 'Testing'
  | 'PrReady'
  | 'Done';

type Priority = 'low' | 'medium' | 'high' | 'critical';

type LogType = 'info' | 'toolUse' | 'toolResult' | 'error' | 'thinking';
```

---

## Implementation Checklist

### Required Changes

- [ ] Set `useMocks = false` in `task.service.ts`
- [ ] Set `useMocks = false` in `sse.service.ts`

### Recommended Additions

- [ ] Add `startAgent(taskId: string)` method to `TaskService`:

```typescript
startAgent(taskId: string): Observable<Task> {
  if (this.useMocks) {
    // Mock implementation
    const index = this.mockTasks.findIndex(t => t.id === taskId);
    if (index === -1) {
      return throwError(() => new Error('Task not found'));
    }
    const updatedTask: Task = {
      ...this.mockTasks[index],
      assignedAgentId: `agent-${Date.now()}`,
      updatedAt: new Date(),
    };
    this.mockTasks[index] = updatedTask;
    return of({ ...updatedTask }).pipe(delay(300));
  }
  return this.http.post<Task>(`${this.apiUrl}/${taskId}/start-agent`, {});
}
```

- [ ] Add `getAgentStatus()` method to a new `AgentService`:

```typescript
@Injectable({ providedIn: 'root' })
export class AgentService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/agent';

  getStatus(): Observable<AgentStatus> {
    return this.http.get<AgentStatus>(`${this.apiUrl}/status`);
  }
}
```

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
   - Create a new task
   - Update task details
   - Transition task through states
   - Start agent on a task
   - View agent logs in real-time
