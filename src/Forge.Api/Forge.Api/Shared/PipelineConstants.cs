namespace Forge.Api.Shared;

/// <summary>
/// Centralized pipeline state constants and transitions.
/// </summary>
public static class PipelineConstants
{
    /// <summary>
    /// Ordered list of all pipeline states for tasks.
    /// Tasks are leaf units that progress through the implementation pipeline.
    /// </summary>
    public static readonly PipelineState[] StateOrder =
    [
        PipelineState.Research,
        PipelineState.Planning,
        PipelineState.Implementing,
        PipelineState.Simplifying,
        PipelineState.Verifying,
        PipelineState.Reviewing,
        PipelineState.PrReady,
        PipelineState.Done
    ];

    /// <summary>
    /// States that require agent work and are eligible for scheduling.
    /// </summary>
    public static readonly PipelineState[] SchedulableStates =
    [
        PipelineState.Research,
        PipelineState.Planning,
        PipelineState.Implementing,
        PipelineState.Simplifying,
        PipelineState.Verifying,
        PipelineState.Reviewing
    ];

    /// <summary>
    /// Maps current state to next state after successful agent completion.
    /// </summary>
    public static readonly IReadOnlyDictionary<PipelineState, PipelineState> StateTransitions =
        new Dictionary<PipelineState, PipelineState>
        {
            { PipelineState.Research, PipelineState.Planning },
            { PipelineState.Planning, PipelineState.Implementing },
            { PipelineState.Implementing, PipelineState.Simplifying },
            { PipelineState.Simplifying, PipelineState.Verifying },
            { PipelineState.Verifying, PipelineState.Reviewing },
            { PipelineState.Reviewing, PipelineState.PrReady }
        };

    /// <summary>
    /// Gets the index of a state in the ordered pipeline.
    /// </summary>
    public static int GetStateIndex(PipelineState state) => Array.IndexOf(StateOrder, state);

    /// <summary>
    /// Determines if a transition between two states is valid (adjacent).
    /// </summary>
    public static bool IsValidTransition(PipelineState from, PipelineState to)
    {
        var fromIndex = GetStateIndex(from);
        var toIndex = GetStateIndex(to);
        return Math.Abs(toIndex - fromIndex) == 1;
    }

    /// <summary>
    /// Gets the next state after the given state, or null if at the end.
    /// </summary>
    public static PipelineState? GetNextState(PipelineState state)
    {
        return StateTransitions.TryGetValue(state, out var next) ? next : null;
    }
}

/// <summary>
/// Centralized backlog item state constants and transitions.
/// </summary>
public static class BacklogItemConstants
{
    /// <summary>
    /// Ordered list of all backlog item states.
    /// </summary>
    public static readonly BacklogItemState[] StateOrder =
    [
        BacklogItemState.New,
        BacklogItemState.Refining,
        BacklogItemState.Ready,
        BacklogItemState.Splitting,
        BacklogItemState.Executing,
        BacklogItemState.Done
    ];

    /// <summary>
    /// States that require agent work and are eligible for scheduling.
    /// </summary>
    public static readonly BacklogItemState[] SchedulableStates =
    [
        BacklogItemState.Refining,
        BacklogItemState.Splitting
    ];

    /// <summary>
    /// Maps current state to next state after successful agent completion.
    /// Note: Refining can loop back to itself or advance to Ready.
    /// </summary>
    public static readonly IReadOnlyDictionary<BacklogItemState, BacklogItemState> StateTransitions =
        new Dictionary<BacklogItemState, BacklogItemState>
        {
            { BacklogItemState.New, BacklogItemState.Refining },
            { BacklogItemState.Refining, BacklogItemState.Ready },  // After approval
            { BacklogItemState.Ready, BacklogItemState.Splitting },
            { BacklogItemState.Splitting, BacklogItemState.Executing },
            // Executing -> Done is derived from task completion
        };

    /// <summary>
    /// Gets the index of a state in the ordered backlog item lifecycle.
    /// </summary>
    public static int GetStateIndex(BacklogItemState state) => Array.IndexOf(StateOrder, state);

    /// <summary>
    /// Determines if a transition between two states is valid.
    /// </summary>
    public static bool IsValidTransition(BacklogItemState from, BacklogItemState to)
    {
        // Special case: Refining can loop back to itself
        if (from == BacklogItemState.Refining && to == BacklogItemState.Refining)
            return true;

        // Otherwise, must be adjacent forward transition
        var fromIndex = GetStateIndex(from);
        var toIndex = GetStateIndex(to);
        return toIndex == fromIndex + 1;
    }

    /// <summary>
    /// Gets the next state after the given state, or null if at the end.
    /// </summary>
    public static BacklogItemState? GetNextState(BacklogItemState state)
    {
        return StateTransitions.TryGetValue(state, out var next) ? next : null;
    }
}
