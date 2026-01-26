namespace Forge.Api.Features.Scheduler;

public record SchedulerStatusDto(
    bool IsEnabled,
    bool IsAgentRunning,
    Guid? CurrentTaskId,
    int PendingTaskCount,
    int PausedTaskCount);

public record PauseTaskDto(string Reason);

public enum AgentCompletionResult
{
    Success,
    Cancelled,
    Error
}
