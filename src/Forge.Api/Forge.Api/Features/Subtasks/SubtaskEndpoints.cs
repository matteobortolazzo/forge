using Forge.Api.Features.Repositories;
using Forge.Api.Features.Tasks;

namespace Forge.Api.Features.Subtasks;

/// <summary>
/// API endpoints for subtask management.
/// </summary>
public static class SubtaskEndpoints
{
    public static void MapSubtaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/repositories/{repoId:guid}/tasks/{taskId:guid}/subtasks")
            .WithTags("Subtasks");

        group.MapGet("/", GetSubtasks)
            .WithName("GetSubtasks")
            .WithSummary("Get all subtasks for a task");

        group.MapGet("/{subtaskId:guid}", GetSubtask)
            .WithName("GetSubtask")
            .WithSummary("Get a specific subtask");

        group.MapPost("/", CreateSubtask)
            .WithName("CreateSubtask")
            .WithSummary("Create a new subtask");

        group.MapPatch("/{subtaskId:guid}", UpdateSubtask)
            .WithName("UpdateSubtask")
            .WithSummary("Update a subtask");

        group.MapDelete("/{subtaskId:guid}", DeleteSubtask)
            .WithName("DeleteSubtask")
            .WithSummary("Delete a subtask");

        group.MapPost("/{subtaskId:guid}/start", StartSubtask)
            .WithName("StartSubtask")
            .WithSummary("Start subtask execution");

        group.MapPost("/{subtaskId:guid}/retry", RetrySubtask)
            .WithName("RetrySubtask")
            .WithSummary("Retry a failed subtask");
    }

    private static async Task<IResult> GetSubtasks(
        Guid repoId,
        Guid taskId,
        SubtaskService service,
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

        var subtasks = await service.GetSubtasksAsync(taskId);
        return Results.Ok(subtasks);
    }

    private static async Task<IResult> GetSubtask(
        Guid repoId,
        Guid taskId,
        Guid subtaskId,
        SubtaskService service,
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

        var subtask = await service.GetSubtaskAsync(taskId, subtaskId);
        return subtask == null
            ? Results.NotFound(new { message = $"Subtask {subtaskId} not found" })
            : Results.Ok(subtask);
    }

    private static async Task<IResult> CreateSubtask(
        Guid repoId,
        Guid taskId,
        CreateSubtaskDto dto,
        SubtaskService service,
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

        try
        {
            var subtask = await service.CreateSubtaskAsync(taskId, dto);
            return Results.Created($"/api/repositories/{repoId}/tasks/{taskId}/subtasks/{subtask.Id}", subtask);
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UpdateSubtask(
        Guid repoId,
        Guid taskId,
        Guid subtaskId,
        UpdateSubtaskDto dto,
        SubtaskService service,
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

        var subtask = await service.UpdateSubtaskAsync(taskId, subtaskId, dto);
        return subtask == null
            ? Results.NotFound(new { message = $"Subtask {subtaskId} not found" })
            : Results.Ok(subtask);
    }

    private static async Task<IResult> DeleteSubtask(
        Guid repoId,
        Guid taskId,
        Guid subtaskId,
        SubtaskService service,
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

        var deleted = await service.DeleteSubtaskAsync(taskId, subtaskId);
        return deleted
            ? Results.NoContent()
            : Results.NotFound(new { message = $"Subtask {subtaskId} not found" });
    }

    private static async Task<IResult> StartSubtask(
        Guid repoId,
        Guid taskId,
        Guid subtaskId,
        SubtaskService service,
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

        try
        {
            var subtask = await service.StartSubtaskAsync(taskId, subtaskId);
            return subtask == null
                ? Results.NotFound(new { message = $"Subtask {subtaskId} not found" })
                : Results.Ok(subtask);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> RetrySubtask(
        Guid repoId,
        Guid taskId,
        Guid subtaskId,
        SubtaskService service,
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

        try
        {
            var subtask = await service.RetrySubtaskAsync(taskId, subtaskId);
            return subtask == null
                ? Results.NotFound(new { message = $"Subtask {subtaskId} not found" })
                : Results.Ok(subtask);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
