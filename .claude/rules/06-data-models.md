# Data Models

## Task States (PipelineState)
```
Backlog → Split → Research → Planning → Implementing → Simplifying → Verifying → Reviewing → PrReady → Done
```

**State Descriptions:**
| State | Description | Agent |
|-------|-------------|-------|
| Backlog | Waiting to be started | None |
| Split | Task decomposition into subtasks | Split agent |
| Research | Codebase analysis and pattern discovery | Research agent |
| Planning | Test-first implementation design | Planning agent |
| Implementing | Code generation (tests first, then code) | Implementing agent |
| Simplifying | Over-engineering review (YAGNI check) | Simplifying agent |
| Verifying | Comprehensive verification and regression testing | Verifying agent |
| Reviewing | Human code review | Review agent |
| PrReady | Ready for PR creation | None |
| Done | Completed | None |

## Priority Levels
```
Low | Medium | High | Critical
```

## Artifact Types
```
task_split | research_findings | plan | implementation | simplification_review | verification_report | review | test | general
```

## Human Gate Types
```
split (conditional) | planning (conditional) | pr (mandatory)
```

## Subtask Status
```
pending | in_progress | completed | failed | skipped
```

## RepositoryEntity Fields

```csharp
public Guid Id { get; set; }
public required string Name { get; set; }          // Display name (max 200)
public required string Path { get; set; }          // Absolute path (max 1000, unique)
public bool IsDefault { get; set; }                // Default for new tasks
public bool IsActive { get; set; } = true;         // Soft delete
public DateTime CreatedAt { get; set; }
public DateTime UpdatedAt { get; set; }

// Cached git info (refreshed on demand)
public string? Branch { get; set; }
public string? CommitHash { get; set; }
public string? RemoteUrl { get; set; }
public bool? IsDirty { get; set; }
public bool IsGitRepository { get; set; }
public DateTime? LastRefreshedAt { get; set; }

// Navigation
public ICollection<TaskEntity> Tasks { get; set; } = [];
```

## TaskEntity Fields (Agent Context)

```csharp
// Repository association (required)
public Guid RepositoryId { get; set; }
public RepositoryEntity Repository { get; set; } = null!;

// Auto-detected or user-specified context
public string? DetectedLanguage { get; set; }    // e.g., "csharp", "typescript"
public string? DetectedFramework { get; set; }   // e.g., "angular", "dotnet"
public PipelineState? RecommendedNextState { get; set; }  // Agent's recommendation

// Confidence and human gates
public decimal? ConfidenceScore { get; set; }    // Agent-reported confidence (0.0-1.0)
public bool HumanInputRequested { get; set; }    // Agent explicitly requested human input
public string? HumanInputReason { get; set; }    // Reason for human input request
public bool HasPendingGate { get; set; }         // Task blocked by pending human gate

// Simplification loop
public int SimplificationIterations { get; set; }  // Number of simplification loops

// Task hierarchy
public Guid? ParentId { get; set; }              // Parent task (for subtasks)
public int ChildCount { get; set; }              // Number of child subtasks
public PipelineState? DerivedState { get; set; } // Computed from children's states
```

## Human Gate Configuration

Configuration in `appsettings.json`:
```json
{
  "Pipeline": {
    "MaxImplementationRetries": 3,
    "MaxSimplificationIterations": 2,
    "ConfidenceThreshold": 0.7,
    "WorktreeIsolation": true,
    "SequentialSubtasks": true,
    "HumanGates": {
      "Split": "conditional",
      "Planning": "conditional",
      "Pr": "mandatory"
    }
  }
}
```

| Gate Type | Trigger | Behavior |
|-----------|---------|----------|
| Split | Confidence < threshold OR mandatory config | Approval required before Research |
| Planning | Confidence < threshold OR high-risk OR mandatory config | Approval required before Implementing |
| PR | Always mandatory | Approval required before merge |
