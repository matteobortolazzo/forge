using Forge.Api.Features.Agent;

namespace Forge.Api.Features.Scheduler;

public static class SchedulerEndpoints
{
    public static void MapSchedulerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/scheduler")
            .WithTags("Scheduler");

        group.MapGet("/status", GetStatus)
            .WithName("GetSchedulerStatus");

        group.MapPost("/enable", EnableScheduler)
            .WithName("EnableScheduler");

        group.MapPost("/disable", DisableScheduler)
            .WithName("DisableScheduler");
    }

    private static async Task<IResult> GetStatus(SchedulerService schedulerService, IAgentRunnerService agentRunner)
    {
        var agentStatus = agentRunner.GetStatus();
        var status = await schedulerService.GetStatusAsync(agentStatus);
        return Results.Ok(status);
    }

    private static IResult EnableScheduler(SchedulerService schedulerService)
    {
        schedulerService.Enable();
        return Results.Ok(new { enabled = true });
    }

    private static IResult DisableScheduler(SchedulerService schedulerService)
    {
        schedulerService.Disable();
        return Results.Ok(new { enabled = false });
    }
}
