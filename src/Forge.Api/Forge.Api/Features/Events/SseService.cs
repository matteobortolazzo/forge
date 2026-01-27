using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Forge.Api.Features.Agent;
using Forge.Api.Features.Notifications;
using Forge.Api.Features.Tasks;

namespace Forge.Api.Features.Events;

public interface ISseService
{
    // Task events
    Task EmitTaskCreatedAsync(TaskDto task);
    Task EmitTaskUpdatedAsync(TaskDto task);
    Task EmitTaskDeletedAsync(Guid taskId);
    Task EmitTaskLogAsync(TaskLogDto log);
    Task EmitTaskPausedAsync(TaskDto task);
    Task EmitTaskResumedAsync(TaskDto task);
    Task EmitTaskSplitAsync(TaskDto parent, IReadOnlyList<TaskDto> children);
    Task EmitChildAddedAsync(Guid parentId, TaskDto child);

    // Agent events
    Task EmitAgentStatusChangedAsync(bool isRunning, Guid? taskId, DateTime? startedAt);

    // Scheduler events
    Task EmitSchedulerTaskScheduledAsync(TaskDto task);

    // Notification events
    Task EmitNotificationNewAsync(NotificationDto notification);

    // Artifact events
    Task EmitArtifactCreatedAsync(ArtifactDto artifact);

    // Human gate events
    Task EmitHumanGateRequestedAsync(HumanGateDto gate);
    Task EmitHumanGateResolvedAsync(HumanGateDto gate);

    // Subtask events
    Task EmitSubtaskCreatedAsync(SubtaskDto subtask);
    Task EmitSubtaskStartedAsync(SubtaskDto subtask);
    Task EmitSubtaskCompletedAsync(SubtaskDto subtask);
    Task EmitSubtaskFailedAsync(SubtaskDto subtask);

    // Rollback events
    Task EmitRollbackInitiatedAsync(RollbackDto rollback);
    Task EmitRollbackCompletedAsync(RollbackDto rollback);

    IAsyncEnumerable<string> GetEventsAsync(CancellationToken ct);
}

public sealed class SseService : ISseService
{
    private readonly Channel<ServerEvent> _channel = Channel.CreateUnbounded<ServerEvent>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

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

    public Task EmitTaskSplitAsync(TaskDto parent, IReadOnlyList<TaskDto> children)
    {
        var payload = new { parent, children };
        var evt = new ServerEvent("task:split", payload, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitChildAddedAsync(Guid parentId, TaskDto child)
    {
        var payload = new { parentId, child };
        var evt = new ServerEvent("task:childAdded", payload, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitAgentStatusChangedAsync(bool isRunning, Guid? taskId, DateTime? startedAt)
    {
        var status = new AgentStatusDto(isRunning, taskId, startedAt);
        var evt = new ServerEvent("agent:statusChanged", status, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitSchedulerTaskScheduledAsync(TaskDto task)
    {
        var evt = new ServerEvent("scheduler:taskScheduled", task, DateTime.UtcNow);
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

    public Task EmitSubtaskCreatedAsync(SubtaskDto subtask)
    {
        var evt = new ServerEvent("subtask:created", subtask, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitSubtaskStartedAsync(SubtaskDto subtask)
    {
        var evt = new ServerEvent("subtask:started", subtask, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitSubtaskCompletedAsync(SubtaskDto subtask)
    {
        var evt = new ServerEvent("subtask:completed", subtask, DateTime.UtcNow);
        return _channel.Writer.WriteAsync(evt).AsTask();
    }

    public Task EmitSubtaskFailedAsync(SubtaskDto subtask)
    {
        var evt = new ServerEvent("subtask:failed", subtask, DateTime.UtcNow);
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

    public async IAsyncEnumerable<string> GetEventsAsync([EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var evt in _channel.Reader.ReadAllAsync(ct))
        {
            var json = JsonSerializer.Serialize(evt, JsonOptions);
            yield return $"data: {json}\n\n";
        }
    }
}

internal record ServerEvent(string Type, object Payload, DateTime Timestamp);
