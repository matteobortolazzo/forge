# AI Agent Dashboard - MVP Task List

**Project**: AI Agent Orchestration Dashboard
**Phase**: Phase 1 - Foundation (MVP)
**Last Updated**: 2026-01-24

---

## Task Organization

Tasks are organized into logical groupings that can be worked on in parallel by different agents. Dependencies are explicitly noted.

**Legend**:
- 🔴 Critical Path - Must be completed first
- 🟡 Backend Task
- 🔵 Frontend Task
- 🟢 Integration Task
- ⚪ Documentation/DevOps

---

## 1. Project Infrastructure Setup 🔴

### INFRA-001: Initialize Nx Monorepo
**Priority**: CRITICAL
**Type**: Infrastructure
**Estimated Tokens**: 3,000

**Description**: Set up the base Nx monorepo structure with Angular and NestJS applications.

**Acceptance Criteria**:
- [ ] Nx workspace created with Angular preset
- [ ] Frontend Angular 21 app configured
- [ ] Backend NestJS app added and configured
- [ ] Shared library created for common types/interfaces
- [ ] Base folder structure follows Nx best practices

**Commands**:
```bash
npx create-nx-workspace@latest agent-dashboard --preset=angular-monorepo --appName=frontend
nx add @nx/nest
nx g @nx/nest:app backend
nx g @nx/js:lib shared
```

**Technical Constraints**:
- Use Angular 21.x
- Use NestJS 11.x
- Use Nx 22.x

---

### INFRA-002: Configure Prisma and Database
**Priority**: CRITICAL
**Type**: Infrastructure
**Dependencies**: INFRA-001
**Estimated Tokens**: 4,000

**Description**: Set up Prisma ORM with PostgreSQL and implement the complete database schema.

**Acceptance Criteria**:
- [ ] Prisma initialized in backend app
- [ ] Complete schema.prisma file implemented (Task, TaskLog, Config, Notification models)
- [ ] All enums defined (PipelineState, Priority, AgentType, LogType, NotificationType)
- [ ] Database migrations created and applied
- [ ] Prisma Client generated and working
- [ ] SQLite configured for development, PostgreSQL for production

**Files to Create**:
- `apps/backend/prisma/schema.prisma`
- `apps/backend/prisma/migrations/`

**Related Documentation**:
- Prisma schema specification in SPEC.md lines 2117-2281

**Technical Constraints**:
- Support both SQLite (dev) and PostgreSQL (prod)
- Include proper indexes for performance
- 30-day retention policy on TaskLog

---

### INFRA-003: Environment Configuration
**Priority**: CRITICAL
**Type**: Infrastructure
**Dependencies**: INFRA-001
**Estimated Tokens**: 2,000

**Description**: Set up environment variables and configuration management for both frontend and backend.

**Acceptance Criteria**:
- [ ] .env.example file created with all required variables
- [ ] ConfigModule configured in NestJS
- [ ] Environment validation implemented
- [ ] Development and production configurations separated
- [ ] Documentation for environment setup

**Environment Variables**:
```
DATABASE_URL
ANTHROPIC_API_KEY
REPOSITORY_PATH
GITHUB_TOKEN
PORT
NODE_ENV
DAILY_TOKEN_BUDGET
MONTHLY_TOKEN_BUDGET
MAX_CONCURRENT_AGENTS
PROTECTED_PATHS
```

**Related Documentation**:
- SPEC.md lines 2385-2411

---

### INFRA-004: Install Core Dependencies
**Priority**: CRITICAL
**Type**: Infrastructure
**Dependencies**: INFRA-001
**Estimated Tokens**: 2,000

**Description**: Install and configure all required npm packages for both frontend and backend.

**Acceptance Criteria**:
- [ ] Backend dependencies installed (Prisma, simple-git, @anthropic-ai/claude-agent-sdk)
- [ ] Frontend dependencies installed (@angular/cdk, Tailwind CSS)
- [ ] Tailwind configured in Angular app
- [ ] Package.json scripts configured for development workflow
- [ ] All dependencies compatible and tested

**Packages**:
```bash
# Backend
npm install @anthropic-ai/claude-agent-sdk
npm install @prisma/client prisma
npm install simple-git
npm install class-validator class-transformer

# Frontend
npm install @angular/cdk
npm install -D tailwindcss postcss autoprefixer
```

**Technical Constraints**:
- Angular 21+ with standalone components
- Tailwind CSS 4.x
- Latest stable versions of all packages

---

## 2. Backend - Data Layer 🟡

### BE-DATA-001: Task CRUD Service
**Priority**: HIGH
**Type**: Backend
**Dependencies**: INFRA-002
**Estimated Tokens**: 5,000

**Description**: Implement TaskService with full CRUD operations and structured field support.

**Acceptance Criteria**:
- [ ] TaskService created with dependency injection
- [ ] Create task with all structured fields (description, acceptanceCriteria, edgeCases, etc.)
- [ ] Read task by ID with relationships (parent, children)
- [ ] Update task with validation
- [ ] Delete task (only if no active agent)
- [ ] List all tasks with filtering by state
- [ ] Proper error handling for all operations

**Files to Create**:
- `apps/backend/src/tasks/tasks.service.ts`
- `apps/backend/src/tasks/dto/create-task.dto.ts`
- `apps/backend/src/tasks/dto/update-task.dto.ts`

**Technical Constraints**:
- Use Prisma for database operations
- Validate DTOs with class-validator
- Include proper TypeScript typing

---

### BE-DATA-002: Task REST API Endpoints
**Priority**: HIGH
**Type**: Backend
**Dependencies**: BE-DATA-001
**Estimated Tokens**: 4,000

**Description**: Implement REST API endpoints for task management.

