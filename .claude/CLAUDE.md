# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Forge is an AI Agent Dashboard for orchestrating and monitoring AI coding agents powered by Claude Code CLI. The system implements a **two-tier pipeline** with separate lifecycles for backlog items and tasks:

**Backlog Item Pipeline** (specification refinement and decomposition):
```
New → Refining → Ready → Splitting → Executing → Done
```

**Task Pipeline** (implementation of individual work units):
```
Research → Planning → Implementing → Simplifying → Verifying → Reviewing → PrReady → Done
```

Backlog items represent high-level features or changes. During **Refining**, Claude asks clarifying questions and improves specifications. Once **Ready**, the **Splitting** agent decomposes items into PR-sized tasks. Tasks then flow through their own pipeline with agents for research, planning, implementation, and review.

The system supports human-in-the-loop oversight through conditional and mandatory approval gates, confidence-based escalation, and git worktree isolation for parallel task execution.

## Repository Structure

```
forge/
├── agents/                         # Agent configuration (YAML)
│   ├── defaults/                   # Default agents for each pipeline state
│   │   ├── refining.yml            # Refining agent (backlog item specification refinement)
│   │   ├── split.yml               # Split agent (backlog item decomposition into tasks)
│   │   ├── research.yml            # Research agent (codebase analysis)
│   │   ├── planning.yml            # Planning agent (test-first design)
│   │   ├── implementing.yml        # Implementation agent (code generation)
│   │   ├── simplifying.yml         # Simplifying agent (over-engineering review)
│   │   ├── verifying.yml           # Verifying agent (comprehensive verification)
│   │   ├── reviewing.yml           # Review agent (code review)
│   │   └── documenting.yml         # Documentation maintenance agent
│   └── variants/                   # Framework-specific variants
│       ├── implementing.angular.yml
│       ├── implementing.dotnet.yml
│       └── reviewing.angular.yml
├── src/
│   ├── Forge.Api/                  # .NET 10 API Solution
│   │   ├── Forge.Api/              # Main API project
│   │   │   ├── Features/           # Backlog, Task, Agent, Events, Scheduler endpoints
│   │   │   │   ├── Backlog/        # Backlog item CRUD, refinement, splitting, logs
│   │   │   │   ├── Tasks/          # Task CRUD, transitions, logs, agent start, pause/resume, artifacts
│   │   │   │   ├── Agents/         # Agent orchestration, config loading, context detection
│   │   │   │   ├── Agent/          # Agent status, runner service
│   │   │   │   ├── Scheduler/      # Automatic backlog/task scheduling with human gates
│   │   │   │   ├── HumanGates/     # Human gate management and resolution
│   │   │   │   ├── Worktree/       # Git worktree isolation for tasks
│   │   │   │   ├── Events/         # SSE endpoint
│   │   │   │   └── Mock/           # Mock control endpoints (E2E only)
│   │   │   ├── Data/               # EF Core DbContext, Entities
│   │   │   ├── Shared/             # Enums, common types
│   │   │   └── Program.cs          # Entry point
│   │   ├── Claude.CodeSdk/         # C# SDK for Claude Code CLI
│   │   │   └── Mock/               # Mock client for E2E testing
│   │   └── tests/                  # Test projects
│   └── Forge.Ui/                   # Angular 21 SPA
│       ├── e2e/                    # Playwright E2E tests
│       └── src/app/
│           ├── features/           # Feature folders
│           │   ├── backlog/        # Backlog item list, detail, create dialog
│           │   ├── queue/          # Task queue view (table with filtering/sorting)
│           │   ├── task-detail/    # Task detail view with logs
│           │   └── notifications/  # Notification panel
│           ├── core/               # Stores, Services, Mocks
│           ├── shared/             # Reusable components, models
│           │   └── components/
│           │       ├── repository-sidebar/           # Repository selection sidebar
│           │       ├── add-repository-dialog/        # Add repository dialog
│           │       └── repository-settings-dialog/   # Repository settings dialog
│           └── app.routes.ts       # Route configuration
```

**Code Organization**: Both API and UI use feature folder organization. Each feature contains all necessary files (endpoints, services, components, models).

## Tech Stack

| Component         | Technology                       | Version  |
|-------------------|----------------------------------|----------|
| Frontend          | Angular                          | 21.x     |
| State Management  | Angular Signals                  | Built-in |
| UI Components     | Angular CDK                      | Latest   |
| Styling           | Tailwind CSS                     | 4.x      |
| Backend           | .NET                             | 10.x     |
| Backend Framework | ASP.NET Core Minimal APIs        | 10.x     |
| Real-time         | EventSource/SSE                  | Native   |
| Database          | SQLite                           | -        |
| ORM               | Entity Framework Core            | 10.x     |
| Agent Execution   | Claude Code CLI                  | Latest   |
| Frontend Testing  | Vitest                           | 4.x      |
| E2E Testing       | Playwright                       | 1.x      |

## Documentation Sources

### Context7 MCP

| Technology          | Context7 Library ID                      |
|---------------------|------------------------------------------|
| .NET / ASP.NET Core | `/websites/learn_microsoft_en-us_dotnet` |

