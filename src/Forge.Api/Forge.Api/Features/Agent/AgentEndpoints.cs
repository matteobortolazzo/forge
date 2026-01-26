namespace Forge.Api.Features.Agent;

public static class AgentEndpoints
{
    public static void MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/agent")
            .WithTags("Agent");

        group.MapGet("/status", GetAgentStatus)
            .WithName("GetAgentStatus");
    }

    private static IResult GetAgentStatus(IAgentRunnerService agentRunner)
    {
        var status = agentRunner.GetStatus();
        return Results.Ok(status);
    }
}
