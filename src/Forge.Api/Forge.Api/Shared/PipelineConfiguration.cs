namespace Forge.Api.Shared;

/// <summary>
/// Configuration settings for the AI agent pipeline.
/// </summary>
public class PipelineConfiguration
{
    public const string SectionName = "Pipeline";

    /// <summary>
    /// Maximum number of retry attempts for the implementation stage.
    /// After this many failures, the task is paused for human intervention.
    /// </summary>
    public int MaxImplementationRetries { get; set; } = 3;

    /// <summary>
    /// Maximum number of simplification review iterations.
    /// If changes are still requested after this many iterations, escalate to human.
    /// </summary>
    public int MaxSimplificationIterations { get; set; } = 2;

    /// <summary>
    /// Maximum number of refining iterations for backlog items.
    /// If the backlog item still needs clarification after this many iterations, escalate to human.
    /// </summary>
    public int MaxRefiningIterations { get; set; } = 3;

    /// <summary>
    /// Confidence threshold below which human input is requested.
    /// Agents reporting confidence below this value trigger a human gate.
    /// </summary>
    public decimal ConfidenceThreshold { get; set; } = 0.7m;

    /// <summary>
    /// Human gate configuration for each gate type.
    /// </summary>
    public HumanGateConfiguration HumanGates { get; set; } = new();
}

/// <summary>
/// Configuration for human gates at different pipeline stages.
/// </summary>
public class HumanGateConfiguration
{
    /// <summary>
    /// Refining gate mode: "conditional" (based on confidence) or "mandatory".
    /// Applied to backlog items in the Refining state.
    /// </summary>
    public string Refining { get; set; } = "conditional";

    /// <summary>
    /// Split gate mode: "conditional" (based on confidence) or "mandatory".
    /// Applied to backlog items in the Splitting state.
    /// </summary>
    public string Split { get; set; } = "conditional";

    /// <summary>
    /// Planning gate mode: "conditional" (based on confidence + risk) or "mandatory".
    /// Applied to tasks in the Planning state.
    /// </summary>
    public string Planning { get; set; } = "conditional";

    /// <summary>
    /// PR gate mode: always "mandatory" - cannot be changed.
    /// Applied to tasks in the Reviewing state.
    /// </summary>
    public string Pr { get; set; } = "mandatory";

    /// <summary>
    /// Check if the refining gate requires human approval.
    /// </summary>
    public bool IsRefiningMandatory => Refining.Equals("mandatory", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Check if the split gate requires human approval.
    /// </summary>
    public bool IsSplitMandatory => Split.Equals("mandatory", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Check if the planning gate requires human approval.
    /// </summary>
    public bool IsPlanningMandatory => Planning.Equals("mandatory", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// PR gate is always mandatory - this is an invariant.
    /// </summary>
    public bool IsPrMandatory => true;
}
