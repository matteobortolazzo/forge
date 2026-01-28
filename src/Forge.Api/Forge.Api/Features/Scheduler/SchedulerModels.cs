namespace Forge.Api.Features.Scheduler;

public record SchedulerStatusDto(
    bool IsEnabled,
    bool IsAgentRunning,
    Guid? CurrentTaskId,
    Guid? CurrentBacklogItemId,
    int PendingTaskCount,
    int PausedTaskCount,
    int PendingBacklogItemCount,
    int PausedBacklogItemCount);

public record PauseTaskDto(string Reason);

public record PauseBacklogItemDto(string Reason);

public enum AgentCompletionResult
{
    Success,
    Cancelled,
    Error
}
