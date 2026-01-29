using System.Collections.Concurrent;

namespace Forge.Api.Features.AgentQuestions;

/// <summary>
/// Singleton service that manages in-memory coordination for agent questions.
/// Uses TaskCompletionSource to allow the tool permission handler to wait for user answers.
/// </summary>
public sealed class AgentQuestionWaiter
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<SubmitAnswerDto>> _pending = new();

    /// <summary>
    /// Creates a new waiter for a question. The tool permission handler calls this
    /// when intercepting an AskUserQuestion tool call.
    /// </summary>
    /// <param name="questionId">The question entity ID.</param>
    /// <returns>A TaskCompletionSource that completes when the user answers.</returns>
    public TaskCompletionSource<SubmitAnswerDto> CreateWaiter(Guid questionId)
    {
        var tcs = new TaskCompletionSource<SubmitAnswerDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[questionId] = tcs;
        return tcs;
    }

    /// <summary>
    /// Sets the answer for a pending question. Called by the answer endpoint
    /// when the user submits their response.
    /// </summary>
    /// <param name="questionId">The question entity ID.</param>
    /// <param name="answer">The user's answer.</param>
    /// <returns>True if the answer was set; false if no waiter exists for this question.</returns>
    public bool TrySetAnswer(Guid questionId, SubmitAnswerDto answer)
    {
        if (_pending.TryRemove(questionId, out var tcs))
        {
            return tcs.TrySetResult(answer);
        }
        return false;
    }

    /// <summary>
    /// Cancels a specific pending question. Called when the task/backlog item is aborted.
    /// </summary>
    /// <param name="questionId">The question entity ID.</param>
    public void CancelQuestion(Guid questionId)
    {
        if (_pending.TryRemove(questionId, out var tcs))
        {
            tcs.TrySetCanceled();
        }
    }

    /// <summary>
    /// Cancels all pending questions. Called on application shutdown or agent abort.
    /// </summary>
    public void CancelAll()
    {
        foreach (var kvp in _pending)
        {
            if (_pending.TryRemove(kvp.Key, out var tcs))
            {
                tcs.TrySetCanceled();
            }
        }
    }

    /// <summary>
    /// Checks if there's a pending waiter for a question.
    /// </summary>
    /// <param name="questionId">The question entity ID.</param>
    /// <returns>True if a waiter exists and is not completed.</returns>
    public bool HasPendingWaiter(Guid questionId)
    {
        return _pending.TryGetValue(questionId, out var tcs) && !tcs.Task.IsCompleted;
    }
}
