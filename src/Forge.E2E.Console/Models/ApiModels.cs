using System.Text.Json.Serialization;

namespace Forge.E2E.Console.Models;

#region Enums

/// <summary>
/// Backlog item states.
/// Flow: New → Refining → Ready → Splitting → Executing → Done
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BacklogItemState
{
    New,
    Refining,
    Ready,
    Splitting,
    Executing,
    Done
}

/// <summary>
/// Pipeline states for task workflow.
/// Flow: Planning → Implementing → PrReady
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PipelineState
{
    Planning,
    Implementing,
    PrReady
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
/// Artifact types produced by agents. Serialized as lowercase.
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
    Refining,
    [JsonStringEnumMemberName("split")]
    Split,
    [JsonStringEnumMemberName("planning")]
    Planning
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

#endregion

#region Repository DTOs

public sealed record RepositoryDto(
    Guid Id,
    string Name,
    string Path,
    bool IsActive,
    bool IsDefault,
    string? Branch,
    string? CommitHash,
    string? RemoteUrl,
    bool? IsDirty,
    bool IsGitRepository,
    DateTime? LastRefreshedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int BacklogItemCount);

public sealed record CreateRepositoryDto(
    string Name,
    string Path,
    bool IsDefault = false);

#endregion

#region Backlog Item DTOs

public sealed record BacklogItemProgressDto(
    int Completed,
    int Total,
    int Percent);

public sealed record BacklogItemDto(
    Guid Id,
    Guid RepositoryId,
    string Title,
    string Description,
    BacklogItemState State,
    Priority Priority,
    string? AcceptanceCriteria,
    string? AssignedAgentId,
    bool HasError,
    string? ErrorMessage,
    bool IsPaused,
    string? PauseReason,
    DateTime? PausedAt,
    int RetryCount,
    int MaxRetries,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int TaskCount,
    int CompletedTaskCount,
    decimal? ConfidenceScore,
    bool HasPendingGate,
    int RefiningIterations,
    BacklogItemProgressDto? Progress = null);

public sealed record CreateBacklogItemDto(
    string Title,
    string Description,
    Priority Priority = Priority.Medium,
    string? AcceptanceCriteria = null);

public sealed record TransitionBacklogItemDto(
    BacklogItemState TargetState);

#endregion

#region Task DTOs

public sealed record TaskDto(
    Guid Id,
    Guid RepositoryId,
    Guid BacklogItemId,
    string Title,
    string Description,
    PipelineState State,
    Priority Priority,
    string? AssignedAgentId,
    bool HasError,
    string? ErrorMessage,
    bool IsPaused,
    string? PauseReason,
    DateTime? PausedAt,
    int RetryCount,
    int MaxRetries,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int ExecutionOrder,
    decimal? ConfidenceScore,
    bool HasPendingGate);

public sealed record TaskLogDto(
    Guid Id,
    Guid TaskId,
    LogType Type,
    string Content,
    string? ToolName,
    DateTime Timestamp);

#endregion

#region Artifact DTOs

public sealed record ArtifactDto(
    Guid Id,
    Guid? TaskId,
    Guid? BacklogItemId,
    PipelineState? ProducedInState,
    BacklogItemState? ProducedInBacklogState,
    ArtifactType ArtifactType,
    string Content,
    DateTime CreatedAt,
    string? AgentId,
    decimal? ConfidenceScore,
    bool HumanInputRequested,
    string? HumanInputReason);

#endregion

#region Human Gate DTOs

public sealed record HumanGateDto(
    Guid Id,
    Guid? TaskId,
    Guid? BacklogItemId,
    HumanGateType GateType,
    HumanGateStatus Status,
    decimal ConfidenceScore,
    string Reason,
    DateTime RequestedAt,
    DateTime? ResolvedAt,
    string? ResolvedBy,
    string? Resolution);

public sealed record ResolveHumanGateDto(
    HumanGateStatus Status,
    string? Resolution = null,
    string? ResolvedBy = null);

#endregion

#region Agent Question DTOs

public sealed record QuestionOption(
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("description")] string Description);

public sealed record AgentQuestionItem(
    [property: JsonPropertyName("question")] string Question,
    [property: JsonPropertyName("header")] string Header,
    [property: JsonPropertyName("options")] IReadOnlyList<QuestionOption> Options,
    [property: JsonPropertyName("multiSelect")] bool MultiSelect);

public sealed record QuestionAnswer(
    [property: JsonPropertyName("questionIndex")] int QuestionIndex,
    [property: JsonPropertyName("selectedOptionIndices")] IReadOnlyList<int> SelectedOptionIndices,
    [property: JsonPropertyName("customAnswer")] string? CustomAnswer);

public sealed record AgentQuestionDto(
    Guid Id,
    Guid? TaskId,
    Guid? BacklogItemId,
    string ToolUseId,
    IReadOnlyList<AgentQuestionItem> Questions,
    AgentQuestionStatus Status,
    DateTime RequestedAt,
    DateTime TimeoutAt,
    IReadOnlyList<QuestionAnswer>? Answers,
    DateTime? AnsweredAt);

public sealed record SubmitAnswerDto(
    [property: JsonPropertyName("answers")] IReadOnlyList<QuestionAnswer> Answers);

#endregion

#region Scheduler DTOs

public sealed record SchedulerStatusDto(
    bool IsEnabled,
    bool IsAgentRunning,
    Guid? CurrentTaskId,
    Guid? CurrentBacklogItemId,
    int PendingTaskCount,
    int PausedTaskCount,
    int PendingBacklogItemCount,
    int PausedBacklogItemCount);

#endregion

#region Agent DTOs

public sealed record AgentStatusDto(
    bool IsRunning,
    Guid? CurrentTaskId,
    Guid? CurrentBacklogItemId,
    DateTime? StartedAt);

#endregion

#region Log DTOs

public sealed record BacklogItemLogDto(
    Guid Id,
    Guid BacklogItemId,
    LogType Type,
    string Content,
    string? ToolName,
    DateTime Timestamp);

#endregion
