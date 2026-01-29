# Project Overview

## Mission

Forge is an AI Agent Dashboard for orchestrating and monitoring AI coding agents powered by Claude Code CLI.

## Architecture

The system implements a **two-tier pipeline** separating backlog items (specifications) from tasks (implementation units):

**Backlog Item Pipeline** (specification refinement and decomposition):
```
New → Refining → Ready → Splitting → Executing → Done
```

**Task Pipeline** (simplified implementation lifecycle):
```
Planning → Implementing → PrReady
```

- **Planning**: Research codebase patterns AND design test-first implementation plan
- **Implementing**: Write tests first, implement code, verify, YAGNI check, update docs
- **PrReady**: Final state - task is ready for PR creation (user reviews on git provider)

Agents are executed via stdin/stdout communication with the Claude Code process. The pipeline supports:
- **Human-in-the-loop oversight** through conditional approval gates (planning only for tasks)
- **Confidence-based escalation** when agent confidence falls below threshold
- **Git worktree isolation** for parallel task execution

## Key Terminology

| Term | Definition |
|------|------------|
| **Backlog Item** | High-level feature or change to implement, refined and split into tasks |
| **Task** | PR-sized implementation unit created from splitting a backlog item |
| **Human Gate** | Approval checkpoint requiring human review before proceeding (conditional or mandatory) |
| **Artifact** | Structured output from an agent (refined spec, task split, plan, implementation details) |
| **Worktree** | Isolated git working directory for parallel task execution |
| **Confidence Score** | Agent-reported confidence (0.0-1.0) that triggers gates when below threshold |
| **Execution Order** | Sequential order for tasks within a backlog item |

## Pipeline Flow Diagram

### Backlog Item Flow
```
┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│    1. NEW    │───▶│  2. REFINING │───▶│   3. READY   │───▶│ 4. SPLITTING │
│              │    │   (± Human)  │    │              │    │   (± Human)  │
└──────────────┘    └───────┬──────┘    └──────────────┘    └──────┬───────┘
                            │                                       │
                            └──────── (loop for clarification) ─────┤
                                                                    │
                    ┌──────────────┐    ┌──────────────┐            │
                    │    6. DONE   │◀───│ 5. EXECUTING │◀───────────┘
                    │              │    │ (tasks run)  │   (creates tasks)
                    └──────────────┘    └──────────────┘
```

### Task Flow (per task created from split)
```
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  1. PLANNING │───▶│ 2. IMPLEMENT │───▶│  3. PR_READY │
│ Research +   │    │ Tests, Code, │    │   (Final)    │
│   (± Human)  │    │ Verify, Docs │    │              │
└──────────────┘    └──────────────┘    └──────────────┘
```

## How the Pipeline Works

### Backlog Item Processing
1. **Backlog item created** → User adds feature/change with description
2. **Refining phase** → Refining agent asks clarifying questions, improves specifications
3. **Human gate (optional)** → If confidence < threshold, await approval
4. **Ready state** → User confirms spec is complete
5. **Splitting phase** → Split agent decomposes into PR-sized tasks with execution order
6. **Human gate (optional)** → If confidence < threshold, await approval on task breakdown
7. **Tasks created** → Each task starts in Planning state
8. **Executing state** → Backlog item tracks task progress
9. **Done** → Auto-transitions when all tasks reach PrReady

### Task Processing
1. **Task enters schedulable state** → Scheduler picks highest-priority task (respects execution order)
2. **Human gate check** → If task has pending gate, wait for approval
3. **Orchestrator selects agent** → Matches task state and detects repository context
4. **Variant selection** → If a framework-specific variant exists (e.g., Angular), it's used
5. **Prompt assembly** → Template variables filled with task data and previous artifacts
6. **Agent execution** → Claude Code CLI runs with assembled prompt
7. **Artifact parsing** → Extract confidence score, structured output, human input requests
8. **Human gate trigger** → If confidence < threshold, create gate and pause task
9. **State transition** → Task moves to next state based on agent recommendation
10. **Backlog item update** → Parent backlog item tracks completion progress
