using Forge.Api.Shared;

namespace Forge.Api.Data.Entities;

/// <summary>
/// Tracks human approval gates in the pipeline.
/// Gates are triggered by low confidence scores or mandatory checkpoints.
/// Can belong to either a BacklogItem (for refining/splitting) or a Task (for implementation).
/// </summary>
public class HumanGateEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// The task this gate belongs to (null for backlog item gates).
    /// </summary>
    public Guid? TaskId { get; set; }

    /// <summary>
    /// The backlog item this gate belongs to (null for task gates).
    /// </summary>
    public Guid? BacklogItemId { get; set; }

    /// <summary>
    /// The type of gate (Refining, Split, Planning, PR).
    /// </summary>
    public HumanGateType GateType { get; set; }

    /// <summary>
    /// Current status of the gate.
    /// </summary>
    public HumanGateStatus Status { get; set; } = HumanGateStatus.Pending;

    /// <summary>
    /// Confidence score that triggered this gate (if conditional).
    /// </summary>
    public decimal ConfidenceScore { get; set; }

    /// <summary>
    /// Reason why the gate was triggered.
    /// </summary>
    public required string Reason { get; set; }

    /// <summary>
    /// When the gate was requested.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the gate was resolved (approved/rejected).
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Who resolved the gate (user identifier).
    /// </summary>
    public string? ResolvedBy { get; set; }

    /// <summary>
    /// Resolution notes (approval message or rejection reason).
    /// </summary>
    public string? Resolution { get; set; }

    /// <summary>
    /// JSON blob containing context for the gate decision.
    /// Includes artifact snapshots, plan details, etc.
    /// </summary>
    public string? ContextJson { get; set; }

    // Navigation properties
    public TaskEntity? Task { get; set; }
    public BacklogItemEntity? BacklogItem { get; set; }
}