**Acceptance Criteria**:
- [ ] GET /api/tasks - List all tasks
- [ ] GET /api/tasks/:id - Get task details with relationships
- [ ] POST /api/tasks - Create new task (includes cost estimation)
- [ ] PATCH /api/tasks/:id - Update task fields
- [ ] DELETE /api/tasks/:id - Delete task with validation
- [ ] POST /api/tasks/:id/transition - State transition endpoint
- [ ] GET /api/tasks/:id/logs - Get task logs with filters
- [ ] POST /api/tasks/:id/abort - Abort assigned agent
- [ ] Proper HTTP status codes and error responses

**Files to Create**:
- `apps/backend/src/tasks/tasks.controller.ts`

**API Specification**: SPEC.md lines 696-708

---

### BE-DATA-003: Config Service
**Priority**: MEDIUM
**Type**: Backend
**Dependencies**: INFRA-002
**Estimated Tokens**: 3,000

**Description**: Implement configuration management service for dashboard settings.

**Acceptance Criteria**:
- [ ] ConfigService reads from database and environment
- [ ] GET /api/config endpoint
- [ ] PATCH /api/config endpoint (with restart warning)
- [ ] Default values for all settings
- [ ] Validation for configuration updates
- [ ] Support for budget, concurrency, and repository settings

**Files to Create**:
- `apps/backend/src/config/config.service.ts`
- `apps/backend/src/config/config.controller.ts`

**Default Configuration**:
- maxConcurrentAgents: 3
- dailyTokenBudget: 100,000
- monthlyTokenBudget: 5,000,000
- defaultBranch: "main"
- branchPrefix: "feature/agent-"

---

### BE-DATA-004: Notification Service
**Priority**: MEDIUM
**Type**: Backend
**Dependencies**: INFRA-002
**Estimated Tokens**: 2,500

**Description**: Implement notification system for task state changes and budget alerts.

**Acceptance Criteria**:
- [ ] NotificationService for creating and managing notifications
- [ ] GET /api/notifications - List recent notifications
- [ ] PATCH /api/notifications/:id/read - Mark as read
- [ ] Notification types: TASK_STATE_CHANGE, PR_CREATED, PR_MERGED, BUDGET_WARNING, BUDGET_EXCEEDED
- [ ] Integration with EventsGateway for real-time delivery

**Files to Create**:
- `apps/backend/src/notifications/notifications.service.ts`
- `apps/backend/src/notifications/notifications.controller.ts`

**Related Specification**: SPEC.md lines 258-276

---

## 3. Backend - Agent System 🟡

### BE-AGENT-001: Agent Pool Service Foundation
**Priority**: CRITICAL
**Type**: Backend
**Dependencies**: BE-DATA-001, INFRA-003
**Estimated Tokens**: 8,000

**Description**: Implement the core AgentPoolService for managing concurrent agent execution.

**Acceptance Criteria**:
- [ ] AgentPoolService with concurrency control
- [ ] Priority queue implementation (no preemption)
- [ ] Agent instance tracking (Map of active agents)
- [ ] Single orchestrator lock mechanism
- [ ] spawnAgent() method with budget checking
- [ ] completeAgent() method with cleanup
- [ ] getPoolStatus() for monitoring
- [ ] Basic error handling structure

**Files to Create**:
- `apps/backend/src/agents/agent-pool.service.ts`
- `apps/backend/src/agents/models/agent-instance.ts`
- `apps/backend/src/agents/models/priority-queue.ts`

**Technical Constraints**:
- Max concurrent agents: configurable (default 3)
- Single orchestrator instance only
- Non-preemptive priority queue

**Related Specification**: SPEC.md lines 1299-1598

---

### BE-AGENT-002: Agent Definitions and Runner
**Priority**: HIGH
**Type**: Backend
**Dependencies**: BE-AGENT-001
**Estimated Tokens**: 6,000

**Description**: Implement agent definitions and the agent runner service for executing Claude SDK agents.

**Acceptance Criteria**:
- [ ] AgentDefinition interface and registry
- [ ] Orchestrator agent definition (Opus)
- [ ] UI Agent definition (Sonnet)
- [ ] API Agent definition (Sonnet)
- [ ] AgentRunnerService for executing SDK agents
- [ ] Prompt building logic with task context
- [ ] Tool permission configuration (promptOnDemand mode)
- [ ] Abort controller integration

**Files to Create**:
- `apps/backend/src/agents/definitions/orchestrator.agent.ts`
- `apps/backend/src/agents/definitions/ui.agent.ts`
- `apps/backend/src/agents/definitions/api.agent.ts`
- `apps/backend/src/agents/agent-runner.service.ts`

**Agent Definitions**: SPEC.md lines 280-464

---

### BE-AGENT-003: Agent Execution and Message Processing
**Priority**: HIGH
**Type**: Backend
**Dependencies**: BE-AGENT-002
**Estimated Tokens**: 7,000

**Description**: Implement agent execution logic with real-time streaming and token tracking.

**Acceptance Criteria**:
- [ ] runAgent() method with SDK query streaming
- [ ] processMessage() for handling SDK messages
- [ ] Token tracking and budget updates during execution
- [ ] Log creation and streaming (hybrid: stream thinking, batch tools)
- [ ] Current action extraction and agent status updates
- [ ] Auto-extend budget with notifications
- [ ] Message formatting for different log types

**Files to Create**:
- `apps/backend/src/agents/services/message-processor.service.ts`

**Technical Constraints**:
- Stream thinking logs in real-time
- Batch tool results
- Update token counts on every message

---

### BE-AGENT-004: Error Handling and Retry Logic
**Priority**: MEDIUM
**Type**: Backend
**Dependencies**: BE-AGENT-003
**Estimated Tokens**: 5,000

**Description**: Implement comprehensive error handling with configurable retry strategies.

