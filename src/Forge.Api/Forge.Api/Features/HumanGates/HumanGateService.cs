using Forge.Api.Data;
using Forge.Api.Data.Entities;
using Forge.Api.Features.Events;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Forge.Api.Features.HumanGates;

/// <summary>
/// Service for managing human gates.
/// </summary>
public class HumanGateService
{
    private readonly ForgeDbContext _db;
    private readonly ISseService _sseService;
    private readonly PipelineConfiguration _pipelineConfig;
    private readonly ILogger<HumanGateService> _logger;

    public HumanGateService(
        ForgeDbContext db,
        ISseService sseService,
        IOptions<PipelineConfiguration> pipelineConfig,
        ILogger<HumanGateService> logger)
    {
        _db = db;
        _sseService = sseService;
        _pipelineConfig = pipelineConfig.Value;
        _logger = logger;
    }

    /// <summary>
    /// Creates a human gate for a task.
    /// </summary>
    public async Task<HumanGateEntity> CreateGateForTaskAsync(TaskEntity entity)
    {
        // Only Planning gate is supported for tasks
        var gateType = HumanGateType.Planning;

        var reason = entity.HumanInputRequested
            ? entity.HumanInputReason ?? "Agent requested human input"
            : entity.ConfidenceScore.HasValue
                ? $"Confidence score ({entity.ConfidenceScore:F2}) below threshold ({_pipelineConfig.ConfidenceThreshold:F2})"
                : "Mandatory approval required";

        var gate = new HumanGateEntity
        {
            Id = Guid.NewGuid(),
            TaskId = entity.Id,
            BacklogItemId = null,
            GateType = gateType,
            Status = HumanGateStatus.Pending,
            ConfidenceScore = entity.ConfidenceScore ?? 0,
            Reason = reason,
            RequestedAt = DateTime.UtcNow
        };

        _db.HumanGates.Add(gate);
        await _db.SaveChangesAsync();

        // Emit SSE event
        var gateDto = MapToEventDto(gate);
        await _sseService.EmitHumanGateRequestedAsync(gateDto);

        _logger.LogInformation("Created human gate {GateId} for task {TaskId} at state {State}",
            gate.Id, entity.Id, gateType);

        return gate;
    }

    /// <summary>
    /// Creates a human gate for a backlog item.
    /// </summary>
    public async Task<HumanGateEntity> CreateGateForBacklogItemAsync(BacklogItemEntity entity)
    {
        var gateType = entity.State switch
        {
            BacklogItemState.Refining => HumanGateType.Refining,
            BacklogItemState.Splitting => HumanGateType.Split,
            _ => HumanGateType.Refining
        };

        var reason = entity.HumanInputRequested
            ? entity.HumanInputReason ?? "Agent requested human input"
            : entity.ConfidenceScore.HasValue
                ? $"Confidence score ({entity.ConfidenceScore:F2}) below threshold ({_pipelineConfig.ConfidenceThreshold:F2})"
                : "Mandatory approval required";

        var gate = new HumanGateEntity
        {
            Id = Guid.NewGuid(),
            TaskId = null,
            BacklogItemId = entity.Id,
            GateType = gateType,
            Status = HumanGateStatus.Pending,
            ConfidenceScore = entity.ConfidenceScore ?? 0,
            Reason = reason,
            RequestedAt = DateTime.UtcNow
        };

        _db.HumanGates.Add(gate);
        await _db.SaveChangesAsync();

        // Emit SSE event
        var gateDto = MapToEventDto(gate);
        await _sseService.EmitHumanGateRequestedAsync(gateDto);

        _logger.LogInformation("Created human gate {GateId} for backlog item {BacklogItemId} at state {State}",
            gate.Id, entity.Id, gateType);

        return gate;
    }

