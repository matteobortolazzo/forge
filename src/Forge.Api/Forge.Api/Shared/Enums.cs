using System.Text.Json.Serialization;

namespace Forge.Api.Shared;

/// <summary>
/// Backlog item states. Serialized as PascalCase.
/// Flow: New → Refining → Ready → Splitting → Executing → Done
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BacklogItemState
{
    New,        // Human creates item, waiting to be refined
    Refining,   // Optimizer agent asks questions, improves spec (can cycle)
    Ready,      // Spec approved, waiting for split
    Splitting,  // Split agent creating tasks
    Executing,  // Tasks in progress
    Done        // All tasks completed
}

/// <summary>
/// Pipeline states for task workflow. Serialized as PascalCase.
/// Flow: Planning → Implementing → PrReady
/// Note: Tasks are leaf units created from BacklogItem splitting.
/// Planning includes research phase. Implementing includes verification and simplification.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PipelineState
{
    Planning,     // Research + test-first implementation design
    Implementing, // Code generation, verification, YAGNI check, docs
    PrReady       // Ready for PR creation (final state)
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
    [JsonStringEnumMemberName("plan")]
    Plan,
    [JsonStringEnumMemberName("implementation")]
    Implementation,
    [JsonStringEnumMemberName("test")]
    Test,
    [JsonStringEnumMemberName("general")]
    General
}

/// <summary>
/// Types of human gates in the pipeline.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<HumanGateType>))]
public enum HumanGateType
{
    [JsonStringEnumMemberName("refining")]
    Refining,    // Conditional - triggered when confidence < threshold during BacklogItem refining
    [JsonStringEnumMemberName("split")]
    Split,       // Conditional - triggered when confidence < threshold during BacklogItem splitting
    [JsonStringEnumMemberName("planning")]
    Planning     // Conditional - triggered when confidence < threshold or high-risk
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

/// <summary>
/// Status of an agent question awaiting human response.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AgentQuestionStatus>))]
public enum AgentQuestionStatus
{
    [JsonStringEnumMemberName("pending")]
    Pending,
    [JsonStringEnumMemberName("answered")]
    Answered,
    [JsonStringEnumMemberName("timeout")]
    Timeout,
    [JsonStringEnumMemberName("cancelled")]
    Cancelled
}
