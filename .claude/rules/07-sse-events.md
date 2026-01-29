# SSE Event Architecture

## Protocol

- **Protocol**: EventSource/SSE (not WebSocket)
- **Payload**: Full state on each event (not deltas)
- **Endpoint**: `GET /api/events`

## Event Types

### Backlog Item Events
| Event Type | Payload | Trigger |
|------------|---------|---------|
| `backlogItem:created` | `BacklogItemDto` | Backlog item creation |
| `backlogItem:updated` | `BacklogItemDto` | Backlog item modification, state transition |
| `backlogItem:deleted` | `{ id: Guid }` | Backlog item deletion |
| `backlogItem:paused` | `BacklogItemDto` | Backlog item paused |
| `backlogItem:resumed` | `BacklogItemDto` | Backlog item resumed |
| `backlogItem:log` | `BacklogLogDto` | Agent output during refinement/splitting |

### Task Events
| Event Type | Payload | Trigger |
|------------|---------|---------|
| `task:created` | `TaskDto` | Task creation (from split) |
| `task:updated` | `TaskDto` | Task modification, state transition, agent assignment |
| `task:deleted` | `{ id: Guid }` | Task deletion |
| `task:log` | `TaskLogDto` | Agent output during execution |
| `task:paused` | `TaskDto` | Task auto-paused after max retries or manual pause |
| `task:resumed` | `TaskDto` | Task resumed from paused state |

### Common Events
| Event Type | Payload | Trigger |
|------------|---------|---------|
| `artifact:created` | `ArtifactDto` | Agent produces structured output |
| `humanGate:requested` | `HumanGateDto` | Human gate triggered (confidence < threshold) |
| `humanGate:resolved` | `HumanGateDto` | Human gate approved/rejected |
| `agent:statusChanged` | `AgentStatusDto` | Agent starts/stops |
| `scheduler:itemScheduled` | `BacklogItemDto` | Scheduler picks next backlog item |
| `scheduler:taskScheduled` | `TaskDto` | Scheduler picks next task |
| `notification:new` | `NotificationDto` | Notification created |
| `repository:created` | `RepositoryDto` | Repository added |
| `repository:updated` | `RepositoryDto` | Repository modified (name, git info refresh) |
| `repository:deleted` | `{ id: Guid }` | Repository soft-deleted |
| `agentQuestion:requested` | `AgentQuestionDto` | Agent uses AskUserQuestion tool |
| `agentQuestion:answered` | `AgentQuestionDto` | User submits answer |
| `agentQuestion:timeout` | `AgentQuestionDto` | Question times out |
| `agentQuestion:cancelled` | `{ id: Guid }` | Backlog item or task aborted while waiting |

## Backend Emission Points

| Service | Events |
|---------|--------|
| `BacklogService` | `backlogItem:created`, `backlogItem:updated`, `backlogItem:deleted`, `backlogItem:log` |
| `TaskService` | `task:created`, `task:updated`, `task:deleted`, `task:log` |
| `AgentRunnerService` | `agent:statusChanged`, logs via BacklogService/TaskService |
| `OrchestratorService` | `artifact:created` (via SseService) |
| `SchedulerService` | `*:paused`, `*:resumed`, `humanGate:requested`, auto-transitions |
| `HumanGateService` | `humanGate:resolved` |
| `TaskSchedulerService` | `scheduler:itemScheduled`, `scheduler:taskScheduled` |
| `NotificationService` | `notification:new` |
| `RepositoryService` | `repository:created`, `repository:updated`, `repository:deleted` |
| `AgentQuestionService` | `agentQuestion:*` events |

## Frontend Consumption

- SseService connects to `/api/events`
- BacklogListComponent and BacklogItemDetailComponent subscribe to backlog events
- QueueComponent and TaskDetailComponent subscribe to task events
- Signal stores (BacklogStore, TaskStore, LogStore, NotificationStore, RepositoryStore) update from events
