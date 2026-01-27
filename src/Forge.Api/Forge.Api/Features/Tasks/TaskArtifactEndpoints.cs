using Forge.Api.Data;
using Forge.Api.Features.Repositories;
using Forge.Api.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Forge.Api.Features.Tasks;

public static class TaskArtifactEndpoints
{
    public static void MapTaskArtifactEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/repositories/{repoId:guid}/tasks/{taskId:guid}/artifacts")
            .WithTags("Task Artifacts");

        group.MapGet("/", GetArtifacts)
            .WithName("GetTaskArtifacts")
            .WithSummary("Get all artifacts for a task");

        group.MapGet("/{artifactId:guid}", GetArtifact)
            .WithName("GetTaskArtifact")
            .WithSummary("Get a specific artifact by ID");

        group.MapGet("/latest", GetLatestArtifact)
            .WithName("GetLatestTaskArtifact")
            .WithSummary("Get the most recent artifact for a task");

        group.MapGet("/by-state/{state}", GetArtifactsByState)
            .WithName("GetTaskArtifactsByState")
            .WithSummary("Get artifacts produced in a specific pipeline state");
    }

    private static async Task<IResult> GetArtifacts(
        Guid repoId,
        Guid taskId,
        ForgeDbContext db,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null || task.RepositoryId != repoId)
        {
            return Results.NotFound();
        }

        var artifacts = await db.AgentArtifacts
            .Where(a => a.TaskId == taskId)
            .OrderBy(a => a.CreatedAt)
            .Select(a => ArtifactDto.FromEntity(a))
            .ToListAsync();

        return Results.Ok(artifacts);
    }

    private static async Task<IResult> GetArtifact(
        Guid repoId,
        Guid taskId,
        Guid artifactId,
        ForgeDbContext db,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null || task.RepositoryId != repoId)
        {
            return Results.NotFound();
        }

        var artifact = await db.AgentArtifacts
            .Where(a => a.TaskId == taskId && a.Id == artifactId)
            .FirstOrDefaultAsync();

        if (artifact == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(ArtifactDto.FromEntity(artifact));
    }

    private static async Task<IResult> GetLatestArtifact(
        Guid repoId,
        Guid taskId,
        ForgeDbContext db,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null || task.RepositoryId != repoId)
        {
            return Results.NotFound();
        }

        var artifact = await db.AgentArtifacts
            .Where(a => a.TaskId == taskId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        if (artifact == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(ArtifactDto.FromEntity(artifact));
    }

    private static async Task<IResult> GetArtifactsByState(
        Guid repoId,
        Guid taskId,
        PipelineState state,
        ForgeDbContext db,
        IRepositoryService repositoryService)
    {
        var repo = await repositoryService.GetByIdAsync(repoId);
        if (repo is null) return Results.NotFound(new { error = "Repository not found" });

        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null || task.RepositoryId != repoId)
        {
            return Results.NotFound();
        }

        var artifacts = await db.AgentArtifacts
            .Where(a => a.TaskId == taskId && a.ProducedInState == state)
            .OrderBy(a => a.CreatedAt)
            .Select(a => ArtifactDto.FromEntity(a))
            .ToListAsync();

        return Results.Ok(artifacts);
    }
}