**Acceptance Criteria**:
- [ ] ErrorClassifierService for categorizing errors
- [ ] handleAgentError() with retry decision logic
- [ ] Configurable retry policies per error type
- [ ] Max retry count tracking on tasks
- [ ] Task state updates on errors (hasError, errorMessage)
- [ ] Re-queue logic for retriable errors
- [ ] BLOCKED state for non-retriable errors
- [ ] Error notifications

**Files to Create**:
- `apps/backend/src/agents/services/error-classifier.service.ts`

**Error Types**:
- NETWORK (3 retries)
- TIMEOUT (2 retries)
- RATE_LIMIT (5 retries)
- LOGIC_ERROR (0 retries)
- TOOL_ERROR (1 retry)
- UNKNOWN (1 retry)

---

### BE-AGENT-005: Agent REST Endpoints
**Priority**: MEDIUM
**Type**: Backend
**Dependencies**: BE-AGENT-001
**Estimated Tokens**: 3,000

**Description**: Implement REST API endpoints for agent management and control.

**Acceptance Criteria**:
- [ ] GET /api/agents - List agent instances and status
- [ ] GET /api/agents/:id - Get agent details
- [ ] POST /api/agents/pause-all - Pause entire pipeline
- [ ] POST /api/agents/resume-all - Resume entire pipeline
- [ ] POST /api/tasks/:id/pause - Pause specific task
- [ ] POST /api/tasks/:id/resume - Resume specific task
- [ ] Proper authorization and validation

**Files to Create**:
- `apps/backend/src/agents/agents.controller.ts`

**API Specification**: SPEC.md lines 711-719

---

## 4. Backend - Orchestration & Budget 🟡

### BE-ORCH-001: Task Orchestration Service
**Priority**: HIGH
**Type**: Backend
**Dependencies**: BE-AGENT-001, BE-DATA-001
**Estimated Tokens**: 8,000

**Description**: Implement TaskOrchestrationService for managing pipeline state transitions and agent coordination.

**Acceptance Criteria**:
- [ ] transitionTask() with validation
- [ ] triggerAgentForState() mapping
- [ ] handleOrchestratorResult() for subtask creation
- [ ] Subtask nesting validation (max 2 levels)
- [ ] Dependency tracking and blocking logic
- [ ] Priority inheritance for subtasks
- [ ] MVP: One implementation task at a time check
- [ ] Notification creation on state changes

**Files to Create**:
- `apps/backend/src/orchestration/task-orchestration.service.ts`

**State Transitions**:
- BACKLOG → PLANNING → IMPLEMENTING → REVIEWING → TESTING → PR_READY → DONE
- Manual BLOCKED state

**Related Specification**: SPEC.md lines 1601-1880

---

### BE-ORCH-002: Subtask Management
**Priority**: MEDIUM
**Type**: Backend
**Dependencies**: BE-ORCH-001
**Estimated Tokens**: 5,000

**Description**: Implement subtask completion handling and dependency resolution.

**Acceptance Criteria**:
- [ ] handleSubtaskCompletion() method
- [ ] Check if all sibling subtasks are done
- [ ] Auto-advance parent task when subtasks complete
- [ ] Unblock dependent subtasks on completion
- [ ] Remove completed tasks from blockedBy arrays
- [ ] Spawn agents for newly unblocked tasks
- [ ] Handle revision subtasks from review/test failures

**Technical Constraints**:
- Strict completion check (all siblings must be DONE)
- Max 2 levels of nesting enforced

---

### BE-ORCH-003: Cost Estimator Service
**Priority**: HIGH
**Type**: Backend
**Dependencies**: INFRA-003
**Estimated Tokens**: 4,000

**Description**: Implement cost estimation service using Haiku model to estimate task token usage.

**Acceptance Criteria**:
- [ ] CostEstimatorService with estimate() method
- [ ] Use Haiku model for estimation
- [ ] Consider task complexity factors
- [ ] Breakdown by agent type
- [ ] Confidence level calculation
- [ ] Integration with task creation
- [ ] Conservative estimation (slightly over-estimate)

**Files to Create**:
- `apps/backend/src/agents/services/cost-estimator.service.ts`

**Output Format**:
```json
{
  "estimatedTokens": 15000,
  "breakdown": {
    "orchestrator": 3000,
    "implementation": 8000,
    "simplifier": 500,
    "review": 2000,
    "testing": 1500
  },
  "confidence": "medium",
  "reasoning": "Task has moderate complexity..."
}
```

**Related Specification**: SPEC.md lines 657-688

---

### BE-BUDGET-001: Budget Service Implementation
**Priority**: HIGH
**Type**: Backend
**Dependencies**: BE-DATA-003
**Estimated Tokens**: 5,000

**Description**: Implement budget tracking and enforcement with auto-extend and notifications.

**Acceptance Criteria**:
- [ ] recordUsage() method with threshold checking
- [ ] Budget warning notifications at 80%
- [ ] Budget exceeded notifications at 100%
- [ ] Auto-extend budget capability
- [ ] isExceeded() check for agent spawning
- [ ] wouldExceed() prediction for mid-execution
- [ ] getStatus() for real-time display
- [ ] Daily and monthly usage tracking

**Files to Create**:
- `apps/backend/src/budget/budget.service.ts`

**Technical Constraints**:
- Global daily and monthly limits
- Hard limits pause new agent spawns
- Warnings at 80% threshold

**Related Specification**: SPEC.md lines 1949-2044

---

### BE-BUDGET-002: Budget Reset Cron Jobs
**Priority**: LOW
**Type**: Backend
**Dependencies**: BE-BUDGET-001
**Estimated Tokens**: 2,000

**Description**: Implement scheduled jobs for daily and monthly budget resets.

**Acceptance Criteria**:
- [ ] Daily reset cron job (midnight)
- [ ] Monthly reset cron job (1st of month)
- [ ] resetDaily() method implementation
- [ ] resetMonthly() method implementation
- [ ] Proper scheduling configuration
- [ ] Logging for reset operations

