using System.Text.Json;
using System.Text.Json.Serialization;
using Forge.Api.Data.Entities;
using Forge.Api.Shared;

namespace Forge.Api.Features.AgentQuestions;

/// <summary>
/// A single option in a question from Claude Code's AskUserQuestion tool.
/// </summary>
/// <param name="Label">The display text for this option (1-5 words).</param>
/// <param name="Description">Explanation of what this option means or what will happen if chosen.</param>
public sealed record QuestionOption(
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("description")] string Description);

/// <summary>
/// A single question from Claude Code's AskUserQuestion tool.
/// </summary>
/// <param name="Question">The complete question to ask the user.</param>
/// <param name="Header">Short label displayed as a chip/tag (max 12 chars).</param>
/// <param name="Options">The available choices for this question (2-4 options).</param>
/// <param name="MultiSelect">Whether multiple answers can be selected.</param>
public sealed record AgentQuestionItem(
    [property: JsonPropertyName("question")] string Question,
    [property: JsonPropertyName("header")] string Header,
    [property: JsonPropertyName("options")] IReadOnlyList<QuestionOption> Options,
    [property: JsonPropertyName("multiSelect")] bool MultiSelect);

/// <summary>
/// The user's answer for a single question.
/// </summary>
/// <param name="QuestionIndex">Which question this answers (0-based index).</param>
/// <param name="SelectedOptionIndices">Indices of selected options.</param>
/// <param name="CustomAnswer">"Other" text if the user provided a custom answer.</param>
public sealed record QuestionAnswer(
    [property: JsonPropertyName("questionIndex")] int QuestionIndex,
    [property: JsonPropertyName("selectedOptionIndices")] IReadOnlyList<int> SelectedOptionIndices,
    [property: JsonPropertyName("customAnswer")] string? CustomAnswer);

/// <summary>
/// DTO for an agent question, sent to frontend via SSE and returned from API.
/// </summary>
public sealed record AgentQuestionDto(
    Guid Id,
    Guid? TaskId,
    Guid? BacklogItemId,
    string ToolUseId,
    IReadOnlyList<AgentQuestionItem> Questions,
    AgentQuestionStatus Status,
    DateTime RequestedAt,
    DateTime TimeoutAt,
    IReadOnlyList<QuestionAnswer>? Answers,
    DateTime? AnsweredAt)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static AgentQuestionDto FromEntity(AgentQuestionEntity entity)
    {
        var questions = JsonSerializer.Deserialize<List<AgentQuestionItem>>(entity.QuestionsJson, JsonOptions)
            ?? [];

        IReadOnlyList<QuestionAnswer>? answers = null;
        if (!string.IsNullOrEmpty(entity.AnswersJson))
        {
            answers = JsonSerializer.Deserialize<List<QuestionAnswer>>(entity.AnswersJson, JsonOptions);
        }

        return new AgentQuestionDto(
            entity.Id,
            entity.TaskId,
            entity.BacklogItemId,
            entity.ToolUseId,
            questions,
            entity.Status,
            entity.RequestedAt,
            entity.TimeoutAt,
            answers,
            entity.AnsweredAt);
    }
}

/// <summary>
/// Request DTO for submitting an answer to an agent question.
/// </summary>
/// <param name="Answers">The user's answers to the questions.</param>
public sealed record SubmitAnswerDto(
    [property: JsonPropertyName("answers")] IReadOnlyList<QuestionAnswer> Answers);

/// <summary>
/// Configuration options for agent questions.
/// </summary>
public sealed class AgentQuestionsOptions
{
    public const string SectionName = "AgentQuestions";

    /// <summary>
    /// Timeout in seconds for agent questions (default: 300 = 5 minutes).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;
}
