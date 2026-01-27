namespace Forge.Api.Shared;

/// <summary>
/// Centralized pipeline state constants and transitions.
/// </summary>
public static class PipelineConstants
{
    /// <summary>
    /// Ordered list of all pipeline states.
    /// </summary>
    public static readonly PipelineState[] StateOrder =
    [
        PipelineState.Backlog,
        PipelineState.Split,
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
        PipelineState.Split,
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
            { PipelineState.Split, PipelineState.Research },
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
