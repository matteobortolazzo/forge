using Forge.Api.Features.Agent;
using Forge.Api.Features.Repositories;
using Forge.Api.Shared;

namespace Forge.Api.Features.Backlog;

public static class BacklogEndpoints
{
    public static void MapBacklogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/repositories/{repoId:guid}/backlog")
            .WithTags("Backlog");

        group.MapGet("/", GetAllBacklogItems)
            .WithName("GetAllBacklogItems");

        group.MapGet("/{id:guid}", GetBacklogItem)
            .WithName("GetBacklogItem");

        group.MapPost("/", CreateBacklogItem)
            .WithName("CreateBacklogItem");

        group.MapPatch("/{id:guid}", UpdateBacklogItem)
            .WithName("UpdateBacklogItem");

        group.MapDelete("/{id:guid}", DeleteBacklogItem)
            .WithName("DeleteBacklogItem");

        group.MapPost("/{id:guid}/transition", TransitionBacklogItem)
            .WithName("TransitionBacklogItem");

        group.MapPost("/{id:guid}/start-agent", StartBacklogAgent)
            .WithName("StartBacklogAgent");

        group.MapPost("/{id:guid}/abort", AbortBacklogAgent)
            .WithName("AbortBacklogAgent");

        group.MapPost("/{id:guid}/pause", PauseBacklogItem)
            .WithName("PauseBacklogItem");

        group.MapPost("/{id:guid}/resume", ResumeBacklogItem)
            .WithName("ResumeBacklogItem");
    }

    private static async Task<IResult> GetAllBacklogItems(
        Guid repoId,
        BacklogService backlogService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var items = await backlogService.GetAllAsync(repoId);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetBacklogItem(
        Guid repoId,
        Guid id,
        BacklogService backlogService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var item = await backlogService.GetByIdAsync(id);

        if (item is not null && item.RepositoryId != repoId)
        {
            return Results.NotFound();
        }

        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    private static async Task<IResult> CreateBacklogItem(
        Guid repoId,
        CreateBacklogItemDto dto,
        BacklogService backlogService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var item = await backlogService.CreateAsync(repoId, dto);
        return Results.Created($"/api/repositories/{repoId}/backlog/{item.Id}", item);
    }

    private static async Task<IResult> UpdateBacklogItem(
        Guid repoId,
        Guid id,
        UpdateBacklogItemDto dto,
        BacklogService backlogService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var existing = await backlogService.GetByIdAsync(id);
        if (existing is not null && existing.RepositoryId != repoId)
        {
            return Results.NotFound();
        }

        var item = await backlogService.UpdateAsync(id, dto);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    private static async Task<IResult> DeleteBacklogItem(
        Guid repoId,
        Guid id,
        BacklogService backlogService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var existing = await backlogService.GetByIdAsync(id);
        if (existing is not null && existing.RepositoryId != repoId)
        {
            return Results.NotFound();
        }

        var deleted = await backlogService.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> TransitionBacklogItem(
        Guid repoId,
        Guid id,
        TransitionBacklogItemDto dto,
        BacklogService backlogService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var existing = await backlogService.GetByIdAsync(id);
        if (existing is not null && existing.RepositoryId != repoId)
        {
            return Results.NotFound();
        }

        try
        {
            var item = await backlogService.TransitionAsync(id, dto);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> StartBacklogAgent(
        Guid repoId,
        Guid id,
        BacklogService backlogService,
        IAgentRunnerService agentRunner,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var item = await backlogService.GetByIdAsync(id);
        if (item is null) return Results.NotFound();
        if (item.RepositoryId != repoId) return Results.NotFound();

        // Auto-transition from New to Refining if needed (so refining agent can start)
        if (item.State == BacklogItemState.New)
        {
            try
            {
                var transitionDto = new TransitionBacklogItemDto(BacklogItemState.Refining);
                item = await backlogService.TransitionAsync(id, transitionDto);
                if (item is null) return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }

        var started = await agentRunner.StartBacklogAgentAsync(id, item.Title, item.Description);
        if (!started)
        {
            return Results.BadRequest(new { error = "Agent is already running on another task" });
        }

        var updatedItem = await backlogService.GetByIdAsync(id);
        return Results.Ok(updatedItem);
    }

    private static async Task<IResult> AbortBacklogAgent(
        Guid repoId,
        Guid id,
        BacklogService backlogService,
        IAgentRunnerService agentRunner,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var item = await backlogService.GetByIdAsync(id);
        if (item is null) return Results.NotFound();
        if (item.RepositoryId != repoId) return Results.NotFound();

        var status = agentRunner.GetStatus();
        if (!status.IsRunning || status.CurrentBacklogItemId != id)
        {
            return Results.BadRequest(new { error = "No agent running for this backlog item" });
        }

        var aborted = await agentRunner.AbortAsync();
        if (!aborted)
        {
            return Results.BadRequest(new { error = "Failed to abort agent" });
        }

        var updatedItem = await backlogService.GetByIdAsync(id);
        return Results.Ok(updatedItem);
    }

    private static async Task<IResult> PauseBacklogItem(
        Guid repoId,
        Guid id,
        PauseBacklogItemDto dto,
        BacklogService backlogService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var existing = await backlogService.GetByIdAsync(id);
        if (existing is not null && existing.RepositoryId != repoId)
        {
            return Results.NotFound();
        }

        try
        {
            var item = await backlogService.PauseAsync(id, dto.Reason);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ResumeBacklogItem(
        Guid repoId,
        Guid id,
        BacklogService backlogService,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var existing = await backlogService.GetByIdAsync(id);
        if (existing is not null && existing.RepositoryId != repoId)
        {
            return Results.NotFound();
        }

        try
        {
            var item = await backlogService.ResumeAsync(id);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
