using System.Text.Json;
using System.Text.Json.Serialization;

namespace Forge.E2E.Console.Models;

/// <summary>
/// Wrapper for SSE events from the server.
/// </summary>
public sealed class SseEvent
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("payload")]
    public JsonElement Payload { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Constants for SSE event types.
/// </summary>
public static class SseEventTypes
{
    // Backlog item events
    public const string BacklogItemCreated = "backlogItem:created";
    public const string BacklogItemUpdated = "backlogItem:updated";
    public const string BacklogItemDeleted = "backlogItem:deleted";
    public const string BacklogItemPaused = "backlogItem:paused";
    public const string BacklogItemResumed = "backlogItem:resumed";
    public const string BacklogItemLog = "backlogItem:log";

    // Task events
    public const string TaskCreated = "task:created";
    public const string TaskUpdated = "task:updated";
    public const string TaskDeleted = "task:deleted";
    public const string TaskPaused = "task:paused";
    public const string TaskResumed = "task:resumed";
    public const string TaskLog = "task:log";

    // Common events
    public const string ArtifactCreated = "artifact:created";
    public const string HumanGateRequested = "humanGate:requested";
    public const string HumanGateResolved = "humanGate:resolved";
    public const string AgentStatusChanged = "agent:statusChanged";
    public const string SchedulerItemScheduled = "scheduler:itemScheduled";
    public const string SchedulerTaskScheduled = "scheduler:taskScheduled";
    public const string NotificationNew = "notification:new";
    public const string RepositoryCreated = "repository:created";
    public const string RepositoryUpdated = "repository:updated";
    public const string RepositoryDeleted = "repository:deleted";

    // Agent question events
    public const string AgentQuestionRequested = "agentQuestion:requested";
    public const string AgentQuestionAnswered = "agentQuestion:answered";
    public const string AgentQuestionTimeout = "agentQuestion:timeout";
    public const string AgentQuestionCancelled = "agentQuestion:cancelled";
}
