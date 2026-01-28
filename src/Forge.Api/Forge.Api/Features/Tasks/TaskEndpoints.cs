using Forge.Api.Features.Agent;
using Forge.Api.Features.Backlog;
using Forge.Api.Features.Repositories;

namespace Forge.Api.Features.Tasks;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/repositories/{repoId:guid}/backlog/{backlogItemId:guid}/tasks")
            .WithTags("Tasks");

        group.MapGet("/", GetAllTasks)
            .WithName("GetAllTasks");

        group.MapGet("/{id:guid}", GetTask)
            .WithName("GetTask");

        group.MapPatch("/{id:guid}", UpdateTask)
            .WithName("UpdateTask");

        group.MapDelete("/{id:guid}", DeleteTask)
            .WithName("DeleteTask");

        group.MapPost("/{id:guid}/transition", TransitionTask)
            .WithName("TransitionTask");

        group.MapGet("/{id:guid}/logs", GetTaskLogs)
            .WithName("GetTaskLogs");

        group.MapPost("/{id:guid}/abort", AbortAgent)
            .WithName("AbortAgent");

        group.MapPost("/{id:guid}/start-agent", StartAgent)
            .WithName("StartAgent");

        group.MapPost("/{id:guid}/pause", PauseTask)
            .WithName("PauseTask");

        group.MapPost("/{id:guid}/resume", ResumeTask)
            .WithName("ResumeTask");
    }

    private static async Task<IResult> GetAllTasks(
        Guid repoId,
        Guid backlogItemId,
        TaskService taskService,
        BacklogService backlogService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var backlogItem = await backlogService.GetByIdAsync(backlogItemId);
        if (backlogItem is null) return Results.NotFound(new { error = "Backlog item not found" });
        if (backlogItem.RepositoryId != repoId) return Results.NotFound(new { error = "Backlog item not found" });

        var tasks = await taskService.GetAllAsync(backlogItemId);
        return Results.Ok(tasks);
    }

    private static async Task<IResult> GetTask(
        Guid repoId,
        Guid backlogItemId,
        Guid id,
        TaskService taskService,
        BacklogService backlogService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var backlogItem = await backlogService.GetByIdAsync(backlogItemId);
        if (backlogItem is null) return Results.NotFound(new { error = "Backlog item not found" });
        if (backlogItem.RepositoryId != repoId) return Results.NotFound(new { error = "Backlog item not found" });

        var task = await taskService.GetByIdAsync(id);

        if (task is null) return Results.NotFound();
        if (task.BacklogItemId != backlogItemId) return Results.NotFound();

        return Results.Ok(task);
    }

    private static async Task<IResult> UpdateTask(
        Guid repoId,
        Guid backlogItemId,
        Guid id,
        UpdateTaskDto dto,
        TaskService taskService,
        BacklogService backlogService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var backlogItem = await backlogService.GetByIdAsync(backlogItemId);
        if (backlogItem is null) return Results.NotFound(new { error = "Backlog item not found" });
        if (backlogItem.RepositoryId != repoId) return Results.NotFound(new { error = "Backlog item not found" });

        var existing = await taskService.GetByIdAsync(id);
        if (existing is not null && existing.BacklogItemId != backlogItemId)
        {
            return Results.NotFound();
        }

        var task = await taskService.UpdateAsync(id, dto);
        return task is null ? Results.NotFound() : Results.Ok(task);
    }

    private static async Task<IResult> DeleteTask(
        Guid repoId,
        Guid backlogItemId,
        Guid id,
        TaskService taskService,
        BacklogService backlogService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var backlogItem = await backlogService.GetByIdAsync(backlogItemId);
        if (backlogItem is null) return Results.NotFound(new { error = "Backlog item not found" });
        if (backlogItem.RepositoryId != repoId) return Results.NotFound(new { error = "Backlog item not found" });

        var existing = await taskService.GetByIdAsync(id);
        if (existing is not null && existing.BacklogItemId != backlogItemId)
        {
            return Results.NotFound();
        }

        var deleted = await taskService.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> TransitionTask(
        Guid repoId,
        Guid backlogItemId,
        Guid id,
        TransitionTaskDto dto,
        TaskService taskService,
        BacklogService backlogService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var backlogItem = await backlogService.GetByIdAsync(backlogItemId);
        if (backlogItem is null) return Results.NotFound(new { error = "Backlog item not found" });
        if (backlogItem.RepositoryId != repoId) return Results.NotFound(new { error = "Backlog item not found" });

        var existing = await taskService.GetByIdAsync(id);
        if (existing is not null && existing.BacklogItemId != backlogItemId)
        {
            return Results.NotFound();
        }

        try
        {
            var task = await taskService.TransitionAsync(id, dto);
            return task is null ? Results.NotFound() : Results.Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetTaskLogs(
        Guid repoId,
        Guid backlogItemId,
        Guid id,
        TaskService taskService,
        BacklogService backlogService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var backlogItem = await backlogService.GetByIdAsync(backlogItemId);
        if (backlogItem is null) return Results.NotFound(new { error = "Backlog item not found" });
        if (backlogItem.RepositoryId != repoId) return Results.NotFound(new { error = "Backlog item not found" });

        var task = await taskService.GetByIdAsync(id);
        if (task is null) return Results.NotFound();
        if (task.BacklogItemId != backlogItemId) return Results.NotFound();

        var logs = await taskService.GetLogsAsync(id);
        return Results.Ok(logs);
    }

    private static async Task<IResult> AbortAgent(
        Guid repoId,
        Guid backlogItemId,
        Guid id,
        TaskService taskService,
        BacklogService backlogService,
        IAgentRunnerService agentRunner,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var backlogItem = await backlogService.GetByIdAsync(backlogItemId);
        if (backlogItem is null) return Results.NotFound(new { error = "Backlog item not found" });
        if (backlogItem.RepositoryId != repoId) return Results.NotFound(new { error = "Backlog item not found" });

        var task = await taskService.GetByIdAsync(id);
        if (task is null) return Results.NotFound();
        if (task.BacklogItemId != backlogItemId) return Results.NotFound();

        var status = agentRunner.GetStatus();
        if (!status.IsRunning || status.CurrentTaskId != id)
        {
            return Results.BadRequest(new { error = "No agent running for this task" });
        }

        var aborted = await agentRunner.AbortAsync();
        if (!aborted)
        {
            return Results.BadRequest(new { error = "Failed to abort agent" });
        }

        var updatedTask = await taskService.GetByIdAsync(id);
        return Results.Ok(updatedTask);
    }

    private static async Task<IResult> StartAgent(
        Guid repoId,
        Guid backlogItemId,
        Guid id,
        TaskService taskService,
        BacklogService backlogService,
        IAgentRunnerService agentRunner,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var backlogItem = await backlogService.GetByIdAsync(backlogItemId);
        if (backlogItem is null) return Results.NotFound(new { error = "Backlog item not found" });
        if (backlogItem.RepositoryId != repoId) return Results.NotFound(new { error = "Backlog item not found" });

        var task = await taskService.GetByIdAsync(id);
        if (task is null) return Results.NotFound();
        if (task.BacklogItemId != backlogItemId) return Results.NotFound();

        var started = await agentRunner.StartAgentAsync(id, task.Title, task.Description);
        if (!started)
        {
            return Results.BadRequest(new { error = "Agent is already running on another task" });
        }

        var updatedTask = await taskService.GetByIdAsync(id);
        return Results.Ok(updatedTask);
    }

    private static async Task<IResult> PauseTask(
        Guid repoId,
        Guid backlogItemId,
        Guid id,
        PauseTaskDto dto,
        TaskService taskService,
        BacklogService backlogService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var backlogItem = await backlogService.GetByIdAsync(backlogItemId);
        if (backlogItem is null) return Results.NotFound(new { error = "Backlog item not found" });
        if (backlogItem.RepositoryId != repoId) return Results.NotFound(new { error = "Backlog item not found" });

        var existing = await taskService.GetByIdAsync(id);
        if (existing is not null && existing.BacklogItemId != backlogItemId)
        {
            return Results.NotFound();
        }

        try
        {
            var task = await taskService.PauseAsync(id, dto.Reason);
            return task is null ? Results.NotFound() : Results.Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ResumeTask(
        Guid repoId,
        Guid backlogItemId,
        Guid id,
        TaskService taskService,
        BacklogService backlogService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var backlogItem = await backlogService.GetByIdAsync(backlogItemId);
        if (backlogItem is null) return Results.NotFound(new { error = "Backlog item not found" });
        if (backlogItem.RepositoryId != repoId) return Results.NotFound(new { error = "Backlog item not found" });

        var existing = await taskService.GetByIdAsync(id);
        if (existing is not null && existing.BacklogItemId != backlogItemId)
        {
            return Results.NotFound();
        }

        try
        {
            var task = await taskService.ResumeAsync(id);
            return task is null ? Results.NotFound() : Results.Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
