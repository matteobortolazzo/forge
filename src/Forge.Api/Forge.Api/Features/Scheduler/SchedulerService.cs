using Forge.Api.Data;
using Forge.Api.Data.Entities;
using Forge.Api.Features.Agent;
using Forge.Api.Features.Events;
using Forge.Api.Features.Notifications;
using Forge.Api.Features.Tasks;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Forge.Api.Features.Scheduler;

public class SchedulerService(
    ForgeDbContext db,
    ISseService sse,
    NotificationService notifications,
    SchedulerState schedulerState,
    IOptions<SchedulerOptions> options,
    IOptions<PipelineConfiguration> pipelineConfig,
    ILogger<SchedulerService> logger)
{
    private readonly PipelineConfiguration _pipelineConfig = pipelineConfig.Value;
    /// <summary>
    /// States that require agent work and are eligible for scheduling.
    /// </summary>
    private static readonly PipelineState[] SchedulableStates =
    [
        PipelineState.Split,
        PipelineState.Research,
        PipelineState.Planning,
        PipelineState.Implementing,
        PipelineState.Simplifying,
        PipelineState.Verifying,
        PipelineState.Reviewing
    ];

    /// <summary>
    /// Maps current state to next state after successful agent completion.
    /// </summary>
    private static readonly Dictionary<PipelineState, PipelineState> StateTransitions = new()
    {
        { PipelineState.Split, PipelineState.Research },
        { PipelineState.Research, PipelineState.Planning },
        { PipelineState.Planning, PipelineState.Implementing },
        { PipelineState.Implementing, PipelineState.Simplifying },
        { PipelineState.Simplifying, PipelineState.Verifying },
        { PipelineState.Verifying, PipelineState.Reviewing },
        { PipelineState.Reviewing, PipelineState.PrReady }
    };

    public bool IsEnabled => schedulerState.IsEnabled;

    public void Enable() => schedulerState.Enable();
    public void Disable() => schedulerState.Disable();

    /// <summary>
    /// Gets the next task eligible for scheduling based on priority and state.
    /// Only leaf tasks (tasks without children) can be scheduled.
    /// </summary>
    public async Task<TaskDto?> GetNextSchedulableTaskAsync()
    {
        if (!IsEnabled)
        {
            return null;
        }

        // Task Selection Algorithm:
        // 1. Filter: State IN schedulable states
        // 2. Filter: IsPaused = false
        // 3. Filter: HasError = false OR RetryCount < MaxRetries
        // 4. Filter: AssignedAgentId IS NULL
        // 5. Filter: ChildCount == 0 (leaf tasks only - parent state is derived)
        // 6. Filter: HasPendingGate = false (no pending human gates)
        // 7. Order: Priority DESC, State ASC (Split first), CreatedAt ASC

        var entity = await db.Tasks
            .Where(t => SchedulableStates.Contains(t.State))
            .Where(t => !t.IsPaused)
            .Where(t => !t.HasError || t.RetryCount < t.MaxRetries)
            .Where(t => t.AssignedAgentId == null)
            .Where(t => t.ChildCount == 0)  // Only schedule leaf tasks
            .Where(t => !t.HasPendingGate)   // No pending human gates
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.State)
            .ThenBy(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        return entity is null ? null : TaskDto.FromEntity(entity);
    }

    /// <summary>
    /// Handles agent completion and auto-transitions task to next state.
    /// Also updates parent derived state if task is a child.
    /// </summary>
    public async Task<TaskDto?> HandleAgentCompletionAsync(Guid taskId, AgentCompletionResult result)
    {
        var entity = await db.Tasks.FindAsync(taskId);
        if (entity is null)
        {
            logger.LogWarning("Task {TaskId} not found for completion handling", taskId);
            return null;
        }

        TaskDto? dto;
        switch (result)
        {
            case AgentCompletionResult.Success:
                dto = await HandleSuccessAsync(entity);
                break;

            case AgentCompletionResult.Error:
                dto = await HandleErrorAsync(entity);
                break;

            case AgentCompletionResult.Cancelled:
                // Auto-pause to prevent scheduler from restarting
                entity.IsPaused = true;
                entity.PauseReason = "Manually aborted by user";
                entity.PausedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();

                logger.LogInformation("Task {TaskId} agent was cancelled, task paused", taskId);

                dto = TaskDto.FromEntity(entity);
                await sse.EmitTaskPausedAsync(dto);
                break;

            default:
                dto = TaskDto.FromEntity(entity);
                break;
        }

        // Update parent derived state if this is a child task
        if (entity.ParentId is not null)
        {
            await UpdateParentDerivedStateAsync(entity.ParentId.Value);
        }

        return dto;
    }

    /// <summary>
    /// Updates parent's derived state when a child's state changes.
    /// </summary>
    private async Task UpdateParentDerivedStateAsync(Guid parentId)
    {
        var parent = await db.Tasks
            .Include(t => t.Children)
            .FirstOrDefaultAsync(t => t.Id == parentId);

        if (parent is null) return;

        var childStates = parent.Children.Select(c => c.State);
        var newDerivedState = Tasks.TaskService.ComputeDerivedState(childStates);

        if (parent.DerivedState != newDerivedState)
        {
            parent.DerivedState = newDerivedState;
            parent.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var progress = Tasks.TaskService.ComputeProgress(childStates);
            var childDtos = parent.Children.Select(c => TaskDto.FromEntity(c)).ToList();
            var parentDto = TaskDto.FromEntity(parent, childDtos, progress);
            await sse.EmitTaskUpdatedAsync(parentDto);

            logger.LogInformation("Parent task {ParentId} derived state updated to {DerivedState}",
                parentId, newDerivedState);
        }
    }

    private async Task<TaskDto> HandleSuccessAsync(TaskEntity entity)
    {
        // Clear any previous error state
        if (entity.HasError)
        {
            entity.HasError = false;
            entity.ErrorMessage = null;
            entity.RetryCount = 0;
        }

        // Check if human gate is needed based on confidence
        if (await ShouldTriggerHumanGateAsync(entity))
        {
            var gate = await CreateHumanGateAsync(entity);
            entity.HasPendingGate = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            logger.LogInformation("Task {TaskId} requires human approval at {State} gate", entity.Id, gate.GateType);

            var dto = TaskDto.FromEntity(entity);
            await sse.EmitTaskUpdatedAsync(dto);
            return dto;
        }

        // Auto-transition to next state
        if (StateTransitions.TryGetValue(entity.State, out var nextState))
        {
            var previousState = entity.State;
            entity.State = nextState;
            entity.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            logger.LogInformation("Task {TaskId} auto-transitioned from {FromState} to {ToState}",
                entity.Id, previousState, nextState);

            var dto = TaskDto.FromEntity(entity);
            await sse.EmitTaskUpdatedAsync(dto);
            await notifications.NotifyTaskStateChangedAsync(entity.Id, entity.Title, previousState, nextState);
            return dto;
        }

        // No transition defined (shouldn't happen for schedulable states)
        logger.LogWarning("No transition defined for state {State}", entity.State);
        return TaskDto.FromEntity(entity);
    }

    /// <summary>
    /// Checks if a human gate should be triggered based on confidence score and pipeline configuration.
    /// </summary>
    private Task<bool> ShouldTriggerHumanGateAsync(TaskEntity entity)
    {
        // PR gate is always mandatory
        if (entity.State == PipelineState.Reviewing)
        {
            return Task.FromResult(true);
        }

        // Check confidence threshold for conditional gates
        var confidenceThreshold = _pipelineConfig.ConfidenceThreshold;

        if (entity.State == PipelineState.Split)
        {
            if (_pipelineConfig.HumanGates.IsSplitMandatory)
                return Task.FromResult(true);

            return Task.FromResult(entity.ConfidenceScore.HasValue && entity.ConfidenceScore < confidenceThreshold);
        }

        if (entity.State == PipelineState.Planning)
        {
            if (_pipelineConfig.HumanGates.IsPlanningMandatory)
                return Task.FromResult(true);

            return Task.FromResult(entity.ConfidenceScore.HasValue && entity.ConfidenceScore < confidenceThreshold);
        }

        // Check if human input was explicitly requested
        if (entity.HumanInputRequested)
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <summary>
    /// Creates a human gate for a task.
    /// </summary>
    private async Task<HumanGateEntity> CreateHumanGateAsync(TaskEntity entity)
    {
        var gateType = entity.State switch
        {
            PipelineState.Split => HumanGateType.Split,
            PipelineState.Planning => HumanGateType.Planning,
            PipelineState.Reviewing => HumanGateType.Pr,
            _ => HumanGateType.Planning
        };

        var reason = entity.HumanInputRequested
            ? entity.HumanInputReason ?? "Agent requested human input"
            : entity.ConfidenceScore.HasValue
                ? $"Confidence score ({entity.ConfidenceScore:F2}) below threshold ({_pipelineConfig.ConfidenceThreshold:F2})"
                : "Mandatory approval required";

        var gate = new HumanGateEntity
        {
            Id = Guid.NewGuid(),
            TaskId = entity.Id,
            SubtaskId = null,
            GateType = gateType,
            Status = HumanGateStatus.Pending,
            ConfidenceScore = entity.ConfidenceScore ?? 0,
            Reason = reason,
            RequestedAt = DateTime.UtcNow
        };

        db.HumanGates.Add(gate);
        await db.SaveChangesAsync();

        // Emit SSE event
        var gateDto = new HumanGateDto(
            gate.Id,
            gate.TaskId,
            gate.SubtaskId,
            gate.GateType,
            gate.Status,
            gate.ConfidenceScore,
            gate.Reason,
            gate.RequestedAt,
            gate.ResolvedAt,
            gate.ResolvedBy,
            gate.Resolution
        );
        await sse.EmitHumanGateRequestedAsync(gateDto);

        return gate;
    }

    /// <summary>
    /// Handles simplification review verdict and potentially loops back to Implementation.
    /// </summary>
    public async Task<TaskDto?> HandleSimplificationVerdictAsync(Guid taskId, string verdict)
    {
        var entity = await db.Tasks.FindAsync(taskId);
        if (entity == null || entity.State != PipelineState.Simplifying)
        {
            return null;
        }

        if (verdict.Equals("approved", StringComparison.OrdinalIgnoreCase))
        {
            // Continue to Verifying
            return await HandleSuccessAsync(entity);
        }

        if (verdict.Equals("changes_requested", StringComparison.OrdinalIgnoreCase))
        {
            entity.SimplificationIterations++;

            if (entity.SimplificationIterations >= _pipelineConfig.MaxSimplificationIterations)
            {
                // Escalate to human after max iterations
                entity.HasPendingGate = true;
                entity.HumanInputRequested = true;
                entity.HumanInputReason = $"Simplification review still requesting changes after {entity.SimplificationIterations} iterations";
                entity.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();

                await CreateHumanGateAsync(entity);

                logger.LogWarning("Task {TaskId} escalated to human after {Iterations} simplification iterations",
                    taskId, entity.SimplificationIterations);

                var escalatedDto = TaskDto.FromEntity(entity);
                await sse.EmitTaskUpdatedAsync(escalatedDto);
                return escalatedDto;
            }

            // Loop back to Implementing
            var previousState = entity.State;
            entity.State = PipelineState.Implementing;
            entity.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            logger.LogInformation("Task {TaskId} looping back to Implementing (simplification iteration {Iteration})",
                taskId, entity.SimplificationIterations);

            var dto = TaskDto.FromEntity(entity);
            await sse.EmitTaskUpdatedAsync(dto);
            await notifications.NotifyTaskStateChangedAsync(entity.Id, entity.Title, previousState, entity.State);
            return dto;
        }

        // escalate_to_human verdict
        entity.HasPendingGate = true;
        entity.HumanInputRequested = true;
        entity.HumanInputReason = "Simplification review requested human escalation";
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await CreateHumanGateAsync(entity);

        var escalatedDto2 = TaskDto.FromEntity(entity);
        await sse.EmitTaskUpdatedAsync(escalatedDto2);
        return escalatedDto2;
    }

    private async Task<TaskDto> HandleErrorAsync(TaskEntity entity)
    {
        entity.RetryCount++;
        entity.UpdatedAt = DateTime.UtcNow;

        var maxRetries = entity.MaxRetries > 0 ? entity.MaxRetries : options.Value.DefaultMaxRetries;

        if (entity.RetryCount >= maxRetries)
        {
            // Auto-pause after max retries
            entity.IsPaused = true;
            entity.PauseReason = $"Auto-paused after {entity.RetryCount} failed attempts";
            entity.PausedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            logger.LogWarning("Task {TaskId} auto-paused after {RetryCount} retries", entity.Id, entity.RetryCount);

            var dto = TaskDto.FromEntity(entity);
            await sse.EmitTaskPausedAsync(dto);
            await notifications.NotifyTaskPausedAsync(entity.Id, entity.Title, entity.PauseReason);
            return dto;
        }

        await db.SaveChangesAsync();

        logger.LogInformation("Task {TaskId} error, retry {RetryCount}/{MaxRetries}",
            entity.Id, entity.RetryCount, maxRetries);

        return TaskDto.FromEntity(entity);
    }

    /// <summary>
    /// Pauses a task from scheduling.
    /// </summary>
    public async Task<TaskDto?> PauseTaskAsync(Guid taskId, string reason)
    {
        var entity = await db.Tasks.FindAsync(taskId);
        if (entity is null) return null;

        entity.IsPaused = true;
        entity.PauseReason = reason;
        entity.PausedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        logger.LogInformation("Task {TaskId} paused: {Reason}", taskId, reason);

        var dto = TaskDto.FromEntity(entity);
        await sse.EmitTaskPausedAsync(dto);
        return dto;
    }

    /// <summary>
    /// Resumes a paused task for scheduling.
    /// </summary>
    public async Task<TaskDto?> ResumeTaskAsync(Guid taskId)
    {
        var entity = await db.Tasks.FindAsync(taskId);
        if (entity is null) return null;

        entity.IsPaused = false;
        entity.PauseReason = null;
        entity.PausedAt = null;
        entity.HasError = false;
        entity.ErrorMessage = null;
        entity.RetryCount = 0;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        logger.LogInformation("Task {TaskId} resumed", taskId);

        var dto = TaskDto.FromEntity(entity);
        await sse.EmitTaskResumedAsync(dto);
        return dto;
    }

    /// <summary>
    /// Gets scheduler status metrics.
    /// </summary>
    public async Task<SchedulerStatusDto> GetStatusAsync(AgentStatusDto agentStatus)
    {
        // Only count leaf tasks as pending (parent tasks are not schedulable)
        var pendingCount = await db.Tasks
            .Where(t => SchedulableStates.Contains(t.State))
            .Where(t => !t.IsPaused)
            .Where(t => !t.HasError || t.RetryCount < t.MaxRetries)
            .Where(t => t.AssignedAgentId == null)
            .Where(t => t.ChildCount == 0)
            .CountAsync();

        var pausedCount = await db.Tasks
            .Where(t => t.IsPaused)
            .CountAsync();

        return new SchedulerStatusDto(
            IsEnabled,
            agentStatus.IsRunning,
            agentStatus.CurrentTaskId,
            pendingCount,
            pausedCount);
    }
}