**Files to Create**:
- `apps/backend/src/budget/budget-scheduler.service.ts`

**Technical Constraints**:
- Use NestJS @nestjs/schedule
- UTC timezone for consistency

---

## 5. Backend - Real-time Communication 🟡

### BE-SSE-001: Events Gateway Foundation
**Priority**: CRITICAL
**Type**: Backend
**Dependencies**: INFRA-001
**Estimated Tokens**: 5,000

**Description**: Implement Server-Sent Events (SSE) gateway for real-time updates to frontend.

**Acceptance Criteria**:
- [ ] EventsGateway service with SSE endpoint
- [ ] GET /api/events connection handler
- [ ] Client subscription management
- [ ] Heartbeat/keep-alive implementation
- [ ] Connection cleanup on client disconnect
- [ ] Proper CORS configuration
- [ ] Buffer management for slow clients

**Files to Create**:
- `apps/backend/src/events/events.gateway.ts`
- `apps/backend/src/events/events.module.ts`

**Technical Constraints**:
- Use EventSource protocol
- Send full state (not deltas)
- Handle slow client disconnections gracefully

**Connection Endpoint**: GET /api/events

---

### BE-SSE-002: Event Emission Methods
**Priority**: HIGH
**Type**: Backend
**Dependencies**: BE-SSE-001
**Estimated Tokens**: 4,000

**Description**: Implement event emission methods for all real-time update types.

**Acceptance Criteria**:
- [ ] emitTaskCreated(task)
- [ ] emitTaskUpdated(task)
- [ ] emitTaskDeleted(taskId)
- [ ] emitTaskLog(taskId, log)
- [ ] emitAgentStatusChanged(agent)
- [ ] emitAgentAssigned(agentId, taskId)
- [ ] emitAgentCompleted(agentId, taskId)
- [ ] emitAgentError(agentId, taskId, error)
- [ ] emitPoolStatus(status)
- [ ] emitBudgetUpdate(budget)
- [ ] emitNotification(notification)
- [ ] Proper JSON serialization

**Event Types**: SPEC.md lines 751-786

---

## 6. Backend - Git Integration 🟡

### BE-GIT-001: Git Service Foundation
**Priority**: MEDIUM
**Type**: Backend
**Dependencies**: INFRA-003
**Estimated Tokens**: 5,000

**Description**: Implement GitService for managing feature branches (MVP: simple branches).

**Acceptance Criteria**:
- [ ] Initialize simple-git with repository path
- [ ] createFeatureBranch(taskId, title) method
- [ ] Single active implementation branch enforcement
- [ ] commitChanges(message) method
- [ ] pushBranch(branch) method
- [ ] releaseBranch() for cleanup
- [ ] Proper error handling for git operations

**Files to Create**:
- `apps/backend/src/git/git.service.ts`

**Technical Constraints**:
- MVP: Only one implementation branch at a time
- Branch naming: feature/agent-{taskId}-{slugified-title}
- Always pull latest before creating branch

**Related Specification**: SPEC.md lines 1883-1946

---

### BE-GIT-002: Git Cleanup Service
**Priority**: LOW
**Type**: Backend
**Dependencies**: BE-GIT-001
**Estimated Tokens**: 2,000

**Description**: Implement startup cleanup for orphaned git branches.

**Acceptance Criteria**:
- [ ] cleanupOrphanedBranches() method
- [ ] Scan for branches with configured prefix
- [ ] Extract taskId from branch name
- [ ] Check if task exists in database
- [ ] Delete orphaned branches
- [ ] Run on application startup
- [ ] Logging for cleanup operations

**Technical Constraints**:
- Only clean branches matching prefix
- Verify task doesn't exist before deletion

---

## 7. Frontend - Foundation 🔵

### FE-SETUP-001: Configure Tailwind CSS
**Priority**: CRITICAL
**Type**: Frontend
**Dependencies**: INFRA-001, INFRA-004
**Estimated Tokens**: 2,000

**Description**: Set up Tailwind CSS 4.x in the Angular application.

**Acceptance Criteria**:
- [ ] Tailwind CSS installed and configured
- [ ] tailwind.config.js with custom theme
- [ ] Global styles configured
- [ ] Utility classes working in components
- [ ] Dark mode support configured (if needed)
- [ ] Responsive breakpoints configured

**Files to Create/Modify**:
- `apps/frontend/tailwind.config.js`
- `apps/frontend/src/styles.css`

**Technical Constraints**:
- Use Tailwind CSS 4.x
- Follow Angular integration best practices

---

### FE-SETUP-002: Shared Types and Models
**Priority**: HIGH
**Type**: Frontend
**Dependencies**: BE-DATA-001
**Estimated Tokens**: 3,000

**Description**: Create shared TypeScript interfaces and types for frontend-backend communication.

**Acceptance Criteria**:
- [ ] Task interface with all fields
- [ ] PipelineState enum
- [ ] Priority enum
- [ ] AgentType enum
- [ ] AgentInstance interface
- [ ] TaskLog interface
- [ ] LogType enum
- [ ] Notification interface
- [ ] NotificationType enum
- [ ] Config interface
- [ ] Export all from shared library

**Files to Create**:
- `libs/shared/src/lib/models/task.model.ts`
- `libs/shared/src/lib/models/agent.model.ts`
- `libs/shared/src/lib/models/notification.model.ts`
- `libs/shared/src/lib/enums/pipeline-state.enum.ts`

**Technical Constraints**:
- Match backend Prisma types exactly
- Use TypeScript strict mode

---

### FE-SETUP-003: API Service Foundation
**Priority**: HIGH
**Type**: Frontend
**Dependencies**: FE-SETUP-002
**Estimated Tokens**: 4,000

**Description**: Implement base API service for HTTP communication with backend.

