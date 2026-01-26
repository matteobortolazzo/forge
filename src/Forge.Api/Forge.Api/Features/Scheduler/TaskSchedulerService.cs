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
                await TryScheduleNextTaskAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in scheduler polling loop");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        logger.LogInformation("TaskSchedulerService stopped");
    }

    internal async Task TryScheduleNextTaskAsync()
    {
        // Check if agent is already running
        var agentStatus = agentRunner.GetStatus();
        if (agentStatus.IsRunning)
        {
            return;
        }

        // Get next schedulable task
        using var scope = scopeFactory.CreateScope();
        var schedulerService = scope.ServiceProvider.GetRequiredService<SchedulerService>();

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
        var started = await agentRunner.StartAgentAsync(nextTask.Id, nextTask.Title, nextTask.Description);
        if (!started)
        {
            logger.LogWarning("Failed to start agent for task {TaskId}", nextTask.Id);
        }
    }
}
