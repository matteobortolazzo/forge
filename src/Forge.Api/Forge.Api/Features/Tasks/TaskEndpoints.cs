using Forge.Api.Features.Agent;
using Forge.Api.Features.Scheduler;

namespace Forge.Api.Features.Tasks;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks")
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
    }

    private static async Task<IResult> GetAllTasks(TaskService taskService)
    {
        var tasks = await taskService.GetAllAsync();
        return Results.Ok(tasks);
    }

    private static async Task<IResult> GetTask(Guid id, TaskService taskService)
    {
        var task = await taskService.GetByIdAsync(id);
        return task is null ? Results.NotFound() : Results.Ok(task);
    }

    private static async Task<IResult> CreateTask(CreateTaskDto dto, TaskService taskService)
    {
        var task = await taskService.CreateAsync(dto);
        return Results.Created($"/api/tasks/{task.Id}", task);
    }

    private static async Task<IResult> UpdateTask(Guid id, UpdateTaskDto dto, TaskService taskService)
    {
        var task = await taskService.UpdateAsync(id, dto);
        return task is null ? Results.NotFound() : Results.Ok(task);
    }

    private static async Task<IResult> DeleteTask(Guid id, TaskService taskService)
    {
        var deleted = await taskService.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> TransitionTask(Guid id, TransitionTaskDto dto, TaskService taskService)
    {
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

    private static async Task<IResult> GetTaskLogs(Guid id, TaskService taskService)
    {
        var task = await taskService.GetByIdAsync(id);
        if (task is null) return Results.NotFound();

        var logs = await taskService.GetLogsAsync(id);
        return Results.Ok(logs);
    }

    private static async Task<IResult> AbortAgent(Guid id, TaskService taskService, IAgentRunnerService agentRunner)
    {
        var task = await taskService.GetByIdAsync(id);
        if (task is null) return Results.NotFound();

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

    private static async Task<IResult> StartAgent(Guid id, TaskService taskService, IAgentRunnerService agentRunner)
    {
        var task = await taskService.GetByIdAsync(id);
        if (task is null) return Results.NotFound();

        var started = await agentRunner.StartAgentAsync(id, task.Title, task.Description);
        if (!started)
        {
            return Results.BadRequest(new { error = "Agent is already running on another task" });
        }

        // Return updated task
        var updatedTask = await taskService.GetByIdAsync(id);
        return Results.Ok(updatedTask);
    }

    private static async Task<IResult> PauseTask(Guid id, PauseTaskDto dto, SchedulerService schedulerService)
    {
        var task = await schedulerService.PauseTaskAsync(id, dto.Reason);
        return task is null ? Results.NotFound() : Results.Ok(task);
    }

    private static async Task<IResult> ResumeTask(Guid id, SchedulerService schedulerService)
    {
        var task = await schedulerService.ResumeTaskAsync(id);
        return task is null ? Results.NotFound() : Results.Ok(task);
    }
}
