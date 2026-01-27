using Forge.Api.Features.Agent;
using Forge.Api.Features.Repositories;
using Forge.Api.Features.Scheduler;

namespace Forge.Api.Features.Tasks;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/repositories/{repoId:guid}/tasks")
            .WithTags("Tasks");

        group.MapGet("/", GetAllTasks)
            .WithName("GetAllTasks");

        group.MapGet("/{id:guid}", GetTask)
            .WithName("GetTask");

        group.MapPost("/", CreateTask)
            .WithName("CreateTask");

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

        group.MapPost("/{id:guid}/split", SplitTask)
            .WithName("SplitTask");

        group.MapPost("/{id:guid}/children", AddChild)
            .WithName("AddChild");

        group.MapGet("/{id:guid}/children", GetChildren)
            .WithName("GetChildren");
    }

    private static async Task<IResult> GetAllTasks(
        Guid repoId,
        TaskService taskService,
        IRepositoryService repositoryService,
        bool rootOnly = false)
    {
        // Verify repository exists
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var tasks = await taskService.GetAllAsync(repoId, rootOnly);
        return Results.Ok(tasks);
    }

    private static async Task<IResult> GetTask(
        Guid repoId,
        Guid id,
        TaskService taskService,
        IRepositoryService repositoryService,
        bool includeChildren = true)
    {
        // Verify repository exists
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var task = await taskService.GetByIdAsync(id, includeChildren);

        // Verify task belongs to this repository
        if (task is not null && task.RepositoryId != repoId)
        {
            return Results.NotFound();
        }

        return task is null ? Results.NotFound() : Results.Ok(task);
    }

    private static async Task<IResult> CreateTask(
        Guid repoId,
        CreateTaskDto dto,
        TaskService taskService,
        IRepositoryService repositoryService)
    {
        // Verify repository exists
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var task = await taskService.CreateAsync(repoId, dto);
        return Results.Created($"/api/repositories/{repoId}/tasks/{task.Id}", task);
    }

    private static async Task<IResult> UpdateTask(
        Guid repoId,
        Guid id,
        UpdateTaskDto dto,
        TaskService taskService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var existing = await taskService.GetByIdAsync(id);
        if (existing is not null && existing.RepositoryId != repoId)
        {
            return Results.NotFound();
        }

        var task = await taskService.UpdateAsync(id, dto);
        return task is null ? Results.NotFound() : Results.Ok(task);
    }

    private static async Task<IResult> DeleteTask(
        Guid repoId,
        Guid id,
        TaskService taskService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var existing = await taskService.GetByIdAsync(id);
        if (existing is not null && existing.RepositoryId != repoId)
        {
            return Results.NotFound();
        }

        var deleted = await taskService.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> TransitionTask(
        Guid repoId,
        Guid id,
        TransitionTaskDto dto,
        TaskService taskService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var existing = await taskService.GetByIdAsync(id);
        if (existing is not null && existing.RepositoryId != repoId)
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
        Guid id,
        TaskService taskService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var task = await taskService.GetByIdAsync(id);
        if (task is null) return Results.NotFound();
        if (task.RepositoryId != repoId) return Results.NotFound();

        var logs = await taskService.GetLogsAsync(id);
        return Results.Ok(logs);
    }

    private static async Task<IResult> AbortAgent(
        Guid repoId,
        Guid id,
        TaskService taskService,
        IAgentRunnerService agentRunner,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var task = await taskService.GetByIdAsync(id);
        if (task is null) return Results.NotFound();
        if (task.RepositoryId != repoId) return Results.NotFound();

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

        // Return updated task
        var updatedTask = await taskService.GetByIdAsync(id);
        return Results.Ok(updatedTask);
    }

    private static async Task<IResult> StartAgent(
        Guid repoId,
        Guid id,
        TaskService taskService,
        IAgentRunnerService agentRunner,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var task = await taskService.GetByIdAsync(id);
        if (task is null) return Results.NotFound();
        if (task.RepositoryId != repoId) return Results.NotFound();

        var started = await agentRunner.StartAgentAsync(id, task.Title, task.Description);
        if (!started)
        {
            return Results.BadRequest(new { error = "Agent is already running on another task" });
        }

        // Return updated task
        var updatedTask = await taskService.GetByIdAsync(id);
        return Results.Ok(updatedTask);
    }

    private static async Task<IResult> PauseTask(
        Guid repoId,
        Guid id,
        PauseTaskDto dto,
        SchedulerService schedulerService,
        IRepositoryService repositoryService,
        TaskService taskService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var existing = await taskService.GetByIdAsync(id);
        if (existing is not null && existing.RepositoryId != repoId)
        {
            return Results.NotFound();
        }

        var task = await schedulerService.PauseTaskAsync(id, dto.Reason);
        return task is null ? Results.NotFound() : Results.Ok(task);
    }

    private static async Task<IResult> ResumeTask(
        Guid repoId,
        Guid id,
        SchedulerService schedulerService,
        IRepositoryService repositoryService,
        TaskService taskService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var existing = await taskService.GetByIdAsync(id);
        if (existing is not null && existing.RepositoryId != repoId)
        {
            return Results.NotFound();
        }

        var task = await schedulerService.ResumeTaskAsync(id);
        return task is null ? Results.NotFound() : Results.Ok(task);
    }

    private static async Task<IResult> SplitTask(
        Guid repoId,
        Guid id,
        SplitTaskDto dto,
        TaskService taskService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var existing = await taskService.GetByIdAsync(id);
        if (existing is not null && existing.RepositoryId != repoId)
        {
            return Results.NotFound();
        }

        try
        {
            var result = await taskService.SplitTaskAsync(id, dto);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> AddChild(
        Guid repoId,
        Guid id,
        CreateSubtaskDto dto,
        TaskService taskService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var existing = await taskService.GetByIdAsync(id);
        if (existing is not null && existing.RepositoryId != repoId)
        {
            return Results.NotFound();
        }

        try
        {
            var child = await taskService.AddChildAsync(id, dto);
            return child is null
                ? Results.NotFound()
                : Results.Created($"/api/repositories/{repoId}/tasks/{child.Id}", child);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetChildren(
        Guid repoId,
        Guid id,
        TaskService taskService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var task = await taskService.GetByIdAsync(id);
        if (task is null) return Results.NotFound();
        if (task.RepositoryId != repoId) return Results.NotFound();

        var children = await taskService.GetChildrenAsync(id);
        return Results.Ok(children);
    }
}