**Acceptance Criteria**:
- [ ] ApiService with HttpClient
- [ ] Base URL configuration
- [ ] Error handling interceptor
- [ ] Loading state management
- [ ] Retry logic for failed requests
- [ ] Type-safe response handling

**Files to Create**:
- `apps/frontend/src/app/core/services/api.service.ts`
- `apps/frontend/src/app/core/interceptors/error.interceptor.ts`

**Technical Constraints**:
- Use Angular HttpClient
- Proper error handling and typing

---

### FE-SETUP-004: SSE Service for Real-time Updates
**Priority**: CRITICAL
**Type**: Frontend
**Dependencies**: FE-SETUP-003
**Estimated Tokens**: 5,000

**Description**: Implement EventSource service for receiving real-time updates from backend.

**Acceptance Criteria**:
- [ ] SseService with EventSource connection
- [ ] Connect to /api/events endpoint
- [ ] Handle all event types (task:updated, agent:statusChanged, etc.)
- [ ] Reconnection logic on disconnect
- [ ] Signal-based state updates
- [ ] Proper cleanup on service destroy

**Files to Create**:
- `apps/frontend/src/app/core/services/sse.service.ts`

**Event Types to Handle**:
- task:created, task:updated, task:deleted
- task:log
- agent:statusChanged, agent:assigned, agent:completed, agent:error
- pool:status
- budget:updated
- notification:new

**Related Specification**: SPEC.md lines 742-786

---

## 8. Frontend - State Management 🔵

### FE-STATE-001: Task Store with Signals
**Priority**: HIGH
**Type**: Frontend
**Dependencies**: FE-SETUP-002, FE-SETUP-004
**Estimated Tokens**: 5,000

**Description**: Implement task state management using Angular Signals.

**Acceptance Criteria**:
- [ ] TaskStore service with signals
- [ ] tasks signal (WritableSignal<Task[]>)
- [ ] tasksByState computed signal
- [ ] selectedTask signal
- [ ] CRUD methods (createTask, updateTask, deleteTask)
- [ ] Integration with SSE updates
- [ ] Optimistic updates for better UX

**Files to Create**:
- `apps/frontend/src/app/core/stores/task.store.ts`

**Technical Constraints**:
- Use Angular Signals (signal(), computed(), effect())
- Immutable state updates
- Efficient reactivity

---

### FE-STATE-002: Agent Store with Signals
**Priority**: MEDIUM
**Type**: Frontend
**Dependencies**: FE-SETUP-002, FE-SETUP-004
**Estimated Tokens**: 3,000

**Description**: Implement agent state management using Angular Signals.

**Acceptance Criteria**:
- [ ] AgentStore service with signals
- [ ] agents signal (WritableSignal<AgentInstance[]>)
- [ ] activeCount computed signal
- [ ] maxAgents signal
- [ ] queuedCount signal
- [ ] paused signal
- [ ] Integration with SSE updates

**Files to Create**:
- `apps/frontend/src/app/core/stores/agent.store.ts`

---

### FE-STATE-003: Budget Store with Signals
**Priority**: MEDIUM
**Type**: Frontend
**Dependencies**: FE-SETUP-002, FE-SETUP-004
**Estimated Tokens**: 2,500

**Description**: Implement budget state management using Angular Signals.

**Acceptance Criteria**:
- [ ] BudgetStore service with signals
- [ ] dailyUsage, dailyLimit signals
- [ ] monthlyUsage, monthlyLimit signals
- [ ] exceeded signal
- [ ] dailyPercentage computed signal
- [ ] monthlyPercentage computed signal
- [ ] Integration with SSE updates

**Files to Create**:
- `apps/frontend/src/app/core/stores/budget.store.ts`

---

### FE-STATE-004: Notification Store with Signals
**Priority**: LOW
**Type**: Frontend
**Dependencies**: FE-SETUP-002, FE-SETUP-004
**Estimated Tokens**: 2,500

**Description**: Implement notification state management using Angular Signals.

**Acceptance Criteria**:
- [ ] NotificationStore service with signals
- [ ] notifications signal (WritableSignal<Notification[]>)
- [ ] recent computed signal (last 10)
- [ ] unreadCount computed signal
- [ ] markAsRead(id) method
- [ ] Integration with SSE updates

**Files to Create**:
- `apps/frontend/src/app/core/stores/notification.store.ts`

---

## 9. Frontend - Kanban Board 🔵

### FE-KANBAN-001: Task Board Component
**Priority**: CRITICAL
**Type**: Frontend
**Dependencies**: FE-STATE-001
**Estimated Tokens**: 5,000

**Description**: Implement main Kanban board component with fixed 7-state pipeline.

**Acceptance Criteria**:
- [ ] TaskBoardComponent with standalone setup
- [ ] Fixed 7 columns (BACKLOG, PLANNING, IMPLEMENTING, REVIEWING, TESTING, PR_READY, DONE)
- [ ] tasksByState computed signal for grouping
- [ ] No drag-and-drop (MVP)
- [ ] Click to navigate to detail view
- [ ] Responsive layout with horizontal scroll
- [ ] Loading and error states

**Files to Create**:
- `apps/frontend/src/app/features/tasks/task-board/task-board.component.ts`

**Template Structure**:
```html
<div class="dashboard-layout">
  <app-budget-widget />
  <app-notifications-panel />
  <div class="flex gap-4 p-4 h-full overflow-x-auto">
    @for (column of columns; track column.state) {
      <app-task-column ... />
    }
  </div>
</div>
```

**Related Specification**: SPEC.md lines 802-853

---

### FE-KANBAN-002: Task Column Component
**Priority**: HIGH
**Type**: Frontend
**Dependencies**: FE-KANBAN-001
**Estimated Tokens**: 3,000

**Description**: Implement task column component for displaying tasks in each pipeline state.

