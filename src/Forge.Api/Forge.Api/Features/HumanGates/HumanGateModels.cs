using Forge.Api.Shared;

namespace Forge.Api.Features.HumanGates;

/// <summary>
/// DTO for human gate responses.
/// </summary>
public record HumanGateDto(
    Guid Id,
    Guid TaskId,
    Guid? SubtaskId,
    HumanGateType GateType,
    HumanGateStatus Status,
    decimal ConfidenceScore,
    string Reason,
    DateTime RequestedAt,
    DateTime? ResolvedAt,
    string? ResolvedBy,
    string? Resolution
);

/// <summary>
/// DTO for resolving a human gate.
/// </summary>
public record ResolveHumanGateDto(
    HumanGateStatus Status,
    string? Resolution = null,
    string? ResolvedBy = null
);
