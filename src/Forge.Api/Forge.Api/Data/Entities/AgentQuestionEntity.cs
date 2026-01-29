using Forge.Api.Shared;

namespace Forge.Api.Data.Entities;

/// <summary>
/// Tracks interactive questions from Claude Code agents that require human answers.
/// Questions are triggered when an agent uses the AskUserQuestion tool.
/// Can belong to either a BacklogItem (for refining/splitting) or a Task (for implementation).
/// </summary>
public class AgentQuestionEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// The task this question belongs to (null for backlog item questions).
    /// </summary>
    public Guid? TaskId { get; set; }

    /// <summary>
    /// The backlog item this question belongs to (null for task questions).
    /// </summary>
    public Guid? BacklogItemId { get; set; }

    /// <summary>
    /// The tool use ID from Claude Code CLI, used to correlate the response.
    /// </summary>
    public required string ToolUseId { get; set; }

    /// <summary>
    /// JSON blob containing the list of questions from the agent.
    /// Serialized List&lt;AgentQuestionItem&gt;.
    /// </summary>
    public required string QuestionsJson { get; set; }

    /// <summary>
    /// Current status of the question.
    /// </summary>
    public AgentQuestionStatus Status { get; set; } = AgentQuestionStatus.Pending;

    /// <summary>
    /// When the question was requested.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the question will timeout if not answered.
    /// </summary>
    public DateTime TimeoutAt { get; set; }

    /// <summary>
    /// JSON blob containing the user's answers.
    /// Serialized List&lt;QuestionAnswer&gt;.
    /// </summary>
    public string? AnswersJson { get; set; }

    /// <summary>
    /// When the question was answered.
    /// </summary>
    public DateTime? AnsweredAt { get; set; }

    // Navigation properties
    public TaskEntity? Task { get; set; }
    public BacklogItemEntity? BacklogItem { get; set; }
}
