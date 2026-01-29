using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Forge.Api.Features.Agent;
using Forge.Api.Features.AgentQuestions;
using Forge.Api.Features.Backlog;
using Forge.Api.Features.Notifications;
using Forge.Api.Features.Repositories;
using Forge.Api.Features.Tasks;
using Forge.Api.Shared;

namespace Forge.Api.Features.Events;

public interface ISseService
{
    // Backlog item events
    Task EmitBacklogItemCreatedAsync(BacklogItemDto item);
    Task EmitBacklogItemUpdatedAsync(BacklogItemDto item);
    Task EmitBacklogItemDeletedAsync(Guid itemId);
    Task EmitBacklogItemPausedAsync(BacklogItemDto item);
    Task EmitBacklogItemResumedAsync(BacklogItemDto item);
    Task EmitBacklogItemLogAsync(BacklogItemLogDto log);

    // Task events
    Task EmitTaskCreatedAsync(TaskDto task);
    Task EmitTaskUpdatedAsync(TaskDto task);
    Task EmitTaskDeletedAsync(Guid taskId);
    Task EmitTaskLogAsync(TaskLogDto log);
    Task EmitTaskPausedAsync(TaskDto task);
    Task EmitTaskResumedAsync(TaskDto task);

    // Agent events
    Task EmitAgentStatusChangedAsync(AgentStatusDto status);

    // Scheduler events
    Task EmitSchedulerTaskScheduledAsync(TaskDto task);
    Task EmitSchedulerBacklogItemScheduledAsync(BacklogItemDto item);

    // Notification events
    Task EmitNotificationNewAsync(NotificationDto notification);

    // Artifact events
    Task EmitArtifactCreatedAsync(ArtifactDto artifact);

    // Human gate events
    Task EmitHumanGateRequestedAsync(HumanGateDto gate);
    Task EmitHumanGateResolvedAsync(HumanGateDto gate);

    // Rollback events
    Task EmitRollbackInitiatedAsync(RollbackDto rollback);
    Task EmitRollbackCompletedAsync(RollbackDto rollback);

    // Repository events
    Task EmitRepositoryCreatedAsync(RepositoryDto repository);
    Task EmitRepositoryUpdatedAsync(RepositoryDto repository);
    Task EmitRepositoryDeletedAsync(Guid repositoryId);

    // Agent question events
    Task EmitAgentQuestionRequestedAsync(AgentQuestionDto question);
    Task EmitAgentQuestionAnsweredAsync(AgentQuestionDto question);
    Task EmitAgentQuestionTimeoutAsync(AgentQuestionDto question);
    Task EmitAgentQuestionCancelledAsync(Guid questionId);

    IAsyncEnumerable<string> GetEventsAsync(CancellationToken ct);
}

public sealed class SseService : ISseService
{
    private readonly Channel<ServerEvent> _channel = Channel.CreateUnbounded<ServerEvent>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });

    // Backlog item events
    public Task EmitBacklogItemCreatedAsync(BacklogItemDto item)
    {
        var evt = new ServerEvent("backlogItem:created", item, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitBacklogItemUpdatedAsync(BacklogItemDto item)
    {
        var evt = new ServerEvent("backlogItem:updated", item, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitBacklogItemDeletedAsync(Guid itemId)
    {
        var evt = new ServerEvent("backlogItem:deleted", new { id = itemId }, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitBacklogItemPausedAsync(BacklogItemDto item)
    {
        var evt = new ServerEvent("backlogItem:paused", item, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitBacklogItemResumedAsync(BacklogItemDto item)
    {
        var evt = new ServerEvent("backlogItem:resumed", item, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitBacklogItemLogAsync(BacklogItemLogDto log)
    {
        var evt = new ServerEvent("backlogItem:log", log, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    // Task events
    public Task EmitTaskCreatedAsync(TaskDto task)
    {
        var evt = new ServerEvent("task:created", task, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitTaskUpdatedAsync(TaskDto task)
    {
        var evt = new ServerEvent("task:updated", task, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitTaskDeletedAsync(Guid taskId)
    {
        var evt = new ServerEvent("task:deleted", new { id = taskId }, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitTaskLogAsync(TaskLogDto log)
    {
        var evt = new ServerEvent("task:log", log, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitTaskPausedAsync(TaskDto task)
    {
        var evt = new ServerEvent("task:paused", task, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitTaskResumedAsync(TaskDto task)
    {
        var evt = new ServerEvent("task:resumed", task, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    // Agent events
    public Task EmitAgentStatusChangedAsync(AgentStatusDto status)
    {
        var evt = new ServerEvent("agent:statusChanged", status, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    // Scheduler events
    public Task EmitSchedulerTaskScheduledAsync(TaskDto task)
    {
        var evt = new ServerEvent("scheduler:taskScheduled", task, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitSchedulerBacklogItemScheduledAsync(BacklogItemDto item)
    {
        var evt = new ServerEvent("scheduler:backlogItemScheduled", item, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitNotificationNewAsync(NotificationDto notification)
    {
        var evt = new ServerEvent("notification:new", notification, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitArtifactCreatedAsync(ArtifactDto artifact)
    {
        var evt = new ServerEvent("artifact:created", artifact, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitHumanGateRequestedAsync(HumanGateDto gate)
    {
        var evt = new ServerEvent("humanGate:requested", gate, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitHumanGateResolvedAsync(HumanGateDto gate)
    {
        var evt = new ServerEvent("humanGate:resolved", gate, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitRollbackInitiatedAsync(RollbackDto rollback)
    {
        var evt = new ServerEvent("rollback:initiated", rollback, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitRollbackCompletedAsync(RollbackDto rollback)
    {
        var evt = new ServerEvent("rollback:completed", rollback, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitRepositoryCreatedAsync(RepositoryDto repository)
    {
        var evt = new ServerEvent("repository:created", repository, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitRepositoryUpdatedAsync(RepositoryDto repository)
    {
        var evt = new ServerEvent("repository:updated", repository, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitRepositoryDeletedAsync(Guid repositoryId)
    {
        var evt = new ServerEvent("repository:deleted", new { id = repositoryId }, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    // Agent question events
    public Task EmitAgentQuestionRequestedAsync(AgentQuestionDto question)
    {
        var evt = new ServerEvent("agentQuestion:requested", question, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitAgentQuestionAnsweredAsync(AgentQuestionDto question)
    {
        var evt = new ServerEvent("agentQuestion:answered", question, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitAgentQuestionTimeoutAsync(AgentQuestionDto question)
    {
        var evt = new ServerEvent("agentQuestion:timeout", question, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitAgentQuestionCancelledAsync(Guid questionId)
    {
        var evt = new ServerEvent("agentQuestion:cancelled", new { id = questionId }, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public async IAsyncEnumerable<string> GetEventsAsync([EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var evt in _channel.Reader.ReadAllAsync(ct))
        {
            var json = JsonSerializer.Serialize(evt, SharedJsonOptions.CamelCase);
            yield return $"data: {json}\n\n";
        }
    }
}

internal record ServerEvent(string Type, object Payload, DateTime Timestamp);
