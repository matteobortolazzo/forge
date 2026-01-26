namespace Forge.Api.Features.Repository;

public static class RepositoryEndpoints
{
    public static void MapRepositoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/repository")
            .WithTags("Repository");

        group.MapGet("/info", GetRepositoryInfo)
            .WithName("GetRepositoryInfo");
    }

    private static IResult GetRepositoryInfo(RepositoryService repositoryService)
    {
        var info = repositoryService.GetRepositoryInfo();
        return Results.Ok(info);
    }
}
