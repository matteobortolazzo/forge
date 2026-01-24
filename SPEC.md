# AI Agent Dashboard - Technical Specification

## Project Overview

A web-based dashboard for orchestrating and monitoring AI coding agents powered by the Claude Agent SDK. The system implements a Kanban-style pipeline where tasks flow through stages, each handled by specialized AI agents working in a controlled queue with budget management.

## Goals

1. Visual Kanban board to manage coding tasks from inception to PR
2. Orchestrate Claude Code instances with configurable concurrency and budget limits
3. Real-time visibility into agent activity, logs, and progress
4. Automatic delegation to specialized agents (UI, API, Review, Test, Simplifier)
5. Human-in-the-loop controls for approvals and manual state transitions
6. Repository-specific knowledge base that evolves with merged PRs

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
│  │  [ui-agent: implementing - 450 tokens] [idle] [idle]            │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │               Budget Widget                                      │    │
│  │  Daily: 45k/100k tokens | Monthly: 1.2M/5M                      │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │               Notifications Panel                                │    │
│  │  • Task #12 → REVIEWING | • Task #8 → PR_READY                  │    │
│  └─────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────┘
                                   │
                            EventSource/SSE
                                   │
┌─────────────────────────────────────────────────────────────────────────┐
│                          NestJS Backend                                  │
│                                                                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐    │
│  │   Tasks     │  │   Agents    │  │     SSE     │  │    Git      │    │
│  │   Module    │  │   Module    │  │  Gateway    │  │   Module    │    │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘    │
│         │                │                                               │
│         └────────────────┼───────────────────────────────────────┐      │
│                          ▼                                       │      │
│  ┌───────────────────────────────────────────────────────────┐   │      │
│  │                  Agent Pool Manager                        │   │      │
│  │  - Single orchestrator for all planning (sequential)      │   │      │
│  │  - Priority queue for task execution (no preemption)      │   │      │
│  │  - Global budget enforcement with auto-extend + notify    │   │      │
│  │  - Max 2-level subtask nesting                            │   │      │
│  │  - One active implementation task at a time (MVP)         │   │      │
│  └───────────────────────────────────────────────────────────┘   │      │
│                          │                                       │      │
│                          ▼                                       │      │
│  ┌───────────────────────────────────────────────────────────┐   │      │
│  │              Claude Agent SDK Instances                    │   │      │
│  │  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐         │   │      │
│  │  │Orchestr.│ │UI Agent │ │API Agent│ │Simplif. │  ...    │   │      │
│  │  │ (Opus)  │ │(Sonnet) │ │(Sonnet) │ │(Haiku)  │         │   │      │
│  │  └─────────┘ └─────────┘ └─────────┘ └─────────┘         │   │      │
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
| UI Components | Angular CDK (drag-drop) | Latest |
| Styling | Tailwind CSS | 4.x |
| Backend | NestJS | 11.x |
| Real-time | EventSource/SSE | Native |
| Database | SQLite (dev) / PostgreSQL (prod) | - |
| ORM | Prisma | 7.x |
| Agent SDK | @anthropic-ai/claude-agent-sdk | Latest |
| Git Operations | simple-git | Latest |
| Monorepo | Nx | 22.x |

---

## Data Models

### Task

```typescript
interface Task {
  id: string;                    // UUID
  title: string;                 // Short description

  // Structured fields
  description: string;           // Detailed requirements
  acceptanceCriteria: string[];  // What defines done
  edgeCases: string[];           // Scenarios to consider
  technicalConstraints: string[];// Requirements (e.g., "Must use React Hooks")
  relatedDocLinks: string[];     // URLs to relevant docs/specs
  expectedOutcome: string;       // For UI tasks, describe visual result

  state: PipelineState;          // Current pipeline stage
  priority: Priority;            // Inherited by subtasks

  // Relationships
  parentTaskId?: string;         // For subtasks created by orchestrator
  childTaskIds: string[];        // Subtasks (max 2 levels deep)
  blockedBy: string[];           // Task IDs that must complete first

  // Agent tracking
  assignedAgentId?: string;      // Currently assigned agent instance
  agentType?: AgentType;         // Type of agent working on it

  // Git integration (MVP: simple branches, one task at a time)
  branch?: string;               // Feature branch name
  prUrl?: string;                // Created PR URL

  // Budget tracking
  tokensUsed: number;            // Cumulative for this task

  // Metadata
  logs: TaskLog[];               // Agent output logs (30-day retention)
  hasError: boolean;             // For red border indication
  errorMessage?: string;         // Latest error for display
  createdAt: Date;
  updatedAt: Date;
  completedAt?: Date;
}

enum PipelineState {
  BACKLOG = 'backlog',           // Not started
  PLANNING = 'planning',         // Orchestrator analyzing
  IMPLEMENTING = 'implementing', // Code being written
  REVIEWING = 'reviewing',       // Code review in progress (may block for revisions)
  TESTING = 'testing',           // Tests being written/run
  PR_READY = 'pr_ready',         // PR created, awaiting human review
  DONE = 'done',                 // PR merged (auto-transitioned via webhook)
  BLOCKED = 'blocked'            // Manual state for external dependencies
}

enum Priority {
  LOW = 'low',
  MEDIUM = 'medium',
  HIGH = 'high',
  CRITICAL = 'critical'
}

interface TaskLog {
  id: string;
  taskId: string;
  agentId: string;
  agentType: AgentType;
  timestamp: Date;
  type: LogType;                 // For filtering
  content: string;
  metadata?: Record<string, unknown>;

  @@index([taskId])
  @@retention(30 days)           // Auto-delete after 30 days
}

enum LogType {
  INFO = 'info',
  TOOL_USE = 'tool_use',
  TOOL_RESULT = 'tool_result',
  ERROR = 'error',
  THINKING = 'thinking'          // Streamed in real-time
}
```

### Agent

```typescript
interface AgentInstance {
  id: string;                    // UUID
  type: AgentType;
  status: AgentStatus;

  // Current work
  taskId?: string;
  currentAction?: string;        // e.g., "Reading file X", "Running tests"
  startedAt?: Date;

  // Stats (simple tracking)
  totalTasksCompleted: number;
  totalTokensUsed: number;
  tokensThisTask: number;        // For real-time display
}

enum AgentType {
  ORCHESTRATOR = 'orchestrator',     // Single instance, Opus
  UI_AGENT = 'ui-agent',             // Sonnet
  API_AGENT = 'api-agent',           // Sonnet
  SIMPLIFIER = 'simplifier',         // Haiku, formatting/linting
  CODE_REVIEWER = 'code-reviewer',   // Sonnet
  TEST_AGENT = 'test-agent',         // Sonnet
  PR_AGENT = 'pr-agent',             // Haiku
  COST_ESTIMATOR = 'cost-estimator'  // Haiku, estimates task complexity
}

enum AgentStatus {
  IDLE = 'idle',
  RUNNING = 'running',
  PAUSED = 'paused',
  ERROR = 'error'
}
```

### Configuration

```typescript
interface DashboardConfig {
  // Concurrency (requires restart to change)
  maxConcurrentAgents: number;   // Default: 3

  // Budget limits (global daily/monthly)
  dailyTokenBudget: number;      // Default: 100k
  monthlyTokenBudget: number;    // Default: 5M
  currentDailyUsage: number;
  currentMonthlyUsage: number;
  budgetExceeded: boolean;       // Pauses new agent spawns

  // Repository settings (single repo for MVP)
  repositoryPath: string;
  defaultBranch: string;         // e.g., 'main'
  branchPrefix: string;          // e.g., 'feature/agent-'

  // Protected paths (read-only for agents)
  protectedPaths: string[];      // Default: ['.git/', '.env', 'node_modules/']

  // Automation settings
  autoAdvance: boolean;          // Auto-move tasks to next stage
  requireHumanApproval: PipelineState[]; // Stages requiring manual approval

  // Pattern learning
  patternsFile: string;          // Path to CLAUDE.md (per-repo)
}
```

### Notification

```typescript
interface Notification {
  id: string;
  type: NotificationType;
  taskId?: string;               // If task-related
  message: string;
  timestamp: Date;
  read: boolean;
}

enum NotificationType {
  TASK_STATE_CHANGE = 'task_state_change',
  PR_CREATED = 'pr_created',
  PR_MERGED = 'pr_merged',
  BUDGET_WARNING = 'budget_warning',   // 80% of limit
  BUDGET_EXCEEDED = 'budget_exceeded'
}
```