**Acceptance Criteria**:
- [ ] TaskColumnComponent with @Input for state and tasks
- [ ] Column header with state name and task count
- [ ] Vertical layout with scrolling
- [ ] Empty state message
- [ ] Task card click emits event to parent
- [ ] Responsive width

**Files to Create**:
- `apps/frontend/src/app/features/tasks/task-column/task-column.component.ts`

**Technical Constraints**:
- Use Angular CDK ScrollingModule for virtual scrolling (if many tasks)

---

### FE-KANBAN-003: Task Card Component
**Priority**: HIGH
**Type**: Frontend
**Dependencies**: FE-KANBAN-002
**Estimated Tokens**: 4,000

**Description**: Implement task card component with error indication and agent status.

**Acceptance Criteria**:
- [ ] TaskCardComponent with task input signal
- [ ] Display title, description (truncated), priority
- [ ] Error state with red border and icon
- [ ] Active agent indicator (colored border by type)
- [ ] Subtask progress (completed/total)
- [ ] Priority badge styling
- [ ] Hover effects
- [ ] Click to navigate to detail

**Files to Create**:
- `apps/frontend/src/app/features/tasks/task-card/task-card.component.ts`

**Visual Requirements**:
- Red border and background for errors
- Blue border for ui-agent
- Green border for api-agent
- Animated pulse dot for active agents
- Priority color coding (critical=red, high=orange, medium=yellow, low=gray)

**Related Specification**: SPEC.md lines 856-915

---

## 10. Frontend - Task Management 🔵

### FE-TASK-001: Task Detail Component
**Priority**: HIGH
**Type**: Frontend
**Dependencies**: FE-STATE-001
**Estimated Tokens**: 6,000

**Description**: Implement task detail view with full information and action buttons.

**Acceptance Criteria**:
- [ ] TaskDetailComponent with route parameter for taskId
- [ ] Display all task fields (description, acceptance criteria, edge cases, constraints, docs)
- [ ] Show subtasks with dependency indicators and checkboxes
- [ ] Display task metadata (state, priority, tokens used)
- [ ] State transition buttons (Advance, Revert)
- [ ] Abort agent button (when agent assigned)
- [ ] Pause task button
- [ ] Split view: details panel + logs panel
- [ ] Proper layout with header and footer

**Files to Create**:
- `apps/frontend/src/app/features/tasks/task-detail/task-detail.component.ts`

**Actions**:
- Advance to next state (if valid)
- Revert to previous state (if valid)
- Abort assigned agent
- Pause task and subtasks

**Related Specification**: SPEC.md lines 1087-1188

---

### FE-TASK-002: Task Logs Component with Virtual Scrolling
**Priority**: HIGH
**Type**: Frontend
**Dependencies**: FE-TASK-001
**Estimated Tokens**: 6,000

**Description**: Implement virtual scrolling log viewer with search and filters.

**Acceptance Criteria**:
- [ ] TaskLogsComponent with taskId input
- [ ] Virtual scrolling using Angular CDK
- [ ] Search input for filtering log content
- [ ] Log type filters (INFO, TOOL_USE, TOOL_RESULT, ERROR, THINKING)
- [ ] Agent type filters (dynamic based on logs)
- [ ] Real-time log streaming from SSE
- [ ] Syntax highlighting for log content
- [ ] Search result highlighting
- [ ] Auto-scroll to bottom on new logs (with pause option)
- [ ] Monospace font for code-like content

**Files to Create**:
- `apps/frontend/src/app/features/tasks/task-logs/task-logs.component.ts`

**Technical Constraints**:
- Use @angular/cdk/scrolling for virtual scroll
- Efficient filtering with signals
- Handle large log volumes (1000+ entries)

**Related Specification**: SPEC.md lines 1190-1293

---

### FE-TASK-003: Create Task Modal/Form
**Priority**: MEDIUM
**Type**: Frontend
**Dependencies**: FE-STATE-001
**Estimated Tokens**: 5,000

**Description**: Implement task creation form with all structured fields.

**Acceptance Criteria**:
- [ ] CreateTaskComponent with reactive form
- [ ] Title and description fields (required)
- [ ] Acceptance criteria (array of strings, add/remove)
- [ ] Edge cases (array of strings, add/remove)
- [ ] Technical constraints (array of strings, add/remove)
- [ ] Related documentation links (array of URLs, add/remove)
- [ ] Expected outcome (optional, for UI tasks)
- [ ] Priority selection
- [ ] Form validation
- [ ] Display cost estimate before creation
- [ ] Cancel and Create buttons
- [ ] Loading state during creation

**Files to Create**:
- `apps/frontend/src/app/features/tasks/create-task/create-task.component.ts`

**Technical Constraints**:
- Use Angular Reactive Forms
- Validate URLs for documentation links
- Show cost estimate from backend before finalizing

---

### FE-TASK-004: Update Task Component
**Priority**: LOW
**Type**: Frontend
**Dependencies**: FE-TASK-003
**Estimated Tokens**: 3,000

**Description**: Implement task editing functionality.

**Acceptance Criteria**:
- [ ] UpdateTaskComponent with pre-filled form
- [ ] Reuse form structure from CreateTaskComponent
- [ ] Load existing task data
- [ ] Update only changed fields
- [ ] Prevent updates when agent is assigned
- [ ] Validation
- [ ] Success/error feedback

**Files to Create**:
- `apps/frontend/src/app/features/tasks/update-task/update-task.component.ts`

---

## 11. Frontend - Dashboard Widgets 🔵

### FE-WIDGET-001: Budget Widget Component
**Priority**: HIGH
**Type**: Frontend
**Dependencies**: FE-STATE-003
**Estimated Tokens**: 3,000

**Description**: Implement budget widget showing daily and monthly token usage.