### Internal Documentation

| Documentation          | Path                                     | Content                                         |
|------------------------|------------------------------------------|-------------------------------------------------|
| Claude.CodeSdk         | `src/Forge.Api/Claude.CodeSdk/README.md` | SDK for Claude Code CLI interaction             |
| UI Implementation      | `src/Forge.Ui/README.md`                 | Component inventory, stores, services, patterns |
| Angular Best Practices | `src/Forge.Ui/CLAUDE.md`                 | Coding standards and conventions                |
| API Integration        | `src/Forge.Ui/API-INTEGRATION.md`        | Endpoints, SSE events, data models              |

## Rule Files Reference

Detailed documentation is organized into rule files that load on-demand:

| Rule File | When to Use |
|-----------|-------------|
| `rules/00-project-overview.md` | Understanding pipeline architecture, terminology |
| `rules/01-backend-patterns.md` | Implementing .NET Minimal API endpoints, services |
| `rules/02-frontend-patterns.md` | Implementing Angular components, signals, SSE |
| `rules/03-agent-pipeline.md` | Creating/modifying YAML agent configs, templates |
| `rules/04-testing.md` | Writing Vitest, integration, or E2E tests |
| `rules/05-api-reference.md` | Looking up REST endpoint paths |
| `rules/06-data-models.md` | Understanding enums, entity fields |
| `rules/07-sse-events.md` | Implementing real-time event handling |
| `rules/08-development-commands.md` | Running, building, testing the app |
| `rules/09-critical-mistakes.md` | Avoiding common pitfalls |

## Critical Constraints

### Frontend Testing: Vitest Only
- Use `vi.fn()` NOT `jasmine.createSpy()`
- Use `vi.spyOn()` NOT `spyOn()`
- Import from `vitest` when explicit imports needed

### Angular 21 Conventions
- **Standalone Components**: All components standalone (no NgModules)
- **Control Flow**: Use @if, @for, @switch (NOT *ngIf, *ngFor)
- **Signals**: Use signals for reactive state
- **Zoneless**: Application runs in zoneless mode

### Backend Service Lifetimes
- **Scoped**: DbContext-dependent services (BacklogService, TaskService, HumanGateService)
- **Singleton**: Stateless or global state services (SseService, AgentRunnerService)
- **Hosted**: Background processing (TaskSchedulerService)
- Never inject scoped into singleton directly; use IServiceScopeFactory

### SSE Events
- Always emit SSE events after state changes
- Use full state payloads (not deltas)
- See `rules/07-sse-events.md` for event types

### Human Gates
- Check `HasPendingGate` before backlog item or task transitions
- Gates triggered by confidence < 0.7 or mandatory config
- Backlog gates: refining (conditional), split (conditional)
- Task gates: planning (conditional), pr (mandatory)

## Claude.CodeSdk

C# SDK for programmatic interaction with Claude Code CLI. Read `src/Forge.Api/Claude.CodeSdk/README.md` before implementing agent execution features.

Key classes:
- `ClaudeAgentClient` - Main client for spawning and managing CLI processes
- `ClaudeAgentOptions` - Configuration (working directory, permission mode, MCP servers)
- `IMessage` - Base interface for `SystemMessage`, `UserMessage`, `AssistantMessage`, `ResultMessage`
- `IContentBlock` - Base interface for `TextBlock`, `ToolUseBlock`, `ToolResultBlock`

## Quick Commands

```bash
# Backend (from src/Forge.Api/Forge.Api)
dotnet run                    # Run API
dotnet watch run              # Run with hot reload

# Frontend (from src/Forge.Ui)
ng serve                      # Run dev server
ng test                       # Run Vitest tests

# E2E (backend in mock mode)
dotnet run --launch-profile e2e   # Start mock backend
npm run e2e                        # Run Playwright tests
```

## Environment Variables

```env
DATABASE_PATH="forge.db"
CLAUDE_CODE_PATH="claude"
ASPNETCORE_URLS="http://localhost:5000"
CLAUDE_MOCK_MODE="true"         # Enable mock Claude client for E2E testing
AGENTS_PATH="./agents"          # Optional: custom path to agents directory
```

## Multi-Repository Support

Forge supports managing multiple repositories. Key concepts:

- **Repository Entity**: Each repository has a name, path, and cached git info (branch, commit, dirty state)
- **Backlog & Task Association**: All backlog items and tasks belong to a repository (required `RepositoryId`)
- **API Structure**: Endpoints scoped under `/api/repositories/{repoId}/backlog` and `/api/repositories/{repoId}/tasks`
- **UI Selection**: Frontend persists selected repository in localStorage (`forge:selectedRepositoryId`)
- **First-Run**: UI prompts to add a repository if none exist
- **Default Repository**: One repository can be marked as default for new backlog items

### UI Components

- **RepositorySidebarComponent**: Discord-style vertical sidebar for repository selection with initials display
- **AddRepositoryDialogComponent**: Modal form for adding new repositories (name, path, set as default)
- **RepositorySettingsDialogComponent**: Repository management (refresh git info, set default, delete with confirmation)

See `rules/05-api-reference.md` for endpoint structure.
