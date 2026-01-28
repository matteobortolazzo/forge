# Agent Pipeline Architecture

The agent pipeline uses state-specific agents with YAML configuration. Each pipeline state has a dedicated agent with specialized prompts and optional framework-specific variants.

## YAML Configuration Schema

Agent configurations are stored in `agents/defaults/` (required) and `agents/variants/` (optional).

```yaml
# agents/defaults/planning.yml
id: planning-default
name: Planning Agent
state: Planning
description: Breaks down tasks into actionable implementation steps

prompt: |
  You are a planning agent. Your goal is to analyze the task and create
  a detailed implementation plan.

  ## Task
  **Title:** {task.title}
  **Description:** {task.description}

  ## Previous Artifacts
  {artifacts}

  ## Output Format
  Provide your plan in structured markdown...

output:
  type: plan
  schema: |
    # Implementation Plan
    ## Summary
    ## Affected Files
    ## Implementation Steps

mcp_servers:
  - context7

max_turns: 30
```

## Variant Configuration

Variants extend default agents with framework-specific prompts and matching rules:

```yaml
# agents/variants/implementing.angular.yml
id: implementing-angular
name: Angular Implementation Agent
state: Implementing
extends: implementing-default
description: Implements features using Angular best practices

match:
  framework: angular        # Match by detected framework
  # OR
  language: typescript      # Match by detected language
  # OR
  files:                    # Match by file presence
    - angular.json
    - package.json

prompt: |
  You are an Angular implementation agent...
  [Angular-specific instructions]

mcp_servers:
  - angular-cli
  - primeng
```

## Backlog Item Agents

Backlog items have their own pipeline with dedicated agents:

| State | Agent File | Description |
|-------|------------|-------------|
| Refining | `refining.yml` | Refines specifications, identifies ambiguities, suggests acceptance criteria |
| Splitting | `split.yml` | Decomposes backlog items into sequential, PR-sized tasks |

Backlog agents use `backlog_state` instead of `state` in their YAML configuration.

### Backlog Template Variables

| Variable | Description |
|----------|-------------|
| `{backlogItem.title}` | Backlog item title |
| `{backlogItem.description}` | Backlog item description |
| `{backlogItem.acceptanceCriteria}` | Backlog item acceptance criteria |
| `{backlogItem.refiningIterations}` | Number of refinement iterations |

## Template Variables

Available placeholders in prompts:

| Variable | Description |
|----------|-------------|
| `{task.title}` | Task title |
| `{task.description}` | Task description |
| `{task.state}` | Current pipeline state |
| `{task.priority}` | Task priority |
| `{task.acceptanceCriteria}` | Task acceptance criteria |
| `{subtask.title}` | Current subtask title |
| `{subtask.description}` | Current subtask description |
| `{subtask.acceptanceCriteria}` | Subtask acceptance criteria |
| `{context.language}` | Detected repository language |
| `{context.framework}` | Detected framework |
| `{context.repoPath}` | Repository path |
| `{artifacts}` | Formatted list of previous artifacts |
| `{artifacts.split}` | Most recent task split artifact content |
| `{artifacts.research}` | Most recent research findings content |
| `{artifacts.plan}` | Most recent plan artifact content |
| `{artifacts.implementation}` | Most recent implementation artifact content |
| `{artifacts.simplification}` | Most recent simplification review content |
| `{artifacts.verification}` | Most recent verification report content |
| `{artifacts.review}` | Most recent review artifact content |

## Artifact Types

| Type | Produced By | Contains |
|------|-------------|----------|
| `refined_spec` | Refining agent | Clarifying questions, suggested improvements, scope assessment |
| `task_split` | Split agent | Task decomposition, execution order, acceptance criteria |
| `research_findings` | Research agent | Existing code, patterns, affected files, external references |
| `plan` | Planning agent | Test specifications, implementation steps, verification commands |
| `implementation` | Implementing agent | Files changed, test results, verification log |
| `simplification_review` | Simplifying agent | Verdict, findings, scope assessment |
| `verification_report` | Verifying agent | Full test suite results, build status, regression check |
| `review` | Reviewing agent | Review findings, suggested changes, approval status |
| `documentation_update` | Documenting agent | Files modified, files reviewed, recommendations |
| `general` | Any agent | Unstructured output |

## Context Detection

The `ContextDetector` service automatically identifies:

- **Language**: Analyzes file extensions in repository (e.g., `.ts` → `typescript`, `.cs` → `csharp`)
- **Framework**: Checks for framework markers (`angular.json` → `angular`, `*.csproj` → `dotnet`)

Detection results are cached on the task entity and used for variant selection.

## Creating New Agents

1. **Default agent**: Create `agents/defaults/{state}.yml` with required fields
2. **Variant**: Create `agents/variants/{state}.{framework}.yml` with `extends` and `match` fields
3. **Register MCP servers**: Add server names to `mcp_servers` array if needed
4. **Restart API**: Configurations are loaded at startup

## Key Classes

| Class | Purpose |
|-------|---------|
| `IOrchestratorService` | Agent selection, prompt assembly, artifact management, confidence tracking |
| `IAgentConfigLoader` | Loads and caches YAML configurations |
| `IContextDetector` | Repository language/framework detection |
| `IPromptBuilder` | Template variable substitution with subtask and artifact context |
| `IArtifactParser` | Extracts structured content, confidence scores, human input requests, verdicts |
| `IWorktreeService` | Git worktree creation/removal for subtask isolation |
| `IRollbackService` | Subtask and task rollback with artifact preservation |
| `HumanGateService` | Human gate CRUD and resolution |
| `SubtaskService` | Subtask lifecycle management |
| `AgentConfig` | YAML configuration model |
| `ResolvedAgentConfig` | Fully resolved config with assembled prompt |
| `PipelineConfiguration` | Retry limits, confidence thresholds, gate configuration |