**Acceptance Criteria**:
- [ ] BudgetWidgetComponent displaying usage bars
- [ ] Daily usage: current/limit with percentage bar
- [ ] Monthly usage: current/limit with percentage bar
- [ ] Color coding (green → yellow → red as usage increases)
- [ ] Red background when budget exceeded
- [ ] Percentage calculations with computed signals
- [ ] Number formatting (e.g., 45K, 1.2M)
- [ ] Real-time updates from budget store

**Files to Create**:
- `apps/frontend/src/app/features/dashboard/budget-widget/budget-widget.component.ts`

**Visual Requirements**:
- Progress bars turn red at 80%
- "Budget exceeded - agents paused" warning when exceeded

**Related Specification**: SPEC.md lines 917-977

---

### FE-WIDGET-002: Notifications Panel Component
**Priority**: MEDIUM
**Type**: Frontend
**Dependencies**: FE-STATE-004
**Estimated Tokens**: 3,000

**Description**: Implement notifications panel showing recent system notifications.

**Acceptance Criteria**:
- [ ] NotificationsPanelComponent with scrollable list
- [ ] Show last 10 notifications
- [ ] Display timestamp and message
- [ ] Unread notifications in bold
- [ ] Click to mark as read
- [ ] Empty state message
- [ ] Real-time updates from notification store
- [ ] Max height with scrolling

**Files to Create**:
- `apps/frontend/src/app/features/dashboard/notifications-panel/notifications-panel.component.ts`

**Related Specification**: SPEC.md lines 979-1019

---

### FE-WIDGET-003: Agent Monitor Component
**Priority**: MEDIUM
**Type**: Frontend
**Dependencies**: FE-STATE-002
**Estimated Tokens**: 4,000

**Description**: Implement agent pool monitoring widget.

**Acceptance Criteria**:
- [ ] AgentMonitorComponent showing active agents
- [ ] Status indicator dots (green=running, gray=idle, red=error)
- [ ] Agent type and current action display
- [ ] Token usage per agent
- [ ] Active/max count display
- [ ] Queued tasks count with priority indication
- [ ] Pipeline paused warning
- [ ] Real-time updates from agent store

**Files to Create**:
- `apps/frontend/src/app/features/dashboard/agent-monitor/agent-monitor.component.ts`

**Visual Requirements**:
- Dark theme (bg-gray-900)
- Animated pulse on active agents
- Clear status indicators

**Related Specification**: SPEC.md lines 1021-1084

---

## 12. Frontend - Routing & Navigation 🔵

### FE-NAV-001: App Routing Configuration
**Priority**: MEDIUM
**Type**: Frontend
**Dependencies**: FE-KANBAN-001, FE-TASK-001
**Estimated Tokens**: 2,000

**Description**: Set up Angular routing with all application routes.

**Acceptance Criteria**:
- [ ] App routing module with routes
- [ ] / → Dashboard (Kanban board)
- [ ] /tasks/:id → Task detail view
- [ ] /settings → Configuration page (placeholder for Phase 3)
- [ ] Route guards for invalid task IDs
- [ ] 404 Not Found page
- [ ] Navigation links in header

**Files to Create/Modify**:
- `apps/frontend/src/app/app.routes.ts`

**Routes**:
```typescript
const routes = [
  { path: '', component: TaskBoardComponent },
  { path: 'tasks/:id', component: TaskDetailComponent },
  { path: 'settings', component: SettingsComponent },
  { path: '**', component: NotFoundComponent }
];
```

---

### FE-NAV-002: App Shell and Header
**Priority**: LOW
**Type**: Frontend
**Dependencies**: FE-NAV-001
**Estimated Tokens**: 2,500

**Description**: Implement application shell with header navigation.

**Acceptance Criteria**:
- [ ] AppComponent with router outlet
- [ ] Header with navigation links
- [ ] App title/logo
- [ ] Link to dashboard (home)
- [ ] Link to settings
- [ ] Responsive layout
- [ ] Consistent styling

**Files to Create**:
- `apps/frontend/src/app/app.component.ts`
- `apps/frontend/src/app/core/components/header/header.component.ts`

---

## 13. Integration & Testing 🟢

### INT-001: End-to-End Task Flow Test
**Priority**: CRITICAL
**Type**: Integration
**Dependencies**: All BE and FE tasks
**Estimated Tokens**: 5,000

**Description**: Test complete task flow from creation to PR ready state.

**Acceptance Criteria**:
- [ ] Create new task via API
- [ ] Verify task appears in Backlog column
- [ ] Transition task to PLANNING state
- [ ] Verify orchestrator agent spawns
- [ ] Monitor orchestrator execution and subtask creation
- [ ] Verify subtasks appear in IMPLEMENTING state
- [ ] Monitor implementation agents
- [ ] Test state transitions through pipeline
- [ ] Verify real-time SSE updates work
- [ ] Test budget tracking
- [ ] Test error handling with retry

**Test Scenarios**:
1. Happy path: Task → Planning → Implementing → Reviewing → Testing → PR Ready
2. Error scenario: Agent failure with retry
3. Budget exceeded: Agent spawning paused
4. Manual interventions: Abort, pause, state transitions

---

### INT-002: Real-time Synchronization Test
**Priority**: HIGH
**Type**: Integration
**Dependencies**: BE-SSE-002, FE-SETUP-004
**Estimated Tokens**: 3,000

**Description**: Verify real-time updates between backend and frontend.

**Acceptance Criteria**:
- [ ] Create task via API, verify appears in UI immediately
- [ ] Update task state, verify UI updates without refresh
- [ ] Spawn agent, verify status appears in agent monitor
- [ ] Stream logs, verify appear in real-time in task detail
- [ ] Update budget, verify widget updates
- [ ] Create notification, verify appears in panel
- [ ] Test with multiple concurrent operations
- [ ] Test reconnection after connection loss

---

