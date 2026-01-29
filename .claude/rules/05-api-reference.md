# API Reference

## Repositories
```
GET    /api/repositories                    # List all active repositories
GET    /api/repositories/{id}               # Get repository with git info
POST   /api/repositories                    # Add repository
PATCH  /api/repositories/{id}               # Update repository name
DELETE /api/repositories/{id}               # Soft delete (set IsActive=false)
POST   /api/repositories/{id}/refresh       # Refresh cached git info
POST   /api/repositories/{id}/set-default   # Set as default repository
```

## Tasks (scoped under repository)
```
GET    /api/repositories/{repoId}/tasks                   # List tasks for repository
GET    /api/repositories/{repoId}/tasks/{id}              # Get task details
POST   /api/repositories/{repoId}/tasks                   # Create new task
PATCH  /api/repositories/{repoId}/tasks/{id}              # Update task
DELETE /api/repositories/{repoId}/tasks/{id}              # Delete task
POST   /api/repositories/{repoId}/tasks/{id}/transition   # Transition to new state
GET    /api/repositories/{repoId}/tasks/{id}/logs         # Get task logs
POST   /api/repositories/{repoId}/tasks/{id}/abort        # Abort assigned agent
POST   /api/repositories/{repoId}/tasks/{id}/start-agent  # Start agent execution for task
POST   /api/repositories/{repoId}/tasks/{id}/pause        # Pause task from automatic scheduling
POST   /api/repositories/{repoId}/tasks/{id}/resume       # Resume paused task
```

## Task Artifacts
```
GET    /api/repositories/{repoId}/tasks/{id}/artifacts              # List all artifacts for task
GET    /api/repositories/{repoId}/tasks/{id}/artifacts/{aid}        # Get specific artifact by ID
GET    /api/repositories/{repoId}/tasks/{id}/artifacts/latest       # Get most recent artifact
GET    /api/repositories/{repoId}/tasks/{id}/artifacts/by-state/{state}  # Filter artifacts by pipeline state
```

## Subtasks
```
GET    /api/repositories/{repoId}/tasks/{id}/subtasks               # List all subtasks for a task
GET    /api/repositories/{repoId}/tasks/{id}/subtasks/{sid}         # Get specific subtask
POST   /api/repositories/{repoId}/tasks/{id}/subtasks               # Create subtask
PATCH  /api/repositories/{repoId}/tasks/{id}/subtasks/{sid}/status  # Update subtask status
DELETE /api/repositories/{repoId}/tasks/{id}/subtasks/{sid}         # Delete subtask
```

## Human Gates
```
GET    /api/gates/pending                                 # Get all pending human gates (cross-repo)
GET    /api/gates/{id}                                    # Get specific gate
POST   /api/gates/{id}/resolve                            # Resolve gate (approve/reject)
GET    /api/repositories/{repoId}/tasks/{id}/gates        # Get all gates for a task
```

## Agent
```
GET    /api/agent/status          # Get current agent status
```

## Agent Questions
```
GET    /api/agent/questions/pending       # Get current pending question (if any)
GET    /api/agent/questions/{id}          # Get specific question by ID
POST   /api/agent/questions/{id}/answer   # Submit answer to question
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
