# SSE Event Architecture

## Protocol

- **Protocol**: EventSource/SSE (not WebSocket)
- **Payload**: Full state on each event (not deltas)
- **Endpoint**: `GET /api/events`

## Event Types

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
| `repository:created` | `RepositoryDto` | Repository added |
| `repository:updated` | `RepositoryDto` | Repository modified (name, git info refresh) |
| `repository:deleted` | `{ id: Guid }` | Repository soft-deleted |
| `agentQuestion:requested` | `AgentQuestionDto` | Agent uses AskUserQuestion tool |
| `agentQuestion:answered` | `AgentQuestionDto` | User submits answer |
| `agentQuestion:timeout` | `AgentQuestionDto` | Question times out |
| `agentQuestion:cancelled` | `{ id: Guid }` | Task aborted while waiting |

## Backend Emission Points

| Service | Events |
|---------|--------|
| `TaskService` | `task:created`, `task:updated`, `task:deleted`, `task:log` |
| `AgentRunnerService` | `agent:statusChanged`, logs via TaskService |
| `OrchestratorService` | `artifact:created` (via SseService) |
| `SchedulerService` | `task:paused`, `task:resumed`, `humanGate:requested`, auto-transitions |
| `HumanGateService` | `humanGate:resolved` |
| `SubtaskService` | `subtask:*` events |
| `RollbackService` | `rollback:*` events |
| `TaskSchedulerService` | `scheduler:taskScheduled` |
| `NotificationService` | `notification:new` |
| `RepositoryService` | `repository:created`, `repository:updated`, `repository:deleted` |
| `AgentQuestionService` | `agentQuestion:*` events |

## Frontend Consumption

- SseService connects to `/api/events`
- BoardComponent and TaskDetailComponent subscribe to events
- Signal stores (TaskStore, LogStore, NotificationStore, RepositoryStore) update from events
