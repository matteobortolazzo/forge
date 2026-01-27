namespace Forge.Api.Features.Repositories;

public static class RepositoryEndpoints
{
    public static void MapRepositoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/repositories")
            .WithTags("Repositories");

        group.MapGet("/", GetAllRepositories)
            .WithName("GetAllRepositories");

        group.MapGet("/{id:guid}", GetRepository)
            .WithName("GetRepository");

        group.MapPost("/", CreateRepository)
            .WithName("CreateRepository");

        group.MapPatch("/{id:guid}", UpdateRepository)
            .WithName("UpdateRepository");

        group.MapDelete("/{id:guid}", DeleteRepository)
            .WithName("DeleteRepository");

        group.MapPost("/{id:guid}/refresh", RefreshRepository)
            .WithName("RefreshRepository");
    }

    private static async Task<IResult> GetAllRepositories(IRepositoryService service)
    {
        var repositories = await service.GetAllAsync();
        return Results.Ok(repositories);
    }

    private static async Task<IResult> GetRepository(Guid id, IRepositoryService service)
    {
        var repository = await service.GetByIdAsync(id);
        return repository is null
            ? Results.NotFound()
            : Results.Ok(repository);
    }

    private static async Task<IResult> CreateRepository(CreateRepositoryDto dto, IRepositoryService service)
    {
        try
        {
            var repository = await service.CreateAsync(dto);
            return Results.Created($"/api/repositories/{repository.Id}", repository);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UpdateRepository(Guid id, UpdateRepositoryDto dto, IRepositoryService service)
    {
        var repository = await service.UpdateAsync(id, dto);
        return repository is null
            ? Results.NotFound()
            : Results.Ok(repository);
    }

    private static async Task<IResult> DeleteRepository(Guid id, IRepositoryService service)
    {
        try
        {
            var deleted = await service.DeleteAsync(id);
            return deleted
                ? Results.NoContent()
                : Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> RefreshRepository(Guid id, IRepositoryService service)
    {
        var repository = await service.RefreshAsync(id);
        return repository is null
            ? Results.NotFound()
            : Results.Ok(repository);
    }
}
