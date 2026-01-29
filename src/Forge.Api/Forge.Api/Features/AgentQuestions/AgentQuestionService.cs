using System.Text.Json;
using Forge.Api.Data;
using Forge.Api.Data.Entities;
using Forge.Api.Features.Events;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Forge.Api.Features.AgentQuestions;

/// <summary>
/// Scoped service for managing agent question lifecycle.
/// </summary>
public sealed class AgentQuestionService(
    ForgeDbContext db,
    ISseService sseService,
    IOptions<AgentQuestionsOptions> options,
    ILogger<AgentQuestionService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Creates a new agent question in the database and emits an SSE event.
    /// </summary>
    public async Task<AgentQuestionEntity> CreateQuestionAsync(
        Guid? taskId,
        Guid? backlogItemId,
        string toolUseId,
        IReadOnlyList<AgentQuestionItem> questions,
        CancellationToken ct = default)
    {
        var entity = new AgentQuestionEntity
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            BacklogItemId = backlogItemId,
            ToolUseId = toolUseId,
            QuestionsJson = JsonSerializer.Serialize(questions, JsonOptions),
            Status = AgentQuestionStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            TimeoutAt = DateTime.UtcNow.AddSeconds(options.Value.TimeoutSeconds)
        };

        db.AgentQuestions.Add(entity);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Agent question {QuestionId} created for {EntityType} {EntityId} with {QuestionCount} questions",
            entity.Id,
            taskId.HasValue ? "task" : "backlog item",
            taskId ?? backlogItemId,
            questions.Count);

        await sseService.EmitAgentQuestionRequestedAsync(AgentQuestionDto.FromEntity(entity));

        return entity;
    }

    /// <summary>
    /// Gets the current pending question (if any).
    /// </summary>
    public async Task<AgentQuestionEntity?> GetPendingAsync(CancellationToken ct = default)
    {
        return await db.AgentQuestions
            .Where(q => q.Status == AgentQuestionStatus.Pending)
            .OrderByDescending(q => q.RequestedAt)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Gets a question by ID.
    /// </summary>
    public async Task<AgentQuestionEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.AgentQuestions
            .Include(q => q.Task)
            .Include(q => q.BacklogItem)
            .FirstOrDefaultAsync(q => q.Id == id, ct);
    }

    /// <summary>
    /// Submits an answer to a pending question.
    /// </summary>
    public async Task<AgentQuestionEntity?> SubmitAnswerAsync(
        Guid questionId,
        SubmitAnswerDto answer,
        AgentQuestionWaiter waiter,
        CancellationToken ct = default)
    {
        var entity = await db.AgentQuestions.FindAsync([questionId], ct);
        if (entity is null)
        {
            logger.LogWarning("Question {QuestionId} not found for answer submission", questionId);
            return null;
        }

        if (entity.Status != AgentQuestionStatus.Pending)
        {
            logger.LogWarning(
                "Question {QuestionId} is not pending (status: {Status}), cannot submit answer",
                questionId, entity.Status);
            return null;
        }

        entity.AnswersJson = JsonSerializer.Serialize(answer.Answers, JsonOptions);
        entity.AnsweredAt = DateTime.UtcNow;
        entity.Status = AgentQuestionStatus.Answered;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Question {QuestionId} answered with {AnswerCount} answers",
            questionId, answer.Answers.Count);

        // Signal the waiter so the tool permission handler can continue
        waiter.TrySetAnswer(questionId, answer);

        await sseService.EmitAgentQuestionAnsweredAsync(AgentQuestionDto.FromEntity(entity));

        return entity;
    }

    /// <summary>
    /// Marks a question as timed out.
    /// </summary>
    public async Task MarkTimeoutAsync(Guid questionId, CancellationToken ct = default)
    {
        var entity = await db.AgentQuestions.FindAsync([questionId], ct);
        if (entity is null || entity.Status != AgentQuestionStatus.Pending)
            return;

        entity.Status = AgentQuestionStatus.Timeout;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Question {QuestionId} timed out", questionId);

        await sseService.EmitAgentQuestionTimeoutAsync(AgentQuestionDto.FromEntity(entity));
    }

    /// <summary>
    /// Marks a question as cancelled (e.g., when the task is aborted).
    /// </summary>
    public async Task MarkCancelledAsync(Guid questionId, CancellationToken ct = default)
    {
        var entity = await db.AgentQuestions.FindAsync([questionId], ct);
        if (entity is null || entity.Status != AgentQuestionStatus.Pending)
            return;

        entity.Status = AgentQuestionStatus.Cancelled;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Question {QuestionId} cancelled", questionId);

        await sseService.EmitAgentQuestionCancelledAsync(questionId);
    }

    /// <summary>
    /// Marks all pending questions for a task as cancelled.
    /// </summary>
    public async Task CancelAllForTaskAsync(Guid taskId, AgentQuestionWaiter waiter, CancellationToken ct = default)
    {
        var pending = await db.AgentQuestions
            .Where(q => q.TaskId == taskId && q.Status == AgentQuestionStatus.Pending)
            .ToListAsync(ct);

        foreach (var entity in pending)
        {
            entity.Status = AgentQuestionStatus.Cancelled;
            waiter.CancelQuestion(entity.Id);
            logger.LogInformation("Question {QuestionId} cancelled for task {TaskId}", entity.Id, taskId);
        }

        if (pending.Count > 0)
        {
            await db.SaveChangesAsync(ct);

            foreach (var entity in pending)
            {
                await sseService.EmitAgentQuestionCancelledAsync(entity.Id);
            }
        }
    }

    /// <summary>
    /// Marks all pending questions for a backlog item as cancelled.
    /// </summary>
    public async Task CancelAllForBacklogItemAsync(Guid backlogItemId, AgentQuestionWaiter waiter, CancellationToken ct = default)
    {
        var pending = await db.AgentQuestions
            .Where(q => q.BacklogItemId == backlogItemId && q.Status == AgentQuestionStatus.Pending)
            .ToListAsync(ct);

        foreach (var entity in pending)
        {
            entity.Status = AgentQuestionStatus.Cancelled;
            waiter.CancelQuestion(entity.Id);
            logger.LogInformation("Question {QuestionId} cancelled for backlog item {BacklogItemId}", entity.Id, backlogItemId);
        }

        if (pending.Count > 0)
        {
            await db.SaveChangesAsync(ct);

            foreach (var entity in pending)
            {
                await sseService.EmitAgentQuestionCancelledAsync(entity.Id);
            }
        }
    }

    /// <summary>
    /// Marks all pending questions as cancelled on startup/shutdown.
    /// </summary>
    public async Task CancelAllPendingAsync(CancellationToken ct = default)
    {
        var pending = await db.AgentQuestions
            .Where(q => q.Status == AgentQuestionStatus.Pending)
            .ToListAsync(ct);

        foreach (var entity in pending)
        {
            entity.Status = AgentQuestionStatus.Cancelled;
            logger.LogInformation("Question {QuestionId} cancelled on startup", entity.Id);
        }

        if (pending.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }
}
