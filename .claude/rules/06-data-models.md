# Data Models

## Backlog Item States (BacklogItemState)
```
New → Refining → Ready → Splitting → Executing → Done
```

**State Descriptions:**
| State | Description | Agent |
|-------|-------------|-------|
| New | Initial state when created | None |
| Refining | Specification refinement and clarification | Refining agent |
| Ready | Spec approved, ready to decompose | None |
| Splitting | Task decomposition into PR-sized units | Split agent |
| Executing | Tasks in progress (auto-derived from tasks) | None |
| Done | All tasks completed (auto-transition) | None |

**Key Constraint:** Refining state can loop back to itself (for iterative refinement) or advance to Ready.

## Task States (PipelineState)
```
Planning → Implementing → PrReady
```

**State Descriptions:**
| State | Description | Agent |
|-------|-------------|-------|
| Planning | Research codebase + test-first implementation design | Planning agent |
| Implementing | Tests first, code, verify, YAGNI check, docs | Implementing agent |
| PrReady | Ready for PR creation (final state) | None |

## Priority Levels
```
Low | Medium | High | Critical
```

## Artifact Types
```
task_split | plan | implementation | test | general
```

## Human Gate Types
```
# Backlog item gates
refining (conditional) | split (conditional)

# Task gates
planning (conditional)
```

## Agent Question Status
```
pending | answered | timeout | cancelled
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
public ICollection<BacklogItemEntity> BacklogItems { get; set; } = [];
public ICollection<TaskEntity> Tasks { get; set; } = [];
```

## BacklogItemEntity Fields

```csharp
public Guid Id { get; set; }
public required string Title { get; set; }
public string? Description { get; set; }
public BacklogItemState State { get; set; }
public Priority Priority { get; set; }
public string? AcceptanceCriteria { get; set; }

// Repository association (required)
public Guid RepositoryId { get; set; }
public RepositoryEntity Repository { get; set; } = null!;

// Agent context
public string? DetectedLanguage { get; set; }    // e.g., "csharp", "typescript"
public string? DetectedFramework { get; set; }   // e.g., "angular", "dotnet"
public Guid? AssignedAgentId { get; set; }       // Currently running agent

// Scheduling
public bool IsPaused { get; set; }
public string? PauseReason { get; set; }
public DateTime? PausedAt { get; set; }
public int RetryCount { get; set; }
public int MaxRetries { get; set; } = 3;

// Confidence and human gates
public decimal? ConfidenceScore { get; set; }    // Agent-reported confidence (0.0-1.0)
public bool HumanInputRequested { get; set; }    // Agent explicitly requested human input
public string? HumanInputReason { get; set; }    // Reason for human input request
public bool HasPendingGate { get; set; }         // Blocked by pending human gate

// Refinement loop
public int RefiningIterations { get; set; }      // Number of refinement iterations

// Task progress (denormalized for efficiency)
public int TaskCount { get; set; }               // Number of tasks created from split
public int CompletedTaskCount { get; set; }      // Number of tasks at PrReady state

// Navigation
public ICollection<TaskEntity> Tasks { get; set; } = [];
public ICollection<AgentArtifactEntity> Artifacts { get; set; } = [];
public ICollection<HumanGateEntity> HumanGates { get; set; } = [];
public ICollection<AgentLogEntity> Logs { get; set; } = [];
```

## TaskEntity Fields

```csharp
public Guid Id { get; set; }
public required string Title { get; set; }
public string? Description { get; set; }
public PipelineState State { get; set; }
public Priority Priority { get; set; }
public string? AcceptanceCriteria { get; set; }

// Repository association (required)
public Guid RepositoryId { get; set; }
public RepositoryEntity Repository { get; set; } = null!;

// Backlog item association (required)
public Guid BacklogItemId { get; set; }          // Parent backlog item
public BacklogItemEntity BacklogItem { get; set; } = null!;
public int ExecutionOrder { get; set; }          // 1-based order within backlog item

// Agent context
public string? DetectedLanguage { get; set; }    // e.g., "csharp", "typescript"
public string? DetectedFramework { get; set; }   // e.g., "angular", "dotnet"
public PipelineState? RecommendedNextState { get; set; }  // Agent's recommendation
public Guid? AssignedAgentId { get; set; }       // Currently running agent

// Scheduling
public bool IsPaused { get; set; }
public string? PauseReason { get; set; }
public DateTime? PausedAt { get; set; }
public int RetryCount { get; set; }
public int MaxRetries { get; set; } = 3;

// Confidence and human gates
public decimal? ConfidenceScore { get; set; }    // Agent-reported confidence (0.0-1.0)
public bool HumanInputRequested { get; set; }    // Agent explicitly requested human input
public string? HumanInputReason { get; set; }    // Reason for human input request
public bool HasPendingGate { get; set; }         // Task blocked by pending human gate

// Pipeline iteration
public int ImplementationRetries { get; set; }   // Number of implementation retries

// Navigation
public ICollection<AgentLogEntity> Logs { get; set; } = [];
public ICollection<AgentArtifactEntity> Artifacts { get; set; } = [];
public ICollection<HumanGateEntity> HumanGates { get; set; } = [];
```

## Human Gate Configuration

Configuration in `appsettings.json`:
```json
{
  "Pipeline": {
    "MaxImplementationRetries": 3,
    "ConfidenceThreshold": 0.7,
    "HumanGates": {
      "IsRefiningMandatory": false,
      "IsSplitMandatory": false,
      "IsPlanningMandatory": false
    }
  }
}
```

**Backlog Item Gates:**
| Gate Type | Trigger | Behavior |
|-----------|---------|----------|
| Refining | Confidence < threshold OR mandatory config | Approval required before Ready |
| Split | Confidence < threshold OR mandatory config | Approval required before task creation |

**Task Gates:**
| Gate Type | Trigger | Behavior |
|-----------|---------|----------|
| Planning | Confidence < threshold OR mandatory config | Approval required before Implementing |

## AgentQuestionEntity Fields

```csharp
public Guid Id { get; set; }
public Guid? TaskId { get; set; }              // Task this question belongs to (null for backlog item)
public Guid? BacklogItemId { get; set; }       // Backlog item this question belongs to (null for task)
public required string ToolUseId { get; set; } // Tool use ID from Claude Code CLI
public required string QuestionsJson { get; set; } // Serialized List<AgentQuestionItem>
public AgentQuestionStatus Status { get; set; } = AgentQuestionStatus.Pending;
public DateTime RequestedAt { get; set; }       // When question was requested
public DateTime TimeoutAt { get; set; }         // When question will timeout
public string? AnswersJson { get; set; }        // Serialized List<QuestionAnswer>
public DateTime? AnsweredAt { get; set; }       // When question was answered
```

## Agent Question Configuration

Configuration in `appsettings.json`:
```json
{
  "AgentQuestions": {
    "TimeoutSeconds": 300
  }
}
```
