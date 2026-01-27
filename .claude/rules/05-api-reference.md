# API Reference

## Tasks
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

## Task Artifacts
```
GET    /api/tasks/{id}/artifacts              # List all artifacts for task
GET    /api/tasks/{id}/artifacts/{aid}        # Get specific artifact by ID
GET    /api/tasks/{id}/artifacts/latest       # Get most recent artifact
GET    /api/tasks/{id}/artifacts/by-state/{state}  # Filter artifacts by pipeline state
```

## Subtasks
```
GET    /api/tasks/{id}/subtasks               # List all subtasks for a task
GET    /api/tasks/{id}/subtasks/{sid}         # Get specific subtask
POST   /api/tasks/{id}/subtasks               # Create subtask
PATCH  /api/tasks/{id}/subtasks/{sid}/status  # Update subtask status
DELETE /api/tasks/{id}/subtasks/{sid}         # Delete subtask
```

## Human Gates
```
GET    /api/gates/pending                     # Get all pending human gates
GET    /api/gates/{id}                        # Get specific gate
POST   /api/gates/{id}/resolve                # Resolve gate (approve/reject)
GET    /api/tasks/{id}/gates                  # Get all gates for a task
```

## Agent
```
GET    /api/agent/status          # Get current agent status
```

## Scheduler
```
GET    /api/scheduler/status      # Get scheduler status (enabled, agent running, pending/paused counts)
POST   /api/scheduler/enable      # Enable automatic task scheduling
POST   /api/scheduler/disable     # Disable automatic task scheduling
```

## Events
```
GET    /api/events                # EventSource/SSE connection
```

## Notifications
```
GET    /api/notifications              # Get recent notifications (?limit=N)
PATCH  /api/notifications/{id}/read    # Mark as read
POST   /api/notifications/mark-all-read # Mark all as read
GET    /api/notifications/unread-count  # Get unread count
```

## Mock (E2E Only - when CLAUDE_MOCK_MODE=true)
```
GET    /api/mock/status              # Get mock configuration status
GET    /api/mock/scenarios           # List available scenarios
POST   /api/mock/scenario            # Set default or pattern-specific scenario
DELETE /api/mock/scenario/{pattern}  # Remove pattern mapping
POST   /api/mock/reset               # Reset to defaults
```