    public async Task<IReadOnlyList<HumanGateDto>> GetGatesForTaskAsync(Guid taskId)
    {
        var gates = await _db.HumanGates
            .Where(g => g.TaskId == taskId)
            .OrderByDescending(g => g.RequestedAt)
            .ToListAsync();

        return gates.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<HumanGateDto>> GetGatesForBacklogItemAsync(Guid backlogItemId)
    {
        var gates = await _db.HumanGates
            .Where(g => g.BacklogItemId == backlogItemId)
            .OrderByDescending(g => g.RequestedAt)
            .ToListAsync();

        return gates.Select(MapToDto).ToList();
    }

    public async Task<HumanGateDto?> GetGateAsync(Guid gateId)
    {
        var gate = await _db.HumanGates.FindAsync(gateId);
        return gate == null ? null : MapToDto(gate);
    }

    public async Task<IReadOnlyList<HumanGateDto>> GetPendingGatesAsync()
    {
        var gates = await _db.HumanGates
            .Where(g => g.Status == HumanGateStatus.Pending)
            .OrderBy(g => g.RequestedAt)
            .ToListAsync();

        return gates.Select(MapToDto).ToList();
    }

    public async Task<HumanGateDto?> ResolveGateAsync(Guid gateId, ResolveHumanGateDto dto)
    {
        var gate = await _db.HumanGates
            .Include(g => g.Task)
            .Include(g => g.BacklogItem)
            .FirstOrDefaultAsync(g => g.Id == gateId);

        if (gate == null)
            return null;

        if (gate.Status != HumanGateStatus.Pending)
        {
            throw new InvalidOperationException($"Gate {gateId} is not in pending status");
        }

        gate.Status = dto.Status;
        gate.Resolution = dto.Resolution;
        gate.ResolvedBy = dto.ResolvedBy;
        gate.ResolvedAt = DateTime.UtcNow;

        // Update task state based on resolution
        if (gate.Task != null)
        {
            gate.Task.HasPendingGate = false;
            gate.Task.UpdatedAt = DateTime.UtcNow;

            if (dto.Status == HumanGateStatus.Rejected)
            {
                // If rejected, pause the task
                gate.Task.IsPaused = true;
                gate.Task.PauseReason = $"Human gate rejected: {dto.Resolution}";
                gate.Task.PausedAt = DateTime.UtcNow;
            }
        }

        // Update backlog item state based on resolution
        if (gate.BacklogItem != null)
        {
            gate.BacklogItem.HasPendingGate = false;
            gate.BacklogItem.UpdatedAt = DateTime.UtcNow;

            if (dto.Status == HumanGateStatus.Rejected)
            {
                // If rejected, pause the backlog item
                gate.BacklogItem.IsPaused = true;
                gate.BacklogItem.PauseReason = $"Human gate rejected: {dto.Resolution}";
                gate.BacklogItem.PausedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();

        var gateDto = MapToDto(gate);

        // Emit SSE event
        await _sseService.EmitHumanGateResolvedAsync(MapToEventDto(gate));

        _logger.LogInformation("Human gate {GateId} resolved with status {Status}", gateId, dto.Status);

        return gateDto;
    }

    private static HumanGateDto MapToDto(HumanGateEntity entity)
    {
        return new HumanGateDto(
            entity.Id,
            entity.TaskId,
            entity.BacklogItemId,
            entity.GateType,
            entity.Status,
            entity.ConfidenceScore,
            entity.Reason,
            entity.RequestedAt,
            entity.ResolvedAt,
            entity.ResolvedBy,
            entity.Resolution
        );
    }

    private static Events.HumanGateDto MapToEventDto(HumanGateEntity entity)
    {
        return new Events.HumanGateDto(
            entity.Id,
            entity.TaskId,
            entity.BacklogItemId,
            entity.GateType,
            entity.Status,
            entity.ConfidenceScore,
            entity.Reason,
            entity.RequestedAt,
            entity.ResolvedAt,
            entity.ResolvedBy,
            entity.Resolution
        );
    }
}
