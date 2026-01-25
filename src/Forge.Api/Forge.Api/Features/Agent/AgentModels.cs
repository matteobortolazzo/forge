namespace Forge.Api.Features.Agent;

public record AgentStatusDto(
    bool IsRunning,
    Guid? CurrentTaskId = null,
    DateTime? StartedAt = null);
