using Claude.CodeSdk.Mock;

namespace Forge.Api.Features.Mock;

/// <summary>
/// Endpoints for controlling mock scenarios during E2E testing.
/// Only available when CLAUDE_MOCK_MODE=true.
/// </summary>
public static class MockEndpoints
{
    /// <summary>
    /// Maps mock control endpoints.
    /// </summary>
    public static void MapMockEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mock")
            .WithTags("Mock");

        group.MapGet("/status", GetStatus)
            .WithName("GetMockStatus")
            .WithDescription("Get current mock configuration status");

        group.MapGet("/scenarios", GetScenarios)
            .WithName("GetMockScenarios")
            .WithDescription("Get all available mock scenarios");

        group.MapPost("/scenario", SetScenario)
            .WithName("SetMockScenario")
            .WithDescription("Set the default scenario or map a pattern to a scenario");

        group.MapDelete("/scenario/{pattern}", RemovePatternMapping)
            .WithName("RemovePatternMapping")
            .WithDescription("Remove a pattern-to-scenario mapping");

        group.MapPost("/reset", Reset)
            .WithName("ResetMock")
            .WithDescription("Reset mock configuration to defaults");
    }

    private static IResult GetStatus(MockScenarioProvider provider)
    {
        var status = new MockStatusDto(
            MockModeEnabled: true,
            DefaultScenarioId: provider.DefaultScenarioId,
            AvailableScenarios: provider.GetAllScenarios().Select(s => s.Id).ToList()
        );

        return Results.Ok(status);
    }

    private static IResult GetScenarios(MockScenarioProvider provider)
    {
        var scenarios = provider.GetAllScenarios()
            .Select(s => new MockScenarioDto(s.Id, s.Description, s.DelayBetweenMessagesMs, s.Messages.Count))
            .ToList();

        return Results.Ok(scenarios);
    }

    private static IResult SetScenario(SetScenarioRequest request, MockScenarioProvider provider)
    {
        try
        {
            if (!string.IsNullOrEmpty(request.Pattern))
            {
                // Map pattern to scenario
                provider.MapPatternToScenario(request.Pattern, request.ScenarioId);
            }
            else
            {
                // Set default scenario
                provider.SetDefaultScenario(request.ScenarioId);
            }

            return Results.Ok(new { success = true, message = $"Scenario '{request.ScenarioId}' configured" });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static IResult RemovePatternMapping(string pattern, MockScenarioProvider provider)
    {
        provider.RemovePatternMapping(pattern);
        return Results.Ok(new { success = true, message = $"Pattern '{pattern}' removed" });
    }

    private static IResult Reset(MockScenarioProvider provider)
    {
        provider.Reset();
        return Results.Ok(new { success = true, message = "Mock configuration reset to defaults" });
    }
}

/// <summary>
/// DTO for mock status response.
/// </summary>
public sealed record MockStatusDto(
    bool MockModeEnabled,
    string DefaultScenarioId,
    IReadOnlyList<string> AvailableScenarios
);

/// <summary>
/// DTO for scenario information.
/// </summary>
public sealed record MockScenarioDto(
    string Id,
    string? Description,
    int DelayBetweenMessagesMs,
    int MessageCount
);

/// <summary>
/// Request to set a scenario.
/// </summary>
public sealed record SetScenarioRequest(
    string ScenarioId,
    string? Pattern = null
);
