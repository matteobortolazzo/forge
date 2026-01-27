using Forge.Api.Data;
using Forge.Api.Data.Entities;
using Forge.Api.Features.Events;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace Forge.Api.Features.HumanGates;

/// <summary>
/// Service for managing human gates.
/// </summary>
public class HumanGateService
{
    private readonly ForgeDbContext _db;
    private readonly ISseService _sseService;
    private readonly ILogger<HumanGateService> _logger;

    public HumanGateService(
        ForgeDbContext db,
        ISseService sseService,
        ILogger<HumanGateService> logger)
    {
        _db = db;
        _sseService = sseService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<HumanGateDto>> GetGatesForTaskAsync(Guid taskId)
    {
        var gates = await _db.HumanGates
            .Where(g => g.TaskId == taskId)
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

        await _db.SaveChangesAsync();

        var gateDto = MapToDto(gate);

        // Emit SSE event
        await _sseService.EmitHumanGateResolvedAsync(new Events.HumanGateDto(
            gate.Id,
            gate.TaskId,
            gate.SubtaskId,
            gate.GateType,
            gate.Status,
            gate.ConfidenceScore,
            gate.Reason,
            gate.RequestedAt,
            gate.ResolvedAt,
            gate.ResolvedBy,
            gate.Resolution
        ));

        _logger.LogInformation("Human gate {GateId} resolved with status {Status}", gateId, dto.Status);

        return gateDto;
    }

    private static HumanGateDto MapToDto(HumanGateEntity entity)
    {
        return new HumanGateDto(
            entity.Id,
            entity.TaskId,
            entity.SubtaskId,
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
