using Forge.Api.Features.Agent;
using Forge.Api.Features.Events;
using Microsoft.Extensions.Options;

namespace Forge.Api.Features.Scheduler;

public class TaskSchedulerService(
    IServiceScopeFactory scopeFactory,
    IAgentRunnerService agentRunner,
    ISseService sse,
    IOptions<SchedulerOptions> options,
    ILogger<TaskSchedulerService> logger) : BackgroundService
{
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(options.Value.PollingIntervalSeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("TaskSchedulerService started with polling interval {Interval}s", _pollingInterval.TotalSeconds);

        // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TryScheduleNextWorkItemAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in scheduler polling loop");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        logger.LogInformation("TaskSchedulerService stopped");
    }

    /// <summary>
    /// Tries to schedule the next work item (backlog item or task).
    /// Backlog items in schedulable states (New, Refining, Ready) are prioritized over tasks.
    /// </summary>
    internal async Task TryScheduleNextWorkItemAsync()
    {
        // Check if agent is already running
        var agentStatus = agentRunner.GetStatus();
        if (agentStatus.IsRunning)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var schedulerService = scope.ServiceProvider.GetRequiredService<SchedulerService>();

        // First, try to schedule a backlog item (prioritize backlog work)
        var nextBacklogItem = await schedulerService.GetNextSchedulableBacklogItemAsync();
        if (nextBacklogItem is not null)
        {
            logger.LogInformation("Scheduling backlog item {BacklogItemId}: {Title} in state {State}",
                nextBacklogItem.Id, nextBacklogItem.Title, nextBacklogItem.State);

            // Emit SSE event for backlog item scheduling
            await sse.EmitSchedulerBacklogItemScheduledAsync(nextBacklogItem);

            // Start the agent for backlog item
            var started = await agentRunner.StartBacklogAgentAsync(
                nextBacklogItem.Id,
                nextBacklogItem.Title,
                nextBacklogItem.Description);

            if (!started)
            {
                logger.LogWarning("Failed to start agent for backlog item {BacklogItemId}", nextBacklogItem.Id);
            }

            return;
        }

        // If no backlog items, try to schedule a task
        var nextTask = await schedulerService.GetNextSchedulableTaskAsync();
        if (nextTask is null)
        {
            return;
        }

        logger.LogInformation("Scheduling task {TaskId}: {Title} in state {State}",
            nextTask.Id, nextTask.Title, nextTask.State);

        // Emit SSE event for task scheduling
        await sse.EmitSchedulerTaskScheduledAsync(nextTask);

        // Start the agent
        var taskStarted = await agentRunner.StartAgentAsync(nextTask.Id, nextTask.Title, nextTask.Description);
        if (!taskStarted)
        {
            logger.LogWarning("Failed to start agent for task {TaskId}", nextTask.Id);
        }
    }
}
