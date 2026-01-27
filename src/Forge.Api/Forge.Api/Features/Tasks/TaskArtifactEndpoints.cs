using Forge.Api.Data;
using Forge.Api.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Forge.Api.Features.Tasks;

public static class TaskArtifactEndpoints
{
    public static void MapTaskArtifactEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks/{taskId:guid}/artifacts")
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

    private static async Task<Results<Ok<List<ArtifactDto>>, NotFound>> GetArtifacts(
        Guid taskId,
        ForgeDbContext db)
    {
        var taskExists = await db.Tasks.AnyAsync(t => t.Id == taskId);
        if (!taskExists)
        {
            return TypedResults.NotFound();
        }

        var artifacts = await db.AgentArtifacts
            .Where(a => a.TaskId == taskId)
            .OrderBy(a => a.CreatedAt)
            .Select(a => ArtifactDto.FromEntity(a))
            .ToListAsync();

        return TypedResults.Ok(artifacts);
    }

    private static async Task<Results<Ok<ArtifactDto>, NotFound>> GetArtifact(
        Guid taskId,
        Guid artifactId,
        ForgeDbContext db)
    {
        var artifact = await db.AgentArtifacts
            .Where(a => a.TaskId == taskId && a.Id == artifactId)
            .FirstOrDefaultAsync();

        if (artifact == null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(ArtifactDto.FromEntity(artifact));
    }

    private static async Task<Results<Ok<ArtifactDto>, NotFound>> GetLatestArtifact(
        Guid taskId,
        ForgeDbContext db)
    {
        var artifact = await db.AgentArtifacts
            .Where(a => a.TaskId == taskId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        if (artifact == null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(ArtifactDto.FromEntity(artifact));
    }

    private static async Task<Results<Ok<List<ArtifactDto>>, NotFound>> GetArtifactsByState(
        Guid taskId,
        PipelineState state,
        ForgeDbContext db)
    {
        var taskExists = await db.Tasks.AnyAsync(t => t.Id == taskId);
        if (!taskExists)
        {
            return TypedResults.NotFound();
        }

        var artifacts = await db.AgentArtifacts
            .Where(a => a.TaskId == taskId && a.ProducedInState == state)
            .OrderBy(a => a.CreatedAt)
            .Select(a => ArtifactDto.FromEntity(a))
            .ToListAsync();

        return TypedResults.Ok(artifacts);
    }
}
