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
    /// Confidence threshold below which human input is requested.
    /// Agents reporting confidence below this value trigger a human gate.
    /// </summary>
    public decimal ConfidenceThreshold { get; set; } = 0.7m;

    /// <summary>
    /// Whether to use git worktrees for subtask isolation.
    /// </summary>
    public bool WorktreeIsolation { get; set; } = true;

    /// <summary>
    /// Whether subtasks should be executed sequentially.
    /// When true, only one subtask runs at a time.
    /// </summary>
    public bool SequentialSubtasks { get; set; } = true;

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
    /// Split gate mode: "conditional" (based on confidence) or "mandatory".
    /// </summary>
    public string Split { get; set; } = "conditional";

    /// <summary>
    /// Planning gate mode: "conditional" (based on confidence + risk) or "mandatory".
    /// </summary>
    public string Planning { get; set; } = "conditional";

    /// <summary>
    /// PR gate mode: always "mandatory" - cannot be changed.
    /// </summary>
    public string Pr { get; set; } = "mandatory";

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
