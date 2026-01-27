using System.Text.Json.Serialization;

namespace Forge.Api.Shared;

/// <summary>
/// Pipeline states for task workflow. Serialized as PascalCase.
/// Flow: Backlog → Split → Research → Planning → Implementing → Simplifying → Verifying → Reviewing → PrReady → Done
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PipelineState
{
    Backlog,
    Split,        // Task decomposition into subtasks
    Research,     // Codebase analysis and pattern discovery
    Planning,     // Test-first implementation design
    Implementing, // Code generation (tests first, then implementation)
    Simplifying,  // Over-engineering review
    Verifying,    // Comprehensive verification (replaces Testing)
    Reviewing,    // Human code review
    PrReady,      // Ready for PR creation
    Done
}

/// <summary>
/// Priority levels for tasks. Serialized as lowercase.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Priority>))]
public enum Priority
{
    [JsonStringEnumMemberName("low")]
    Low,
    [JsonStringEnumMemberName("medium")]
    Medium,
    [JsonStringEnumMemberName("high")]
    High,
    [JsonStringEnumMemberName("critical")]
    Critical
}

/// <summary>
/// Log types for agent output. Serialized as camelCase.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<LogType>))]
public enum LogType
{
    [JsonStringEnumMemberName("info")]
    Info,
    [JsonStringEnumMemberName("toolUse")]
    ToolUse,
    [JsonStringEnumMemberName("toolResult")]
    ToolResult,
    [JsonStringEnumMemberName("error")]
    Error,
    [JsonStringEnumMemberName("thinking")]
    Thinking
}

/// <summary>
/// Notification types for user alerts. Serialized as lowercase.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<NotificationType>))]
public enum NotificationType
{
    [JsonStringEnumMemberName("info")]
    Info,
    [JsonStringEnumMemberName("success")]
    Success,
    [JsonStringEnumMemberName("warning")]
    Warning,
    [JsonStringEnumMemberName("error")]
    Error
}

/// <summary>
/// Artifact types produced by agents at each pipeline stage. Serialized as lowercase.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ArtifactType>))]
public enum ArtifactType
{
    [JsonStringEnumMemberName("task_split")]
    TaskSplit,
    [JsonStringEnumMemberName("research_findings")]
    ResearchFindings,
    [JsonStringEnumMemberName("plan")]
    Plan,
    [JsonStringEnumMemberName("implementation")]
    Implementation,
    [JsonStringEnumMemberName("simplification_review")]
    SimplificationReview,
    [JsonStringEnumMemberName("verification_report")]
    VerificationReport,
    [JsonStringEnumMemberName("review")]
    Review,
    [JsonStringEnumMemberName("test")]
    Test,
    [JsonStringEnumMemberName("general")]
    General
}

/// <summary>
/// Status of a subtask within a split task.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SubtaskStatus>))]
public enum SubtaskStatus
{
    [JsonStringEnumMemberName("pending")]
    Pending,
    [JsonStringEnumMemberName("in_progress")]
    InProgress,
    [JsonStringEnumMemberName("completed")]
    Completed,
    [JsonStringEnumMemberName("failed")]
    Failed,
    [JsonStringEnumMemberName("skipped")]
    Skipped
}

/// <summary>
/// Types of human gates in the pipeline.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<HumanGateType>))]
public enum HumanGateType
{
    [JsonStringEnumMemberName("split")]
    Split,       // Conditional - triggered when confidence < threshold
    [JsonStringEnumMemberName("planning")]
    Planning,    // Conditional - triggered when confidence < threshold or high-risk
    [JsonStringEnumMemberName("pr")]
    Pr           // Mandatory - always requires human approval
}

/// <summary>
/// Status of a human gate.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<HumanGateStatus>))]
public enum HumanGateStatus
{
    [JsonStringEnumMemberName("pending")]
    Pending,
    [JsonStringEnumMemberName("approved")]
    Approved,
    [JsonStringEnumMemberName("rejected")]
    Rejected,
    [JsonStringEnumMemberName("skipped")]
    Skipped
}

/// <summary>
/// Estimated scope of a subtask.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<EstimatedScope>))]
public enum EstimatedScope
{
    [JsonStringEnumMemberName("small")]
    Small,    // Minor change, ~50-100 lines
    [JsonStringEnumMemberName("medium")]
    Medium,   // Moderate change, ~100-300 lines
    [JsonStringEnumMemberName("large")]
    Large     // Significant change, ~300-500 lines
}

/// <summary>
/// Trigger reasons for rollback.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RollbackTrigger>))]
public enum RollbackTrigger
{
    [JsonStringEnumMemberName("max_retries_exceeded")]
    MaxRetriesExceeded,
    [JsonStringEnumMemberName("human_rejected")]
    HumanRejected,
    [JsonStringEnumMemberName("regression_detected")]
    RegressionDetected,
    [JsonStringEnumMemberName("manual_abort")]
    ManualAbort
}
