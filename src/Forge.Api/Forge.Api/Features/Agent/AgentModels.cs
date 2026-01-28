namespace Forge.Api.Features.Agent;

public record AgentStatusDto(
    bool IsRunning,
    Guid? CurrentTaskId = null,
    Guid? CurrentBacklogItemId = null,
    DateTime? StartedAt = null);

public interface IAgentRunnerService
{
    AgentStatusDto GetStatus();
    Task<bool> StartAgentAsync(Guid taskId, string title, string description);
    Task<bool> StartBacklogAgentAsync(Guid backlogItemId, string title, string description);
    Task<bool> AbortAsync();
}
