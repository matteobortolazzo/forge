# Project Overview

## Mission

Forge is an AI Agent Dashboard for orchestrating and monitoring AI coding agents powered by Claude Code CLI.

## Architecture

The system implements a pipeline where tasks flow through stages:

```
Backlog → Split → Research → Planning → Implementing → Simplifying → Verifying → Reviewing → PrReady → Done
```

Agents are executed via stdin/stdout communication with the Claude Code process. The pipeline supports:
- **Human-in-the-loop oversight** through conditional and mandatory approval gates
- **Confidence-based escalation** when agent confidence falls below threshold
- **Git worktree isolation** for parallel subtask execution

## Key Terminology

| Term | Definition |
|------|------------|
| **Human Gate** | Approval checkpoint requiring human review before proceeding (conditional or mandatory) |
| **Artifact** | Structured output from an agent (plan, research findings, implementation details) |
| **Worktree** | Isolated git working directory for parallel subtask execution |
| **Subtask** | Child task created from splitting a parent task |
| **Derived State** | Parent task state computed from children's states |
| **Confidence Score** | Agent-reported confidence (0.0-1.0) that triggers gates when below threshold |

## Pipeline Flow Diagram

```
┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│   1. SPLIT   │───▶│  2. RESEARCH │───▶│   3. PLAN    │───▶│ 4. IMPLEMENT │
│   (± Human)  │    │              │    │   (± Human)  │    │  (retry loop)│
└──────────────┘    └──────────────┘    └──────────────┘    └──────────────┘
                                                                    │
┌──────────────┐    ┌──────────────┐    ┌──────────────┐            │
│   7. PR      │◀───│  6. VERIFY   │◀───│ 5. SIMPLIFY  │◀───────────┘
│   (Human)    │    │              │    │ (sep. agent) │
└──────────────┘    └──────────────┘    └──────────────┘
```

## How the Pipeline Works

1. **Task enters schedulable state** → Scheduler picks highest-priority leaf task
2. **Human gate check** → If task has pending gate, wait for approval
3. **Orchestrator selects agent** → Matches task state and detects repository context
4. **Variant selection** → If a framework-specific variant exists (e.g., Angular), it's used
5. **Prompt assembly** → Template variables filled with task data, subtask context, and previous artifacts
6. **Agent execution** → Claude Code CLI runs in worktree (for subtasks) with assembled prompt
7. **Artifact parsing** → Extract confidence score, structured output, human input requests
8. **Human gate trigger** → If confidence < threshold, create gate and pause task
9. **State transition** → Task moves to next state based on agent recommendation
10. **Parent state update** → If subtask, parent's derived state is recomputed
