using Forge.Api.Shared;

namespace Forge.Api.Features.AgentQuestions;

/// <summary>
/// API endpoints for agent question management.
/// </summary>
public static class AgentQuestionEndpoints
{
    public static void MapAgentQuestionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/agent/questions")
            .WithTags("Agent Questions");

        group.MapGet("/pending", GetPending)
            .WithName("GetPendingQuestion")
            .WithSummary("Get the current pending agent question (if any)");

        group.MapGet("/{questionId:guid}", GetQuestion)
            .WithName("GetQuestion")
            .WithSummary("Get a specific agent question by ID");

        group.MapPost("/{questionId:guid}/answer", SubmitAnswer)
            .WithName("SubmitAnswer")
            .WithSummary("Submit an answer to an agent question");
    }

    private static async Task<IResult> GetPending(AgentQuestionService service)
    {
        var question = await service.GetPendingAsync();
        return question is null
            ? Results.Ok<AgentQuestionDto?>(null)
            : Results.Ok(AgentQuestionDto.FromEntity(question));
    }

    private static async Task<IResult> GetQuestion(Guid questionId, AgentQuestionService service)
    {
        var question = await service.GetByIdAsync(questionId);
        return question is null
            ? Results.NotFound(new { message = $"Question {questionId} not found" })
            : Results.Ok(AgentQuestionDto.FromEntity(question));
    }

    private static async Task<IResult> SubmitAnswer(
        Guid questionId,
        SubmitAnswerDto dto,
        AgentQuestionService service,
        AgentQuestionWaiter waiter)
    {
        var question = await service.GetByIdAsync(questionId);
        if (question is null)
        {
            return Results.NotFound(new { message = $"Question {questionId} not found" });
        }

        if (question.Status != AgentQuestionStatus.Pending)
        {
            return Results.BadRequest(new { message = $"Question is not pending (status: {question.Status})" });
        }

        var updated = await service.SubmitAnswerAsync(questionId, dto, waiter);
        return updated is null
            ? Results.NotFound(new { message = $"Question {questionId} not found" })
            : Results.Ok(AgentQuestionDto.FromEntity(updated));
    }
}
