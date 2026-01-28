using Forge.Api.Data;
using Forge.Api.Data.Entities;
using Forge.Api.Features.Agent;
using Forge.Api.Features.Backlog;
using Forge.Api.Features.Events;
using Forge.Api.Features.HumanGates;
using Forge.Api.Features.Notifications;
using Forge.Api.Features.Tasks;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static Forge.Api.Shared.PipelineConstants;

namespace Forge.Api.Features.Scheduler;

public class SchedulerService(
    ForgeDbContext db,
    ISseService sse,
    NotificationService notifications,
    SchedulerState schedulerState,
    IOptions<SchedulerOptions> options,
    IOptions<PipelineConfiguration> pipelineConfig,
    HumanGateService humanGateService,
    ILogger<SchedulerService> logger)
{
    private readonly PipelineConfiguration _pipelineConfig = pipelineConfig.Value;

    public bool IsEnabled => schedulerState.IsEnabled;

    public void Enable() => schedulerState.Enable();
    public void Disable() => schedulerState.Disable();

    #region Task Scheduling

    /// <summary>
    /// Gets the next task eligible for scheduling based on priority and state.
    /// All tasks are now leaf tasks (no hierarchy).
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
        // 5. Filter: HasPendingGate = false (no pending human gates)
        // 6. Order: Priority DESC, State ASC (Research first), CreatedAt ASC

        var entity = await db.Tasks
            .Where(t => SchedulableStates.Contains(t.State))
            .Where(t => !t.IsPaused)
            .Where(t => !t.HasError || t.RetryCount < t.MaxRetries)
            .Where(t => t.AssignedAgentId == null)
            .Where(t => !t.HasPendingGate)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.State)
            .ThenBy(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        return entity is null ? null : TaskDto.FromEntity(entity);
    }

    /// <summary>
    /// Handles agent completion and auto-transitions task to next state.
    /// Also updates parent backlog item task count.
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
                dto = await HandleTaskSuccessAsync(entity);
                break;

            case AgentCompletionResult.Error:
                dto = await HandleTaskErrorAsync(entity);
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

        // Update parent backlog item state if task completed
        if (entity.State == PipelineState.Done)
        {
            await UpdateBacklogItemFromTaskCompletionAsync(entity.BacklogItemId);
        }

        return dto;
    }

    private async Task<TaskDto> HandleTaskSuccessAsync(TaskEntity entity)
    {
        // Clear any previous error state
        if (entity.HasError)
        {
            entity.HasError = false;
            entity.ErrorMessage = null;
            entity.RetryCount = 0;
        }

        // Check if human gate is needed based on confidence
        if (await ShouldTriggerTaskHumanGateAsync(entity))
        {
            var gate = await humanGateService.CreateGateForTaskAsync(entity);
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
    private Task<bool> ShouldTriggerTaskHumanGateAsync(TaskEntity entity)
    {
        // PR gate is always mandatory
        if (entity.State == PipelineState.Reviewing)
        {
            return Task.FromResult(true);
        }

        // Check confidence threshold for conditional gates
        var confidenceThreshold = _pipelineConfig.ConfidenceThreshold;

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
            return await HandleTaskSuccessAsync(entity);
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

                await humanGateService.CreateGateForTaskAsync(entity);

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

        await humanGateService.CreateGateForTaskAsync(entity);

        var escalatedDto2 = TaskDto.FromEntity(entity);
        await sse.EmitTaskUpdatedAsync(escalatedDto2);
        return escalatedDto2;
    }

    private async Task<TaskDto> HandleTaskErrorAsync(TaskEntity entity)
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

    #endregion

    #region Backlog Item Scheduling

    /// <summary>
    /// Gets the next backlog item eligible for scheduling based on priority and state.
    /// </summary>
    public async Task<BacklogItemDto?> GetNextSchedulableBacklogItemAsync()
    {
        if (!IsEnabled)
        {
            return null;
        }

        // Backlog Item Selection Algorithm:
        // 1. Filter: State IN schedulable states (New, Refining, Ready)
        // 2. Filter: IsPaused = false
        // 3. Filter: AssignedAgentId IS NULL
        // 4. Filter: HasPendingGate = false
        // 5. Order: Priority DESC, State ASC (New first), CreatedAt ASC

        var entity = await db.BacklogItems
            .Where(b => BacklogItemConstants.SchedulableStates.Contains(b.State))
            .Where(b => !b.IsPaused)
            .Where(b => b.AssignedAgentId == null)
            .Where(b => !b.HasPendingGate)
            .OrderByDescending(b => b.Priority)
            .ThenBy(b => b.State)
            .ThenBy(b => b.CreatedAt)
            .FirstOrDefaultAsync();

        return entity is null ? null : BacklogItemDto.FromEntity(entity);
    }

    /// <summary>
    /// Handles agent completion for a backlog item and auto-transitions to next state.
    /// </summary>
    public async Task<BacklogItemDto?> HandleBacklogAgentCompletionAsync(Guid backlogItemId, AgentCompletionResult result)
    {
        var entity = await db.BacklogItems.FindAsync(backlogItemId);
        if (entity is null)
        {
            logger.LogWarning("BacklogItem {BacklogItemId} not found for completion handling", backlogItemId);
            return null;
        }

        BacklogItemDto? dto;
        switch (result)
        {
            case AgentCompletionResult.Success:
                dto = await HandleBacklogSuccessAsync(entity);
                break;

            case AgentCompletionResult.Error:
                dto = await HandleBacklogErrorAsync(entity);
                break;

            case AgentCompletionResult.Cancelled:
                entity.IsPaused = true;
                entity.PauseReason = "Manually aborted by user";
                entity.PausedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();

                logger.LogInformation("BacklogItem {BacklogItemId} agent was cancelled, item paused", backlogItemId);

                dto = BacklogItemDto.FromEntity(entity);
                await sse.EmitBacklogItemPausedAsync(dto);
                break;

            default:
                dto = BacklogItemDto.FromEntity(entity);
                break;
        }

        return dto;
    }

    private async Task<BacklogItemDto> HandleBacklogSuccessAsync(BacklogItemEntity entity)
    {
        // Check if human gate is needed based on confidence
        if (await ShouldTriggerBacklogHumanGateAsync(entity))
        {
            var gate = await humanGateService.CreateGateForBacklogItemAsync(entity);
            entity.HasPendingGate = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            logger.LogInformation("BacklogItem {BacklogItemId} requires human approval at {State} gate",
                entity.Id, gate.GateType);

            var dto = BacklogItemDto.FromEntity(entity);
            await sse.EmitBacklogItemUpdatedAsync(dto);
            return dto;
        }

        // Auto-transition to next state
        if (BacklogItemConstants.StateTransitions.TryGetValue(entity.State, out var nextState))
        {
            var previousState = entity.State;
            entity.State = nextState;
            entity.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            logger.LogInformation("BacklogItem {BacklogItemId} auto-transitioned from {FromState} to {ToState}",
                entity.Id, previousState, nextState);

            var dto = BacklogItemDto.FromEntity(entity);
            await sse.EmitBacklogItemUpdatedAsync(dto);
            return dto;
        }

        logger.LogWarning("No transition defined for backlog state {State}", entity.State);
        return BacklogItemDto.FromEntity(entity);
    }

    private Task<bool> ShouldTriggerBacklogHumanGateAsync(BacklogItemEntity entity)
    {
        var confidenceThreshold = _pipelineConfig.ConfidenceThreshold;

        // Refining gate is conditional based on confidence
        if (entity.State == BacklogItemState.Refining)
        {
            if (_pipelineConfig.HumanGates.IsRefiningMandatory)
                return Task.FromResult(true);

            return Task.FromResult(entity.ConfidenceScore.HasValue && entity.ConfidenceScore < confidenceThreshold);
        }

        // Splitting gate (conditional based on confidence)
        if (entity.State == BacklogItemState.Splitting)
        {
            if (_pipelineConfig.HumanGates.IsSplitMandatory)
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

    private async Task<BacklogItemDto> HandleBacklogErrorAsync(BacklogItemEntity entity)
    {
        entity.RetryCount++;
        entity.UpdatedAt = DateTime.UtcNow;

        var maxRetries = options.Value.DefaultMaxRetries;

        if (entity.RetryCount >= maxRetries)
        {
            entity.IsPaused = true;
            entity.PauseReason = $"Auto-paused after {entity.RetryCount} failed attempts";
            entity.PausedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            logger.LogWarning("BacklogItem {BacklogItemId} auto-paused after {RetryCount} retries",
                entity.Id, entity.RetryCount);

            var dto = BacklogItemDto.FromEntity(entity);
            await sse.EmitBacklogItemPausedAsync(dto);
            return dto;
        }

        await db.SaveChangesAsync();

        logger.LogInformation("BacklogItem {BacklogItemId} error, retry {RetryCount}/{MaxRetries}",
            entity.Id, entity.RetryCount, maxRetries);

        return BacklogItemDto.FromEntity(entity);
    }

    /// <summary>
    /// Pauses a backlog item from scheduling.
    /// </summary>
    public async Task<BacklogItemDto?> PauseBacklogItemAsync(Guid backlogItemId, string reason)
    {
        var entity = await db.BacklogItems.FindAsync(backlogItemId);
        if (entity is null) return null;

        entity.IsPaused = true;
        entity.PauseReason = reason;
        entity.PausedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        logger.LogInformation("BacklogItem {BacklogItemId} paused: {Reason}", backlogItemId, reason);

        var dto = BacklogItemDto.FromEntity(entity);
        await sse.EmitBacklogItemPausedAsync(dto);
        return dto;
    }

    /// <summary>
    /// Resumes a paused backlog item for scheduling.
    /// </summary>
    public async Task<BacklogItemDto?> ResumeBacklogItemAsync(Guid backlogItemId)
    {
        var entity = await db.BacklogItems.FindAsync(backlogItemId);
        if (entity is null) return null;

        entity.IsPaused = false;
        entity.PauseReason = null;
        entity.PausedAt = null;
        entity.RetryCount = 0;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        logger.LogInformation("BacklogItem {BacklogItemId} resumed", backlogItemId);

        var dto = BacklogItemDto.FromEntity(entity);
        await sse.EmitBacklogItemResumedAsync(dto);
        return dto;
    }

    /// <summary>
    /// Updates a backlog item state based on task completion.
    /// When all tasks are done, transitions backlog item to Done.
    /// </summary>
    private async Task UpdateBacklogItemFromTaskCompletionAsync(Guid backlogItemId)
    {
        var backlogItem = await db.BacklogItems
            .Include(b => b.Tasks)
            .FirstOrDefaultAsync(b => b.Id == backlogItemId);

        if (backlogItem is null) return;

        // Update completed count
        backlogItem.CompletedTaskCount = backlogItem.Tasks.Count(t => t.State == PipelineState.Done);
        backlogItem.UpdatedAt = DateTime.UtcNow;

        // Check if all tasks are done
        if (backlogItem.Tasks.Count > 0 && backlogItem.Tasks.All(t => t.State == PipelineState.Done))
        {
            backlogItem.State = BacklogItemState.Done;
            logger.LogInformation("BacklogItem {BacklogItemId} completed - all {TaskCount} tasks done",
                backlogItemId, backlogItem.Tasks.Count);
        }

        await db.SaveChangesAsync();
        await sse.EmitBacklogItemUpdatedAsync(BacklogItemDto.FromEntity(backlogItem));
    }

    #endregion

    #region Status

    /// <summary>
    /// Gets scheduler status metrics.
    /// </summary>
    public async Task<SchedulerStatusDto> GetStatusAsync(AgentStatusDto agentStatus)
    {
        var pendingTaskCount = await db.Tasks
            .Where(t => SchedulableStates.Contains(t.State))
            .Where(t => !t.IsPaused)
            .Where(t => !t.HasError || t.RetryCount < t.MaxRetries)
            .Where(t => t.AssignedAgentId == null)
            .CountAsync();

        var pausedTaskCount = await db.Tasks
            .Where(t => t.IsPaused)
            .CountAsync();

        var pendingBacklogCount = await db.BacklogItems
            .Where(b => BacklogItemConstants.SchedulableStates.Contains(b.State))
            .Where(b => !b.IsPaused)
            .Where(b => b.AssignedAgentId == null)
            .CountAsync();

        var pausedBacklogCount = await db.BacklogItems
            .Where(b => b.IsPaused)
            .CountAsync();

        return new SchedulerStatusDto(
            IsEnabled,
            agentStatus.IsRunning,
            agentStatus.CurrentTaskId,
            agentStatus.CurrentBacklogItemId,
            pendingTaskCount,
            pausedTaskCount,
            pendingBacklogCount,
            pausedBacklogCount);
    }

    #endregion
}