---

## Agent Definitions

### Orchestrator Agent

**Purpose**: Analyzes incoming tasks, creates work plan with subtasks, handles dynamic replanning on failures.

**Key Behaviors**:
- Single instance for all planning (sequential planning ensures consistency)
- Uses Opus for highest quality planning
- Creates explicit dependency graph (max 2 levels deep)
- Only queues tasks that can be completed end-to-end
- Can dynamically replan if subtasks fail
- Quality-first approach (doesn't optimize for budget)

```typescript
const orchestratorAgent: AgentDefinition = {
  description: 'Senior software architect that analyzes requirements and creates optimal execution plans.',
  prompt: `You are a senior software architect and task orchestrator.

When given a task:
1. Analyze the structured fields:
   - Description and acceptance criteria
   - Edge cases to handle
   - Technical constraints to respect
   - Related documentation to reference
   - Expected outcomes (especially for UI work)

2. Check repository patterns (CLAUDE.md) for:
   - Established coding conventions
   - Common architectural patterns
   - Known gotchas and solutions

3. Identify work type and dependencies:
   - UI/Frontend work (Angular components, styling, routing)
   - API/Backend work (endpoints, services, database)
   - Mixed UI + API work
   - Infrastructure/DevOps
   - Identify hard dependencies (A must complete before B)

4. Create optimal execution plan:
   - Break into concrete subtasks (max 2 levels deep)
   - Assign appropriate agent types
   - Define explicit dependency graph
   - Ensure tasks are completable end-to-end (no external blockers)

5. Output structured plan as JSON:
{
  "analysis": "Brief analysis of the task and approach",
  "strategy": "Overall implementation strategy",
  "subtasks": [
    {
      "title": "Clear, actionable subtask title",
      "description": "Detailed description with context",
      "acceptanceCriteria": ["Criterion 1", "Criterion 2"],
      "agentType": "ui-agent|api-agent",
      "dependencies": ["subtask-id-1", "subtask-id-2"],
      "estimatedComplexity": "low|medium|high"
    }
  ],
  "criticalPath": ["subtask-id-1", "subtask-id-3"],
  "notes": "Important considerations or risks"
}

REPLANNING MODE (triggered on subtask failure):
- When a subtask fails, you'll receive:
  - Original plan
  - Failed subtask details
  - Error information
  - Current state
- Analyze the failure and create updated plan
- May add new subtasks, modify existing, or change approach
- Maintain consistency with completed work

CONSTRAINTS:
- Maximum 2 levels of subtask nesting
- Each subtask must be independently completable
- No external dependencies (if needed, mark parent task as BLOCKED)
- Prefer smaller, focused subtasks over large monolithic ones
- One implementation task active at a time (MVP limitation)

Remember: Quality planning prevents execution issues. Be thorough.`,
  tools: ['Read', 'Glob', 'Grep'],
  model: 'opus'
};
```

### UI Agent (Angular Specialist)

```typescript
const uiAgent: AgentDefinition = {
  description: 'Angular 21 frontend specialist. Use for UI components, styling, routing, and frontend logic.',
  prompt: `You are an expert Angular 21 developer.

CONTEXT SOURCES:
1. Task structured fields (description, criteria, constraints, expected outcome)
2. Repository patterns from CLAUDE.md
3. Related documentation links provided in task

Technology requirements:
- Angular 21 with standalone components (NO NgModules)
- Signals for state management (signal(), computed(), effect())
- Modern control flow (@if, @for, @switch - NOT *ngIf/*ngFor)
- Tailwind CSS for styling
- Angular CDK for complex UI patterns
- Strict TypeScript with proper typing

Code standards:
- One component per file
- Use inject() function, not constructor injection
- Prefer OnPush change detection
- Write self-documenting code with minimal comments
- Follow Angular style guide naming conventions
- Respect technical constraints from task

When implementing:
1. Review CLAUDE.md for repo-specific patterns
2. Explore existing code structure with Glob/Grep
3. Check related documentation links
4. Identify related components and services
5. Implement following existing patterns
6. Ensure proper error handling
7. Match expected outcome if provided
8. Add basic unit test structure

FILE ACCESS:
- Read/write access to most files
- PROTECTED (read-only): .git/, .env, node_modules/
- If you need to modify protected paths, explain why and request approval

COMPLETION:
- Commit changes with clear message
- Report completion with summary of changes`,
  tools: ['Read', 'Write', 'Edit', 'Bash', 'Glob', 'Grep'],
  model: 'sonnet'
};
```

### API Agent (Backend Specialist)

```typescript
const apiAgent: AgentDefinition = {
  description: 'NestJS backend specialist. Use for API endpoints, services, database operations.',
  prompt: `You are an expert NestJS backend developer.

CONTEXT SOURCES:
1. Task structured fields (description, criteria, constraints)
2. Repository patterns from CLAUDE.md
3. Related documentation links provided in task

Technology requirements:
- NestJS 10 with TypeScript
- Prisma ORM for database operations
- Class-validator for DTO validation
- Proper dependency injection
- RESTful API design principles

Code standards:
- Controllers handle HTTP, Services handle business logic
- Use DTOs for all request/response shapes
- Proper error handling with NestJS exceptions
- Follow NestJS module organization
- Write integration-ready code
- Respect technical constraints from task

When implementing:
1. Review CLAUDE.md for repo-specific patterns
2. Explore existing module structure
3. Check related documentation links
4. Review existing services and patterns
5. Implement following established conventions
6. Add proper validation and error handling
7. Consider database migrations if schema changes needed

FILE ACCESS:
- Read/write access to most files
- PROTECTED (read-only): .git/, .env, node_modules/
- If you need to modify protected paths, explain why and request approval

COMPLETION:
- Commit changes with clear message
- Report completion with summary of changes`,
  tools: ['Read', 'Write', 'Edit', 'Bash', 'Glob', 'Grep'],
  model: 'sonnet'
};
```

### Simplifier Agent

**Purpose**: Runs formatting and linting before code review to reduce noise.

```typescript
const simplifierAgent: AgentDefinition = {
  description: 'Code formatting and linting specialist. Runs before review to clean up style issues.',
  prompt: `You are a code quality automation specialist.

Your job is to run automated formatting and linting tools to ensure code follows style standards before human review.

Tasks:
1. Identify the project type (Angular, NestJS, etc.)
2. Run appropriate formatters:
   - Prettier for TypeScript/JavaScript
   - ESLint with --fix for auto-fixable issues
   - Any other repo-specific formatters (check package.json scripts)
3. Amend changes to the last commit (implementation commit)
   - Use: git commit --amend --no-edit
4. Report what was fixed

DO NOT:
- Make logic changes
- Refactor code structure
- Fix non-auto-fixable lint errors (report these instead)
- Modify protected paths (.git/, .env, node_modules/)

Commands to run:
- npm run format (or equivalent)
- npm run lint:fix (or equivalent)
- git add .
- git commit --amend --no-edit

OUTPUT:
Report summary of what was formatted/fixed.`,
  tools: ['Bash', 'Read'],
  model: 'haiku'
};
```

### Code Review Agent

```typescript
const codeReviewerAgent: AgentDefinition = {
  description: 'Code review specialist. Reviews code quality and creates revision subtasks if issues found.',
  prompt: `You are a senior code reviewer.

Review code for:
1. **Correctness**: Does it solve the stated problem? Meet acceptance criteria?
2. **Security**: SQL injection, XSS, auth issues, secrets exposure
3. **Performance**: N+1 queries, unnecessary re-renders, memory leaks
4. **Maintainability**: Clear naming, proper abstractions, DRY
5. **Type Safety**: Proper TypeScript usage, no 'any' types
6. **Error Handling**: Edge cases covered, proper error messages
7. **Testing**: Are critical paths testable?
8. **Edge Cases**: Are specified edge cases handled?
9. **Technical Constraints**: Are task constraints respected?

Output format:
{
  "summary": "Overall assessment of the changes",
  "issues": [
    {
      "severity": "critical|major|minor",
      "file": "path/to/file.ts",
      "line": 42,
      "category": "security|performance|correctness|style",
      "description": "Clear description of the issue",
      "suggestion": "Specific fix recommendation"
    }
  ],
  "approved": true|false,
  "revisionSubtasks": [
    {
      "title": "Fix SQL injection in user search",
      "description": "Use parameterized queries instead of string concatenation",
      "severity": "critical",
      "files": ["src/users/users.service.ts"]
    }
  ]
}

REVISION WORKFLOW:
- If critical or major issues found: approved = false, create revision subtasks
- Revision subtasks should be specific, actionable, and focused
- Parent task will BLOCK in REVIEWING state until revisions complete
- Revision subtasks inherit parent priority
- Revision subtasks are assigned to original implementation agent type

Be constructive. Focus on significant issues, not style nitpicks (simplifier handles those).`,
  tools: ['Read', 'Grep', 'Glob'],
  model: 'sonnet'
};
```

### Test Agent

```typescript
const testAgent: AgentDefinition = {
  description: 'Test writing and execution specialist.',
  prompt: `You are a test automation specialist.

Testing stack:
- Frontend: Jest + Angular Testing Library
- Backend: Jest + Supertest for e2e
- Use describe/it/expect patterns

Testing priorities:
1. Acceptance criteria coverage
2. Edge cases from task definition
3. Critical business logic
4. Error paths
5. Integration points
6. Happy path scenarios

Guidelines:
- Write focused, single-assertion tests when possible
- Use descriptive test names that explain the scenario
- Mock external dependencies appropriately
- Aim for meaningful coverage, not 100% coverage
- Test edge cases specified in task

Execution:
1. Run clean build (npm ci or equivalent)
2. Write tests in same worktree
3. Run test suite
4. If tests fail:
   - Analyze failures
   - DO NOT fix tests yourself
   - Create revision subtasks for test failures
   - Revision subtasks go back to implementation agent type
5. Report test results

REVISION SUBTASKS FORMAT:
{
  "title": "Fix failing test: User authentication",
  "description": "Test expects 200 status but getting 401. Implementation may not be handling token correctly.",
  "testFailures": ["describe > it block name"],
  "files": ["path/to/failing/file.ts"]
}`,
  tools: ['Read', 'Write', 'Edit', 'Bash', 'Glob', 'Grep'],
  model: 'sonnet'
};
```

### PR Agent

```typescript
const prAgent: AgentDefinition = {
  description: 'Pull request creation specialist. Creates well-documented PRs.',
  prompt: `You are a PR creation specialist.

Create pull requests with:
1. Clear, descriptive title following conventional commits
2. Comprehensive description including:
   - What changes were made and why
   - How to test the changes
   - Link to related task
   - Screenshots for UI changes (if expected outcome provided)
   - Breaking changes if any

Use git commands to:
1. Ensure all changes are committed
2. Push the feature branch
3. Create PR using gh CLI or provide manual instructions

PR title format: type(scope): description
Types: feat, fix, refactor, test, docs, chore

PR description template:
## Summary
[Brief overview]

## Changes
- Change 1
- Change 2

## Testing
- [ ] How to verify the changes

## Related
- Task: #[task-id]
- Closes: #[issue] (if applicable)

Auto-create immediately when task reaches PR_READY state.`,
  tools: ['Bash', 'Read', 'Glob'],
  model: 'haiku'
};
```

### Cost Estimator Agent

```typescript
const costEstimatorAgent: AgentDefinition = {
  description: 'Estimates token usage for tasks before execution.',
  prompt: `You are a cost estimation specialist.

Given a task, estimate how many tokens will be required to complete it.

Consider:
1. Task complexity (description length, acceptance criteria count)
2. Agent types needed (orchestrator + implementation + review + test)
3. Code exploration needed (large codebase vs small change)
4. Similar historical tasks (if available)

Output format:
{
  "estimatedTokens": 15000,
  "breakdown": {
    "orchestrator": 3000,
    "implementation": 8000,
    "simplifier": 500,
    "review": 2000,
    "testing": 1500
  },
  "confidence": "low|medium|high",
  "reasoning": "Brief explanation of estimate"
}

Be conservative (slightly over-estimate). Better to be safe than exceed budget.`,
  tools: ['Read', 'Grep'],
  model: 'haiku'
};
```

---

## API Specification

### REST Endpoints

#### Tasks

```
GET    /api/tasks                 # List all tasks
GET    /api/tasks/:id             # Get task details
POST   /api/tasks                 # Create new task (runs cost estimate first)
PATCH  /api/tasks/:id             # Update task
DELETE /api/tasks/:id             # Delete task (only if no active agent)
POST   /api/tasks/:id/transition  # Transition to new state (via buttons in detail view)
GET    /api/tasks/:id/logs        # Get task logs (with filters)
POST   /api/tasks/:id/abort       # Abort assigned agent
```

#### Agents

```
GET    /api/agents                # List agent instances and status
GET    /api/agents/:id            # Get agent details
POST   /api/agents/pause-all      # Pause entire pipeline
POST   /api/agents/resume-all     # Resume entire pipeline
POST   /api/tasks/:id/pause       # Pause specific task and subtasks
POST   /api/tasks/:id/resume      # Resume specific task
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
PATCH  /api/notifications/:id/read # Mark as read
```

### Server-Sent Events (SSE)

#### Connection

```
GET    /api/events                # EventSource connection
```

#### Event Types

```typescript
interface ServerEvents {
  // Full task state (sent every time)
  'task:updated': Task;
  'task:created': Task;
  'task:deleted': { taskId: string };

  // Log streaming (hybrid: stream thinking, batch tool results)
  'task:log': TaskLog;

  // Agent updates
  'agent:statusChanged': AgentInstance;
  'agent:assigned': { agentId: string; taskId: string };
  'agent:completed': { agentId: string; taskId: string; result: unknown };
  'agent:error': { agentId: string; taskId: string; error: string };

  // Pool status
  'pool:status': {
    active: number;
    queued: TaskQueueItem[];
    paused: boolean;
  };

  // Budget updates
  'budget:updated': {
    dailyUsage: number;
    dailyLimit: number;
    monthlyUsage: number;
    monthlyLimit: number;
    exceeded: boolean;
  };

  // Notifications
  'notification:new': Notification;
}
```

---

## Frontend Components

### Page Structure

```
/                           → Dashboard (Kanban board + budget widget + notifications)
/tasks/:id                  → Task detail view with logs and transition buttons
/settings                   → Configuration page (changes require restart)
```

### Core Components

#### TaskBoardComponent

Main Kanban board. Fixed 7-state pipeline. No drag-and-drop, no filtering.

```typescript
@Component({
  selector: 'app-task-board',
  standalone: true,
  imports: [TaskColumnComponent, BudgetWidgetComponent, NotificationsPanelComponent],
  template: `
    <div class="dashboard-layout">
      <!-- Budget Widget -->
      <app-budget-widget />

      <!-- Notifications Panel -->
      <app-notifications-panel />

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
    { state: PipelineState.BACKLOG, title: 'Backlog' },
    { state: PipelineState.PLANNING, title: 'Planning' },
    { state: PipelineState.IMPLEMENTING, title: 'Implementing' },
    { state: PipelineState.REVIEWING, title: 'Reviewing' },
    { state: PipelineState.TESTING, title: 'Testing' },
    { state: PipelineState.PR_READY, title: 'PR Ready' },
    { state: PipelineState.DONE, title: 'Done' }
  ];

  tasksByState = this.taskStore.tasksByState;

  navigateToDetail(taskId: string) {
    this.router.navigate(['/tasks', taskId]);
  }
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
         [class.border-blue-500]="task().agentType === 'ui-agent'"
         [class.border-green-500]="task().agentType === 'api-agent'"
         [class.border-red-500]="task().hasError"
         [class.bg-red-50]="task().hasError">

      @if (task().hasError) {
        <div class="flex items-center gap-1 text-red-600 text-xs mb-2">
          <svg class="w-4 h-4"><!-- error icon --></svg>
          <span>Error</span>
        </div>
      }

      <h4 class="font-medium text-sm">{{ task().title }}</h4>
      <p class="text-xs text-gray-500 mt-1 line-clamp-2">{{ task().description }}</p>

      @if (task().assignedAgentId) {
        <div class="mt-2 flex items-center gap-1 text-xs">
          <span class="w-2 h-2 rounded-full bg-green-500 animate-pulse"></span>
          <span>{{ task().agentType }}</span>
        </div>
      }

      <div class="mt-2 flex justify-between items-center">
        <span class="text-xs px-2 py-0.5 rounded-full"
              [class]="priorityClass()">
          {{ task().priority }}
        </span>
        @if (task().childTaskIds.length) {
          <span class="text-xs text-gray-400">
            {{ completedSubtasks() }}/{{ task().childTaskIds.length }}
          </span>
        }
      </div>
    </div>
  `
})
export class TaskCardComponent {
  task = input.required<Task>();

  priorityClass = computed(() => {
    const classes = {
      critical: 'bg-red-100 text-red-800',
      high: 'bg-orange-100 text-orange-800',
      medium: 'bg-yellow-100 text-yellow-800',
      low: 'bg-gray-100 text-gray-800'
    };
    return classes[this.task().priority];
  });
}
```

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

Shows recent notifications filtered by type.

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

#### AgentMonitorComponent

Shows agent pool status with basic status + current action + token usage.

```typescript
@Component({
  selector: 'app-agent-monitor',
  standalone: true,
  template: `
    <div class="bg-gray-900 text-white p-4 rounded-lg">
      <div class="flex justify-between items-center mb-4">
        <h3 class="font-semibold">Agent Pool</h3>
        <span class="text-sm">
          {{ activeCount() }}/{{ maxAgents() }} active
        </span>
      </div>

      @if (paused()) {
        <div class="bg-yellow-900 text-yellow-200 p-2 rounded mb-3 text-sm">
          Pipeline paused
        </div>
      }

      <div class="space-y-2">
        @for (agent of agents(); track agent.id) {
          <div class="flex items-center gap-3 p-2 bg-gray-800 rounded">
            <span class="w-2 h-2 rounded-full"
                  [class.bg-green-500]="agent.status === 'running'"
                  [class.bg-gray-500]="agent.status === 'idle'"
                  [class.bg-red-500]="agent.status === 'error'">
            </span>
            <div class="flex-1">
              <div class="text-sm">{{ agent.type }}</div>
              @if (agent.currentAction) {
                <div class="text-xs text-gray-400">{{ agent.currentAction }}</div>
              }
            </div>
            @if (agent.tokensThisTask > 0) {
              <span class="text-xs text-gray-400">
                {{ agent.tokensThisTask }} tokens
              </span>
            }
          </div>
        }
      </div>

      @if (queuedCount() > 0) {
        <div class="mt-3 text-sm text-yellow-400">
          {{ queuedCount() }} tasks queued (priority order)
        </div>
      }
    </div>
  `
})
export class AgentMonitorComponent {
  private agentStore = inject(AgentStore);

  agents = this.agentStore.agents;
  activeCount = this.agentStore.activeCount;
  maxAgents = this.agentStore.maxAgents;
  queuedCount = this.agentStore.queuedCount;
  paused = this.agentStore.paused;
}
```

#### TaskDetailComponent

Full task view with logs, dependency indicators, and transition buttons.

```typescript
@Component({
  selector: 'app-task-detail',
  standalone: true,
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

          @if (task()?.acceptanceCriteria?.length) {
            <h2 class="font-semibold mt-4 mb-2">Acceptance Criteria</h2>
            <ul class="list-disc list-inside space-y-1">
              @for (criterion of task()?.acceptanceCriteria; track criterion) {
                <li class="text-sm">{{ criterion }}</li>
              }
            </ul>
          }

          @if (task()?.edgeCases?.length) {
            <h2 class="font-semibold mt-4 mb-2">Edge Cases</h2>
            <ul class="list-disc list-inside space-y-1">
              @for (edgeCase of task()?.edgeCases; track edgeCase) {
                <li class="text-sm">{{ edgeCase }}</li>
              }
            </ul>
          }

          @if (task()?.childTaskIds?.length) {
            <h2 class="font-semibold mt-4 mb-2">Subtasks</h2>
            <ul class="space-y-1">
              @for (subtaskId of task()?.childTaskIds; track subtaskId) {
                <li class="flex items-center gap-2 text-sm">
                  <input type="checkbox" [checked]="isSubtaskDone(subtaskId)" disabled>
                  <span>{{ getSubtaskTitle(subtaskId) }}</span>
                  @if (getSubtaskBlockers(subtaskId).length > 0) {
                    <span class="text-xs text-gray-400">
                      (Blocked by: {{ getSubtaskBlockers(subtaskId).join(', ') }})
                    </span>
                  }
                </li>
              }
            </ul>
          }
        </div>

        <!-- Logs panel with virtual scrolling -->
        <app-task-logs
          [taskId]="taskId()"
          class="w-1/2"
        />
      </div>

      <!-- Actions (buttons in detail view only) -->
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
        @if (canPauseTask()) {
          <button (click)="pauseTask()" class="btn-secondary">
            Pause Task & Subtasks
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

#### TaskLogsComponent

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
          <button
            *ngFor="let type of logTypes"
            (click)="toggleLogTypeFilter(type)"
            [class.bg-blue-600]="logTypeFilters().includes(type)"
            class="px-2 py-1 text-xs rounded bg-gray-800 hover:bg-gray-700">
            {{ type }}
          </button>
        </div>

        <div class="flex gap-2 flex-wrap">
          <button
            *ngFor="let agentType of agentTypes()"
            (click)="toggleAgentTypeFilter(agentType)"
            [class.bg-green-600]="agentTypeFilters().includes(agentType)"
            class="px-2 py-1 text-xs rounded bg-gray-800 hover:bg-gray-700">
            {{ agentType }}
          </button>
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
          <span class="ml-2 text-gray-400">[{{ log.agentType }}]</span>
          <span class="ml-2" [innerHTML]="highlightSearch(log.content)"></span>
        </div>
      </cdk-virtual-scroll-viewport>
    </div>
  `
})
export class TaskLogsComponent {
  taskId = input.required<string>();

  searchQuery = '';
  logTypeFilters = signal<LogType[]>([]);
  agentTypeFilters = signal<AgentType[]>([]);

  logs = computed(() => this.logStore.getLogsForTask(this.taskId()));

  filteredLogs = computed(() => {
    let filtered = this.logs();

    // Filter by log type
    if (this.logTypeFilters().length > 0) {
      filtered = filtered.filter(log =>
        this.logTypeFilters().includes(log.type)
      );
    }

    // Filter by agent type
    if (this.agentTypeFilters().length > 0) {
      filtered = filtered.filter(log =>
        this.agentTypeFilters().includes(log.agentType)
      );
    }

    // Search filter
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

### AgentPoolService

Manages concurrent agent execution with budget enforcement, priority queue, and error retry.

```typescript
@Injectable()
export class AgentPoolService {
  private readonly maxConcurrent = this.configService.get('maxConcurrentAgents');
  private activeAgents = new Map<string, AgentInstance>();
  private taskQueue: PriorityQueue<QueuedTask> = new PriorityQueue();
  private paused = false;
  private singleOrchestratorLock = false;  // Only one orchestrator at a time

  constructor(
    private configService: ConfigService,
    private prisma: PrismaService,
    private sseGateway: EventsGateway,
    private agentRunner: AgentRunnerService,
    private budgetService: BudgetService,
    private errorClassifier: ErrorClassifierService
  ) {}

  async spawnAgent(taskId: string, agentType: AgentType): Promise<string | null> {
    // Check budget
    if (this.budgetService.isExceeded()) {
      this.taskQueue.enqueue({ taskId, agentType, priority: task.priority });
      this.sseGateway.emitPoolStatus(this.getPoolStatus());
      return null;
    }

    // Single orchestrator check
    if (agentType === AgentType.ORCHESTRATOR && this.singleOrchestratorLock) {
      this.taskQueue.enqueue({ taskId, agentType, priority: task.priority });
      return null;
    }

    // Concurrency limit check
    if (this.activeAgents.size >= this.maxConcurrent) {
      this.taskQueue.enqueue({ taskId, agentType, priority: task.priority });
      this.sseGateway.emitPoolStatus(this.getPoolStatus());
      return null;
    }

    const agentId = randomUUID();
    const agent: AgentInstance = {
      id: agentId,
      type: agentType,
      status: AgentStatus.RUNNING,
      taskId,
      startedAt: new Date(),
      currentAction: 'Initializing...',
      totalTasksCompleted: 0,
      totalTokensUsed: 0,
      tokensThisTask: 0
    };

    if (agentType === AgentType.ORCHESTRATOR) {
      this.singleOrchestratorLock = true;
    }

    this.activeAgents.set(agentId, agent);
    this.sseGateway.emitAgentStatusChanged(agent);

    // Run agent in background
    this.runAgent(agent).catch(err => {
      this.handleAgentError(agent, err);
    });

    return agentId;
  }

  private async runAgent(agent: AgentInstance): Promise<void> {
    const task = await this.prisma.task.findUnique({
      where: { id: agent.taskId }
    });

    const agentDef = this.getAgentDefinition(agent.type);
    const abortController = new AbortController();
    agent.abortController = abortController;

    try {
      for await (const message of query({
        prompt: this.buildPrompt(task, agent.type),
        options: {
          agents: { [agent.type]: agentDef },
          allowedTools: agentDef.tools,
          workingDirectory: this.configService.get('repositoryPath'),
          permissionMode: 'promptOnDemand',  // Prompt for tools not in allowed list
          signal: abortController.signal
        }
      })) {
        // Check budget mid-execution
        if (this.budgetService.wouldExceed(message.usage?.tokens || 0)) {
          // Auto-extend with notification
          await this.budgetService.extendBudget(message.usage.tokens);
          this.sseGateway.emitNotification({
            type: NotificationType.BUDGET_WARNING,
            message: `Budget extended for task ${task.title}`,
            taskId: task.id
          });
        }

        // Update token tracking
        if (message.usage?.tokens) {
          agent.tokensThisTask += message.usage.tokens;
          await this.budgetService.recordUsage(message.usage.tokens);
          this.sseGateway.emitAgentStatusChanged(agent);
          this.sseGateway.emitBudgetUpdate(this.budgetService.getStatus());
        }

        // Stream to frontend (hybrid: stream thinking, batch tool results)
        await this.processMessage(agent, task, message);
      }

      await this.completeAgent(agent.id, 'success');

    } catch (error) {
      await this.handleAgentError(agent, error);
    }
  }

  private async processMessage(
    agent: AgentInstance,
    task: Task,
    message: SDKMessage
  ): Promise<void> {
    // Update current action
    agent.currentAction = this.extractAction(message);
    this.sseGateway.emitAgentStatusChanged(agent);

    // Create log entry
    const logType = this.getLogType(message);
    const shouldStream = logType === LogType.THINKING;  // Hybrid: stream thinking

    const log = await this.prisma.taskLog.create({
      data: {
        taskId: task.id,
        agentId: agent.id,
        agentType: agent.type,
        type: logType,
        content: this.formatMessage(message),
        metadata: message
      }
    });

    // Emit to subscribed clients
    if (shouldStream) {
      this.sseGateway.emitTaskLog(task.id, log);  // Real-time
    } else {
      // Batch tool results (send when complete)
      this.sseGateway.emitTaskLog(task.id, log);
    }
  }

  private async handleAgentError(agent: AgentInstance, error: Error): Promise<void> {
    const errorType = this.errorClassifier.classify(error);

    // Update task with error
    await this.prisma.task.update({
      where: { id: agent.taskId },
      data: {
        hasError: true,
        errorMessage: error.message
      }
    });

    // Determine retry strategy based on error type
    const shouldRetry = await this.shouldRetryError(errorType, agent.taskId);

    if (shouldRetry) {
      // Re-queue task
      const task = await this.prisma.task.findUnique({ where: { id: agent.taskId } });
      this.taskQueue.enqueue({
        taskId: agent.taskId,
        agentType: agent.type,
        priority: task.priority,
        retryCount: (task.retryCount || 0) + 1
      });
    } else {
      // Mark task for human intervention
      await this.prisma.task.update({
        where: { id: agent.taskId },
        data: { state: PipelineState.BLOCKED }
      });

      this.sseGateway.emitNotification({
        type: NotificationType.AGENT_ERROR,
        message: `Agent failed on task: ${error.message}`,
        taskId: agent.taskId
      });
    }

    await this.completeAgent(agent.id, 'error');
  }

  private async shouldRetryError(errorType: ErrorType, taskId: string): Promise<boolean> {
    // Configurable per error type
    const retryConfig = {
      [ErrorType.NETWORK]: { maxRetries: 3, shouldRetry: true },
      [ErrorType.TIMEOUT]: { maxRetries: 2, shouldRetry: true },
      [ErrorType.RATE_LIMIT]: { maxRetries: 5, shouldRetry: true },
      [ErrorType.LOGIC_ERROR]: { maxRetries: 0, shouldRetry: false },
      [ErrorType.TOOL_ERROR]: { maxRetries: 1, shouldRetry: true },
      [ErrorType.UNKNOWN]: { maxRetries: 1, shouldRetry: true }
    };

    const config = retryConfig[errorType];
    const task = await this.prisma.task.findUnique({ where: { id: taskId } });
    const retryCount = task.retryCount || 0;

    return config.shouldRetry && retryCount < config.maxRetries;
  }

  private async completeAgent(agentId: string, status: 'success' | 'error'): Promise<void> {
    const agent = this.activeAgents.get(agentId);
    if (!agent) return;

    // Update stats
    if (status === 'success') {
      agent.totalTasksCompleted++;
    }

    // Release orchestrator lock
    if (agent.type === AgentType.ORCHESTRATOR) {
      this.singleOrchestratorLock = false;
    }

    this.activeAgents.delete(agentId);

    // Process queue (priority order, no preemption)
    if (!this.paused && this.taskQueue.length > 0) {
      const next = this.taskQueue.dequeue();  // Gets highest priority
      await this.spawnAgent(next.taskId, next.agentType);
    }

    this.sseGateway.emitAgentCompleted(agentId, agent.taskId);
    this.sseGateway.emitPoolStatus(this.getPoolStatus());
  }

  async pauseAll(): Promise<void> {
    this.paused = true;
    // Let running agents finish current message
    this.sseGateway.emitPoolStatus(this.getPoolStatus());
  }

  async resumeAll(): Promise<void> {
    this.paused = false;
    // Process queue
    while (this.taskQueue.length > 0 && this.activeAgents.size < this.maxConcurrent) {
      const next = this.taskQueue.dequeue();
      await this.spawnAgent(next.taskId, next.agentType);
    }
  }

  async pauseTask(taskId: string): Promise<void> {
    // Pause task and all its subtasks
    const task = await this.prisma.task.findUnique({
      where: { id: taskId },
      include: { childTasks: true }
    });

    // Abort agent if running
    const agent = Array.from(this.activeAgents.values())
      .find(a => a.taskId === taskId);
    if (agent?.abortController) {
      agent.abortController.abort();
    }

    // Abort subtask agents
    for (const subtask of task.childTasks) {
      const subtaskAgent = Array.from(this.activeAgents.values())
        .find(a => a.taskId === subtask.id);
      if (subtaskAgent?.abortController) {
        subtaskAgent.abortController.abort();
      }
    }
  }

  async abortAgent(agentId: string): Promise<void> {
    const agent = this.activeAgents.get(agentId);
    if (agent?.abortController) {
      agent.abortController.abort();
    }
    await this.completeAgent(agentId, 'error');
  }

  getPoolStatus(): PoolStatus {
    return {
      active: this.activeAgents.size,
      queued: this.taskQueue.items.map(item => ({
        taskId: item.taskId,
        agentType: item.agentType,
        priority: item.priority
      })),
      max: this.maxConcurrent,
      paused: this.paused,
      agents: Array.from(this.activeAgents.values())
    };
  }
}
```

### TaskOrchestrationService

Handles pipeline state transitions, agent coordination, and subtask management.

```typescript
@Injectable()
export class TaskOrchestrationService {
  constructor(
    private prisma: PrismaService,
    private agentPool: AgentPoolService,
    private sseGateway: EventsGateway,
    private gitService: GitService,
    private costEstimator: CostEstimatorService,
    private notificationService: NotificationService
  ) {}

  async createTask(dto: CreateTaskDto): Promise<Task> {
    // Run cost estimate first
    const estimate = await this.costEstimator.estimate(dto);

    const task = await this.prisma.task.create({
      data: {
        title: dto.title,
        description: dto.description,
        acceptanceCriteria: dto.acceptanceCriteria,
        edgeCases: dto.edgeCases,
        technicalConstraints: dto.technicalConstraints,
        relatedDocLinks: dto.relatedDocLinks,
        expectedOutcome: dto.expectedOutcome,
        state: PipelineState.BACKLOG,
        priority: dto.priority,
        estimatedTokens: estimate.estimatedTokens,
        tokensUsed: 0
      }
    });

    this.sseGateway.emitTaskCreated(task);
    return task;
  }

  async transitionTask(taskId: string, newState: PipelineState): Promise<Task> {
    const task = await this.prisma.task.findUnique({
      where: { id: taskId },
      include: { childTasks: true }
    });

    // Validate transition
    if (!this.isValidTransition(task.state, newState)) {
      throw new BadRequestException(`Cannot transition from ${task.state} to ${newState}`);
    }

    // For IMPLEMENTING state, check if another task is already implementing (MVP)
    if (newState === PipelineState.IMPLEMENTING) {
      const activeImplementation = await this.prisma.task.findFirst({
        where: {
          state: PipelineState.IMPLEMENTING,
          assignedAgentId: { not: null }
        }
      });

      if (activeImplementation) {
        throw new BadRequestException('Another task is already in implementation. Please wait.');
      }
    }

    // Update task
    const updated = await this.prisma.task.update({
      where: { id: taskId },
      data: {
        state: newState,
        updatedAt: new Date(),
        hasError: false,  // Clear error on manual transition
        errorMessage: null
      }
    });

    this.sseGateway.emitTaskUpdated(updated);

    // Send notification
    await this.notificationService.create({
      type: NotificationType.TASK_STATE_CHANGE,
      message: `${updated.title} → ${newState}`,
      taskId: updated.id
    });

    // Trigger agent for new state
    await this.triggerAgentForState(updated);

    return updated;
  }

  private async triggerAgentForState(task: Task): Promise<void> {
    const agentMapping: Record<PipelineState, AgentType | null> = {
      [PipelineState.BACKLOG]: null,
      [PipelineState.PLANNING]: AgentType.ORCHESTRATOR,
      [PipelineState.IMPLEMENTING]: null,  // Orchestrator creates subtasks
      [PipelineState.REVIEWING]: AgentType.SIMPLIFIER,  // Simplifier then reviewer
      [PipelineState.TESTING]: AgentType.TEST_AGENT,
      [PipelineState.PR_READY]: AgentType.PR_AGENT,  // Auto-create PR
      [PipelineState.DONE]: null,
      [PipelineState.BLOCKED]: null
    };

    const agentType = agentMapping[task.state];
    if (agentType) {
      await this.agentPool.spawnAgent(task.id, agentType);
    }
  }

  async handleOrchestratorResult(taskId: string, plan: OrchestratorPlan): Promise<void> {
    // Validate max 2 levels of nesting
    const parentTask = await this.prisma.task.findUnique({
      where: { id: taskId },
      include: { parentTask: true }
    });

    if (parentTask.parentTask) {
      throw new BadRequestException('Maximum subtask nesting depth (2) exceeded');
    }

    // Create subtasks from orchestrator output
    const subtaskIds: string[] = [];

    for (const subtask of plan.subtasks) {
      const created = await this.prisma.task.create({
        data: {
          title: subtask.title,
          description: subtask.description,
          acceptanceCriteria: subtask.acceptanceCriteria || [],
          edgeCases: [],
          technicalConstraints: parentTask.technicalConstraints,  // Inherit
          relatedDocLinks: parentTask.relatedDocLinks,  // Inherit
          state: PipelineState.IMPLEMENTING,
          priority: parentTask.priority,  // Always inherit
          parentTaskId: taskId,
          blockedBy: subtask.dependencies || [],
          tokensUsed: 0
        }
      });

      subtaskIds.push(created.id);
      this.sseGateway.emitTaskCreated(created);
    }

    // Update parent task
    await this.prisma.task.update({
      where: { id: taskId },
      data: {
        state: PipelineState.IMPLEMENTING,
        childTaskIds: subtaskIds
      }
    });

    // Spawn agents for unblocked subtasks only
    for (const subtaskId of subtaskIds) {
      const subtask = await this.prisma.task.findUnique({ where: { id: subtaskId } });

      if (subtask.blockedBy.length === 0) {
        // Determine agent type
        const agentType = this.determineAgentType(subtask);
        await this.agentPool.spawnAgent(subtask.id, agentType);
      }
    }
  }

  async handleReviewerResult(taskId: string, review: ReviewResult): Promise<void> {
    if (review.approved) {
      // Advance to testing
      await this.transitionTask(taskId, PipelineState.TESTING);
    } else {
      // Create revision subtasks (blocks parent in REVIEWING)
      for (const revisionSpec of review.revisionSubtasks) {
        const revision = await this.prisma.task.create({
          data: {
            title: revisionSpec.title,
            description: revisionSpec.description,
            acceptanceCriteria: ['Fix the issues described'],
            state: PipelineState.IMPLEMENTING,
            priority: parentTask.priority,  // Inherit
            parentTaskId: taskId
          }
        });

        // Assign to original agent type
        const originalAgentType = await this.getOriginalImplementationAgentType(taskId);
        await this.agentPool.spawnAgent(revision.id, originalAgentType);
      }

      // Parent stays in REVIEWING until revisions complete
    }
  }

  async handleTestResult(taskId: string, result: TestResult): Promise<void> {
    if (result.allPassed) {
      // Advance to PR_READY
      await this.transitionTask(taskId, PipelineState.PR_READY);
    } else {
      // Create revision subtasks for test failures
      for (const failure of result.failures) {
        const revision = await this.prisma.task.create({
          data: {
            title: `Fix failing test: ${failure.testName}`,
            description: failure.description,
            state: PipelineState.IMPLEMENTING,
            priority: parentTask.priority,
            parentTaskId: taskId
          }
        });

        const originalAgentType = await this.getOriginalImplementationAgentType(taskId);
        await this.agentPool.spawnAgent(revision.id, originalAgentType);
      }
    }
  }

  async handleSubtaskCompletion(subtaskId: string): Promise<void> {
    const subtask = await this.prisma.task.findUnique({
      where: { id: subtaskId },
      include: { parentTask: { include: { childTasks: true } } }
    });

    if (!subtask.parentTask) return;

    // Check if all sibling subtasks are done (strict)
    const allDone = subtask.parentTask.childTasks.every(
      child => child.state === PipelineState.DONE
    );

    if (allDone) {
      // Advance parent to next stage
      const nextState = this.getNextState(subtask.parentTask.state);
      await this.transitionTask(subtask.parentTask.id, nextState);
    } else {
      // Check if this subtask was blocking others
      const nowUnblocked = await this.prisma.task.findMany({
        where: {
          parentTaskId: subtask.parentTaskId,
          blockedBy: { has: subtaskId }
        }
      });

      for (const task of nowUnblocked) {
        // Remove this subtask from blockedBy
        const updatedBlockers = task.blockedBy.filter(id => id !== subtaskId);
        await this.prisma.task.update({
          where: { id: task.id },
          data: { blockedBy: updatedBlockers }
        });

        // If no longer blocked, spawn agent
        if (updatedBlockers.length === 0) {
          const agentType = this.determineAgentType(task);
          await this.agentPool.spawnAgent(task.id, agentType);
        }
      }
    }
  }

  async handleDynamicReplan(taskId: string, failedSubtaskId: string, error: Error): Promise<void> {
    // Trigger orchestrator replanning
    const task = await this.prisma.task.findUnique({
      where: { id: taskId },
      include: { childTasks: true }
    });

    const replanContext = {
      originalPlan: task.metadata.originalPlan,
      failedSubtask: await this.prisma.task.findUnique({ where: { id: failedSubtaskId } }),
      error: error.message,
      completedSubtasks: task.childTasks.filter(t => t.state === PipelineState.DONE),
      inProgressSubtasks: task.childTasks.filter(t => t.state !== PipelineState.DONE)
    };

    // Spawn orchestrator in replan mode
    await this.agentPool.spawnAgent(taskId, AgentType.ORCHESTRATOR, {
      mode: 'replan',
      context: replanContext
    });
  }
}
```

### GitService

Manages git operations for feature branches (MVP: simple branches, one task at a time).

```typescript
@Injectable()
export class GitService {
  private git: SimpleGit;
  private activeImplementationBranch: string | null = null;

  constructor(private configService: ConfigService) {
    this.git = simpleGit(configService.get('repositoryPath'));
  }

  async createFeatureBranch(taskId: string, title: string): Promise<string> {
    const branchName = `${this.configService.get('branchPrefix')}${taskId}-${slugify(title)}`;
    const defaultBranch = this.configService.get('defaultBranch');

    // MVP: Only one implementation branch at a time
    if (this.activeImplementationBranch) {
      throw new Error('Another task is currently in implementation. Please wait.');
    }

    await this.git.checkout(defaultBranch);
    await this.git.pull();
    await this.git.checkoutLocalBranch(branchName);

    this.activeImplementationBranch = branchName;

    return branchName;
  }

  async commitChanges(message: string): Promise<void> {
    await this.git.add('.');
    await this.git.commit(message);
  }

  async pushBranch(branch: string): Promise<void> {
    await this.git.push('origin', branch, ['--set-upstream']);
  }

  async releaseBranch(): Promise<void> {
    this.activeImplementationBranch = null;
  }

  // Startup: Scan for orphaned branches and clean up
  async cleanupOrphanedBranches(): Promise<void> {
    const branches = await this.git.branchLocal();
    const prefix = this.configService.get('branchPrefix');

    for (const branch of branches.all) {
      if (branch.startsWith(prefix)) {
        // Check if task exists in database
        const taskId = this.extractTaskIdFromBranch(branch);
        const task = await this.prisma.task.findUnique({ where: { id: taskId } });

        if (!task) {
          // Orphaned branch, delete it
          await this.git.deleteLocalBranch(branch, true);
        }
      }
    }
  }
}
```

### BudgetService

Manages global daily/monthly token budgets with auto-extend and notifications.

```typescript
@Injectable()
export class BudgetService {
  constructor(
    private configService: ConfigService,
    private prisma: PrismaService,
    private notificationService: NotificationService
  ) {}

  async recordUsage(tokens: number): Promise<void> {
    const config = await this.getConfig();

    config.currentDailyUsage += tokens;
    config.currentMonthlyUsage += tokens;

    // Check thresholds
    const dailyPercent = (config.currentDailyUsage / config.dailyTokenBudget) * 100;
    const monthlyPercent = (config.currentMonthlyUsage / config.monthlyTokenBudget) * 100;

    if (dailyPercent >= 80 && !config.dailyWarningShown) {
      await this.notificationService.create({
        type: NotificationType.BUDGET_WARNING,
        message: `Daily budget 80% used (${config.currentDailyUsage}/${config.dailyTokenBudget})`
      });
      config.dailyWarningShown = true;
    }

    if (monthlyPercent >= 80 && !config.monthlyWarningShown) {
      await this.notificationService.create({
        type: NotificationType.BUDGET_WARNING,
        message: `Monthly budget 80% used (${config.currentMonthlyUsage}/${config.monthlyTokenBudget})`
      });
      config.monthlyWarningShown = true;
    }

    // Hard limit reached
    if (config.currentDailyUsage >= config.dailyTokenBudget) {
      config.budgetExceeded = true;
      await this.notificationService.create({
        type: NotificationType.BUDGET_EXCEEDED,
        message: 'Daily budget exceeded - agents paused'
      });
    }

    await this.saveConfig(config);
  }

  async extendBudget(additionalTokens: number): Promise<void> {
    // Auto-extend logic
    const config = await this.getConfig();
    config.currentDailyUsage += additionalTokens;
    config.currentMonthlyUsage += additionalTokens;
    await this.saveConfig(config);
  }

  isExceeded(): boolean {
    // Check if budget is exceeded (pauses new spawns)
    return this.config.budgetExceeded;
  }

  wouldExceed(tokens: number): boolean {
    const config = this.config;
    return (config.currentDailyUsage + tokens) > config.dailyTokenBudget;
  }

  // Daily reset job (cron)
  async resetDaily(): Promise<void> {
    const config = await this.getConfig();
    config.currentDailyUsage = 0;
    config.dailyWarningShown = false;
    config.budgetExceeded = false;
    await this.saveConfig(config);
  }

  // Monthly reset job (cron)
  async resetMonthly(): Promise<void> {
    const config = await this.getConfig();
    config.currentMonthlyUsage = 0;
    config.monthlyWarningShown = false;
    await this.saveConfig(config);
  }

  getStatus(): BudgetStatus {
    return {
      dailyUsage: this.config.currentDailyUsage,
      dailyLimit: this.config.dailyTokenBudget,
      monthlyUsage: this.config.currentMonthlyUsage,
      monthlyLimit: this.config.monthlyTokenBudget,
      exceeded: this.config.budgetExceeded
    };
  }
}
```

### PatternsService

Manages repository-specific knowledge base (CLAUDE.md).

```typescript
@Injectable()
export class PatternsService {
  constructor(
    private configService: ConfigService,
    private gitService: GitService
  ) {}

  async updatePatterns(): Promise<void> {
    // Manual trigger only
    // Analyzes recent merged PRs and updates CLAUDE.md

    const repoPath = this.configService.get('repositoryPath');
    const claudePath = path.join(repoPath, 'CLAUDE.md');

    // Get recent merged PRs (last 30 days)
    const mergedPRs = await this.getRecentMergedPRs();

    // Extract patterns using cost estimator (cheap model)
    const patterns = await this.extractPatterns(mergedPRs);

    // Update CLAUDE.md
    const existingContent = await fs.readFile(claudePath, 'utf-8').catch(() => '');
    const updatedContent = this.mergePatterns(existingContent, patterns);

    await fs.writeFile(claudePath, updatedContent);

    // Commit the update
    await this.gitService.commitChanges('Update CLAUDE.md with learned patterns');
  }

  private async extractPatterns(prs: PR[]): Promise<Pattern[]> {
    // Use Haiku to analyze PRs and extract patterns
    const prompt = `Analyze these merged PRs and extract coding patterns, conventions, and gotchas:

${prs.map(pr => `PR: ${pr.title}\n${pr.description}\nFiles: ${pr.files.join(', ')}`).join('\n\n')}

Output format:
{
  "patterns": [
    {
      "category": "architecture|conventions|gotchas|best-practices",
      "title": "Clear pattern title",
      "description": "Detailed description with examples",
      "files": ["relevant/file/patterns"]
    }
  ]
}`;

    // Query with Haiku
    const result = await query({ prompt, model: 'haiku' });
    return JSON.parse(result);
  }

  private mergePatterns(existing: string, newPatterns: Pattern[]): string {
    // Intelligent merge of new patterns into existing CLAUDE.md
    // Deduplicates, organizes by category, maintains structure
    // ... implementation
  }
}
```

---

## Database Schema (Prisma)

```prisma
// prisma/schema.prisma

datasource db {
  provider = "postgresql"
  url      = env("DATABASE_URL")
}

generator client {
  provider = "prisma-client-js"
}

model Task {
  id                    String        @id @default(uuid())
  title                 String

  // Structured fields
  description           String
  acceptanceCriteria    String[]
  edgeCases             String[]
  technicalConstraints  String[]
  relatedDocLinks       String[]
  expectedOutcome       String?

  state                 PipelineState @default(BACKLOG)
  priority              Priority      @default(MEDIUM)

  // Relationships
  parentTaskId          String?
  parentTask            Task?         @relation("TaskSubtasks", fields: [parentTaskId], references: [id])
  childTasks            Task[]        @relation("TaskSubtasks")
  childTaskIds          String[]      @default([])
  blockedBy             String[]      @default([])

  // Agent tracking
  assignedAgentId       String?
  agentType             AgentType?

  // Git integration
  branch                String?
  prUrl                 String?

  // Budget
  estimatedTokens       Int?
  tokensUsed            Int           @default(0)

  // Error tracking
  hasError              Boolean       @default(false)
  errorMessage          String?
  retryCount            Int           @default(0)

  // Logs
  logs                  TaskLog[]

  // Metadata
  metadata              Json?         // For storing orchestrator plans, etc.

  // Timestamps
  createdAt             DateTime      @default(now())
  updatedAt             DateTime      @updatedAt
  completedAt           DateTime?
}

model TaskLog {
  id        String    @id @default(uuid())
  taskId    String
  task      Task      @relation(fields: [taskId], references: [id], onDelete: Cascade)
  agentId   String
  agentType AgentType
  timestamp DateTime  @default(now())
  type      LogType
  content   String    @db.Text
  metadata  Json?

  @@index([taskId])
  @@index([timestamp])
}

model Config {
  id                   String          @id @default("default")

  // Concurrency
  maxConcurrentAgents  Int             @default(3)

  // Budget
  dailyTokenBudget     Int             @default(100000)
  monthlyTokenBudget   Int             @default(5000000)
  currentDailyUsage    Int             @default(0)
  currentMonthlyUsage  Int             @default(0)
  budgetExceeded       Boolean         @default(false)
  dailyWarningShown    Boolean         @default(false)
  monthlyWarningShown  Boolean         @default(false)

  // Repository
  repositoryPath       String
  defaultBranch        String          @default("main")
  branchPrefix         String          @default("feature/agent-")

  // Protected paths
  protectedPaths       String[]        @default([".git/", ".env", "node_modules/"])

  // Automation
  autoAdvance          Boolean         @default(true)
  requireApproval      PipelineState[]

  // Patterns
  patternsFile         String          @default("CLAUDE.md")
}

model Notification {
  id        String           @id @default(uuid())
  type      NotificationType
  taskId    String?
  message   String
  timestamp DateTime         @default(now())
  read      Boolean          @default(false)

  @@index([timestamp])
}

enum PipelineState {
  BACKLOG
  PLANNING
  IMPLEMENTING
  REVIEWING
  TESTING
  PR_READY
  DONE
  BLOCKED
}

enum Priority {
  LOW
  MEDIUM
  HIGH
  CRITICAL
}

enum AgentType {
  ORCHESTRATOR
  UI_AGENT
  API_AGENT
  SIMPLIFIER
  CODE_REVIEWER
  TEST_AGENT
  PR_AGENT
  COST_ESTIMATOR
}

enum LogType {
  INFO
  TOOL_USE
  TOOL_RESULT
  ERROR
  THINKING
}

enum NotificationType {
  TASK_STATE_CHANGE
  PR_CREATED
  PR_MERGED
  BUDGET_WARNING
  BUDGET_EXCEEDED
}
```

---

## Implementation Phases

### Phase 1: Foundation (MVP)

**Goal**: Basic working pipeline with manual state transitions and simple git integration.

**Scope**:
1. Set up Nx monorepo with Angular + NestJS
2. Implement Prisma schema and database
3. Create basic REST API for tasks CRUD with structured fields
4. Build Kanban board UI (no drag-and-drop, click to view details)
5. Add EventSource/SSE for real-time updates (full state)
6. Implement single orchestrator agent execution
7. Basic git integration (branches only, one task at a time)
8. Budget widget on dashboard
9. Notifications panel
10. Task detail view with transition buttons
11. Virtual scrolling log viewer with search/filters
12. Cost estimation before task creation (Haiku)

**Deliverables**:
- Working Kanban board with fixed 7 states
- Create/edit/delete tasks with structured fields
- Manual state transitions via buttons in detail view
- Single orchestrator can execute (Opus)
- Real-time log streaming (hybrid: stream thinking, batch tools)
- Simple git branches, one active implementation at a time
- Global budget tracking with auto-extend
- Notifications for state changes, budget warnings

### Phase 2: Full Agent Orchestration

**Goal**: Multi-agent coordination with all specialist agents.

**Scope**:
1. Implement AgentPoolService with concurrency control (max N)
2. Add all agent definitions (UI, API, Simplifier, Reviewer, Test, PR)
3. Build TaskOrchestrationService for automatic transitions
4. Implement subtask creation from orchestrator (max 2 levels)
5. Dependency graph tracking with blocking
6. Agent monitoring UI with status + action + tokens
7. Priority queue without preemption
8. Error retry with configurable per-type logic
9. Dynamic replanning on failures
10. Revision subtask workflow

**Deliverables**:
- Concurrent agent execution (configurable max, default 3)
- Priority-based task queue
- All specialist agents working
- Automatic flow: orchestrator → specialists → review → test → PR
- Real-time agent status monitoring
- Revision cycles for review/test failures
- Error handling with smart retries

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
DATABASE_URL="postgresql://user:password@localhost:5432/agent_dashboard"

# Claude API
ANTHROPIC_API_KEY="sk-ant-..."

# Git
REPOSITORY_PATH="/path/to/your/repo"
GITHUB_TOKEN="ghp_..."  # For PR creation

# Server
PORT=3000
NODE_ENV="development"

# Budget
DAILY_TOKEN_BUDGET=100000
MONTHLY_TOKEN_BUDGET=5000000

# Agent Pool
MAX_CONCURRENT_AGENTS=3

# Protected Paths (comma-separated)
PROTECTED_PATHS=".git/,.env,node_modules/"
```

---

## Getting Started Commands

```bash
# Create workspace
npx create-nx-workspace@latest agent-dashboard --preset=angular-monorepo --appName=frontend

# Add NestJS
nx add @nx/nest
nx g @nx/nest:app backend

# Add shared library
nx g @nx/js:lib shared

# Install dependencies
npm install @anthropic-ai/claude-agent-sdk
npm install @nestjs/platform-socket.io
npm install @prisma/client prisma
npm install simple-git
npm install -D tailwindcss postcss autoprefixer

# Frontend deps
npm install @angular/cdk

# Initialize Prisma
npx prisma init
npx prisma generate
npx prisma migrate dev

# Start development
nx run-many -t serve -p frontend,backend

# Run pattern update manually
curl -X POST http://localhost:3000/api/patterns/update
```

---

## Success Criteria

1. **Functional**: Tasks flow from Backlog to Done with agent automation
2. **Concurrent**: Multiple agents work in parallel up to configured limit
3. **Observable**: All agent activity visible in real-time with detailed logs
4. **Controllable**: Manual overrides and pause controls at all levels
5. **Reliable**: Graceful error handling with smart retries
6. **Budget-aware**: Hard limits prevent runaway costs
7. **Integrated**: PRs created automatically, tasks auto-complete on merge
8. **Learning**: Pattern database improves agent performance over time

---

## Design Decisions Summary

### Error Handling
- **Retry strategy**: Configurable per error type using Agent SDK error classification
- Network/timeout errors: Auto-retry with backoff
- Logic errors: Human intervention required
- Max retry counts prevent infinite loops

### State Management
- **Pipeline**: Fixed 7-state workflow, no customization
- **Transitions**: Buttons in task detail view only (no drag-and-drop)
- **Completion**: Auto-DONE when PR merged (GitHub webhook)

### Concurrency
- **Orchestrator**: Single instance only (sequential planning)
- **Other agents**: Configurable pool (default 3)
- **Queue**: Priority-based without preemption
- **MVP**: One implementation task at a time

### Budget
- **Global limits**: Daily and monthly budgets
- **Enforcement**: Hard limits pause new agents
- **Overflow**: Auto-extend with notification
- **Warnings**: At 80% threshold

### Git Workflow
- **MVP**: Simple branches, one active at a time
- **Future**: Worktrees for true parallelism
- **Protected paths**: .git/, .env, node_modules/ (read-only)
- **Cleanup**: Startup scan for orphaned branches

### Real-time Updates
- **Protocol**: EventSource/SSE (not WebSocket)
- **Payload**: Full state every time (not deltas)
- **Streaming**: Hybrid - stream thinking, batch tool results
- **Slow clients**: Buffer with limit

### Agent Coordination
- **Communication**: Orchestrator only (no direct agent-to-agent)
- **Dependencies**: Explicit graph with blocking (max 2 levels)
- **Replanning**: Dynamic on failures only
- **Reviews**: Create revision subtasks that block parent

### Knowledge Base
- **Scope**: Per-repository (CLAUDE.md)
- **Updates**: Manual trigger only
- **Learning**: Extract from merged PRs

### UI/UX
- **Board**: Fixed states, no filtering, no persistence
- **Logs**: Virtual scrolling with search and type/agent filters
- **Errors**: Red border on task card
- **Notifications**: Dedicated panel for state changes, budget
- **Budget**: Dashboard widget always visible

---

## Open Questions / Future Considerations

1. **Multi-repo support**: Workspace concept for managing multiple repositories
2. **Worktrees**: Replace MVP branch strategy for true parallel implementation
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
- MVP uses simple branches with one task at a time
- Protected paths prevent critical file modification
- All changes reviewed before PR
- Future: Worktrees provide isolation

### Risk: Runaway agent costs
**Mitigation**:
- Hard budget limits pause agents
- Cost estimation before task creation
- Token tracking per task and globally
- Warnings at 80% threshold

### Risk: System state inconsistency
**Mitigation**:
- Graceful shutdown for migrations
- Error retry with exponential backoff
- Human intervention for unrecoverable errors
- Startup cleanup of orphaned state

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

**Specification Version**: 2.0
**Last Updated**: 2026-01-24
**Status**: Ready for Implementation
