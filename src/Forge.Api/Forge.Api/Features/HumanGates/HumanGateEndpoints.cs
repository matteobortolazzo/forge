using Forge.Api.Features.Repositories;
using Forge.Api.Features.Tasks;

namespace Forge.Api.Features.HumanGates;

/// <summary>
/// API endpoints for human gate management.
/// </summary>
public static class HumanGateEndpoints
{
    public static void MapHumanGateEndpoints(this IEndpointRouteBuilder app)
    {
        // Global gate endpoints (cross-repository)
        var group = app.MapGroup("/api/gates")
            .WithTags("Human Gates");

        group.MapGet("/pending", GetPendingGates)
            .WithName("GetPendingGates")
            .WithSummary("Get all pending human gates (cross-repository)");

        group.MapGet("/{gateId:guid}", GetGate)
            .WithName("GetGate")
            .WithSummary("Get a specific human gate");

        group.MapPost("/{gateId:guid}/resolve", ResolveGate)
            .WithName("ResolveGate")
            .WithSummary("Resolve a pending human gate");

        // Task-specific gate endpoints (repository-scoped)
        var taskGroup = app.MapGroup("/api/repositories/{repoId:guid}/tasks/{taskId:guid}/gates")
            .WithTags("Human Gates");

        taskGroup.MapGet("/", GetGatesForTask)
            .WithName("GetGatesForTask")
            .WithSummary("Get all human gates for a task");
    }

    private static async Task<IResult> GetPendingGates(HumanGateService service)
    {
        var gates = await service.GetPendingGatesAsync();
        return Results.Ok(gates);
    }

    private static async Task<IResult> GetGate(Guid gateId, HumanGateService service)
    {
        var gate = await service.GetGateAsync(gateId);
        return gate == null
            ? Results.NotFound(new { message = $"Gate {gateId} not found" })
            : Results.Ok(gate);
    }

    private static async Task<IResult> ResolveGate(Guid gateId, ResolveHumanGateDto dto, HumanGateService service)
    {
        try
        {
            var gate = await service.ResolveGateAsync(gateId, dto);
            return gate == null
                ? Results.NotFound(new { message = $"Gate {gateId} not found" })
                : Results.Ok(gate);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> GetGatesForTask(
        Guid repoId,
        Guid taskId,
        HumanGateService service,
        IRepositoryService repositoryService,
        TaskService taskService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var task = await taskService.GetByIdAsync(taskId);
        if (task is null || task.RepositoryId != repoId)
        {
            return Results.NotFound(new { error = "Task not found" });
        }

        var gates = await service.GetGatesForTaskAsync(taskId);
        return Results.Ok(gates);
    }
}