### INT-003: Agent Execution Integration Test
**Priority**: HIGH
**Type**: Integration
**Dependencies**: BE-AGENT-003, BE-ORCH-001
**Estimated Tokens**: 4,000

**Description**: Test agent execution with actual Claude SDK integration.

**Acceptance Criteria**:
- [ ] Configure test environment with valid API key
- [ ] Spawn orchestrator agent for test task
- [ ] Verify agent receives proper context and tools
- [ ] Monitor token usage tracking
- [ ] Verify log streaming works
- [ ] Test agent completion and cleanup
- [ ] Test abort functionality
- [ ] Verify budget updates during execution

**Test Task**: Simple feature like "Add a hello world endpoint"

---

## 14. Documentation & DevOps ⚪

### DOC-001: README and Setup Guide
**Priority**: MEDIUM
**Type**: Documentation
**Dependencies**: None
**Estimated Tokens**: 2,500

**Description**: Create comprehensive README with setup instructions.

**Acceptance Criteria**:
- [ ] Project overview and architecture diagram
- [ ] Prerequisites (Node.js, PostgreSQL, Git, Anthropic API key)
- [ ] Installation steps
- [ ] Environment variable configuration
- [ ] Database setup with Prisma
- [ ] Development server startup
- [ ] Common troubleshooting issues
- [ ] Links to SPEC.md and other docs

**Files to Create**:
- `README.md`
- `CONTRIBUTING.md`

---

### DOC-002: API Documentation
**Priority**: LOW
**Type**: Documentation
**Dependencies**: BE-DATA-002
**Estimated Tokens**: 3,000

**Description**: Document all REST API endpoints with examples.

**Acceptance Criteria**:
- [ ] OpenAPI/Swagger specification
- [ ] Endpoint descriptions with request/response examples
- [ ] Authentication requirements
- [ ] Error codes and responses
- [ ] SSE event documentation
- [ ] Postman collection (optional)

**Files to Create**:
- `docs/API.md`
- `apps/backend/swagger.yaml` (optional)

---

### DEVOPS-001: Docker Configuration
**Priority**: LOW
**Type**: DevOps
**Dependencies**: INFRA-001
**Estimated Tokens**: 3,000

**Description**: Create Docker and docker-compose configuration for easy deployment.

**Acceptance Criteria**:
- [ ] Dockerfile for backend
- [ ] Dockerfile for frontend
- [ ] docker-compose.yml for full stack
- [ ] PostgreSQL service in compose
- [ ] Environment variable configuration
- [ ] Volume mounts for development
- [ ] Health checks
- [ ] Build and deployment instructions

**Files to Create**:
- `apps/backend/Dockerfile`
- `apps/frontend/Dockerfile`
- `docker-compose.yml`
- `docs/DEPLOYMENT.md`

---

### DEVOPS-002: Environment Validation and Health Checks
**Priority**: MEDIUM
**Type**: DevOps
**Dependencies**: INFRA-003
**Estimated Tokens**: 2,000

**Description**: Implement environment validation and application health checks.

**Acceptance Criteria**:
- [ ] Validate required environment variables on startup
- [ ] Verify Anthropic API key is valid
- [ ] Check repository path exists and is accessible
- [ ] Verify database connection
- [ ] Health check endpoint: GET /health
- [ ] Readiness check endpoint: GET /ready
- [ ] Graceful shutdown handling

**Files to Create**:
- `apps/backend/src/health/health.controller.ts`

---

## Summary

**Total Tasks**: 76
**Critical Path Tasks**: 11
**Backend Tasks**: 26
**Frontend Tasks**: 23
**Integration Tasks**: 3
**Infrastructure Tasks**: 4
**Documentation/DevOps Tasks**: 6

**Estimated Total Tokens**: ~280,000

---

## Implementation Strategy

### Phase 1a: Foundation (Week 1)
Focus: Infrastructure, database, basic backend structure
- All INFRA tasks
- BE-DATA-001 through BE-DATA-004
- FE-SETUP-001 through FE-SETUP-004

### Phase 1b: Core Features (Week 2-3)
Focus: Agent system, orchestration, frontend components
- All BE-AGENT tasks
- BE-ORCH-001 through BE-ORCH-003
- BE-BUDGET-001
- All FE-STATE tasks
- All FE-KANBAN tasks

### Phase 1c: Real-time & Integration (Week 4)
Focus: SSE, git integration, task detail views
- BE-SSE-001, BE-SSE-002
- BE-GIT-001
- FE-TASK-001, FE-TASK-002
- FE-WIDGET-001 through FE-WIDGET-003
- All INT tasks

### Phase 1d: Polish & Deploy (Week 5)
Focus: Documentation, Docker, testing
- Remaining FE-TASK tasks
- FE-NAV tasks
- All DOC tasks
- All DEVOPS tasks

---

## Task Assignment Guidelines

### UI/Frontend Agent
- All FE- prefixed tasks
- Focus on Angular 21, Signals, Tailwind CSS
- Standalone components only

### API/Backend Agent
- All BE- prefixed tasks
- Focus on NestJS, Prisma, Claude SDK
- Proper dependency injection and error handling

### DevOps Agent
- All INFRA- and DEVOPS- prefixed tasks
- Focus on Nx, Docker, environment setup

### Integration Agent
- All INT- prefixed tasks
- Focus on E2E testing and validation

### Documentation Agent
- All DOC- prefixed tasks
- Focus on clarity and completeness

---

## Next Steps

1. Review and approve this task breakdown
2. Set up the development environment (INFRA tasks)
3. Begin parallel development on independent task groups
4. Regular integration points to ensure compatibility
5. Continuous testing as features are completed

---

**Notes**:
- Each task includes estimated token usage for cost planning
- Dependencies are explicitly stated to enable parallel work
- All tasks include clear acceptance criteria
- Technical constraints from SPEC.md are preserved
- Tasks are sized for completion in 1-2 agent sessions
