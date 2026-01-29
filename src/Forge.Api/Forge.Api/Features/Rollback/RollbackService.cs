using System.Text.Json;
using Forge.Api.Data;
using Forge.Api.Data.Entities;
using Forge.Api.Features.Events;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace Forge.Api.Features.Rollback;

/// <summary>
/// Service for handling pipeline rollback procedures.
/// </summary>
public interface IRollbackService
{
    /// <summary>
    /// Rolls back a task to a specific pipeline stage.
    /// </summary>
    Task<RollbackRecordEntity> RollbackToStageAsync(
        Guid taskId,
        PipelineState targetStage,
        RollbackTrigger trigger,
        string? notes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Rolls back a backlog item to a specific state.
    /// </summary>
    Task<RollbackRecordEntity> RollbackBacklogItemAsync(
        Guid backlogItemId,
        BacklogItemState targetState,
        RollbackTrigger trigger,
        string? notes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets rollback records for a task.
    /// </summary>
    Task<IReadOnlyList<RollbackRecordEntity>> GetRollbackRecordsAsync(
        Guid taskId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets rollback records for a backlog item.
    /// </summary>
    Task<IReadOnlyList<RollbackRecordEntity>> GetBacklogRollbackRecordsAsync(
        Guid backlogItemId,
        CancellationToken ct = default);
}

/// <summary>
/// Implementation of IRollbackService.
/// </summary>
public class RollbackService : IRollbackService
{
    private readonly ForgeDbContext _db;
    private readonly ISseService _sseService;
    private readonly ILogger<RollbackService> _logger;

    public RollbackService(
        ForgeDbContext db,
        ISseService sseService,
        ILogger<RollbackService> logger)
    {
        _db = db;
        _sseService = sseService;
        _logger = logger;
    }

    public async Task<RollbackRecordEntity> RollbackToStageAsync(
        Guid taskId,
        PipelineState targetStage,
        RollbackTrigger trigger,
        string? notes = null,
        CancellationToken ct = default)
    {
        var task = await _db.Tasks
            .Include(t => t.Artifacts)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new InvalidOperationException($"Task {taskId} not found");

        _logger.LogInformation("Rolling back task {TaskId} to stage {Stage} due to {Trigger}",
            taskId, targetStage, trigger);

        // Capture state before rollback
        var stateBefore = new RollbackStateBefore
        {
            Branch = null, // Task-level rollback doesn't involve branches
            Commit = null,
            FilesChanged = []
        };

        var actionTaken = new RollbackActionTaken
        {
            WorktreeRemoved = false,
            BranchDeleted = false,
            CommitsReverted = []
        };

        // Preserve artifact references for stages being rolled back
        var preservedArtifacts = task.Artifacts
            .Where(a => a.ProducedInState >= targetStage)
            .Select(a => new PreservedArtifactInfo
            {
                Stage = a.ProducedInState?.ToString() ?? "Unknown",
                Path = $"/api/repositories/{task.BacklogItem?.RepositoryId}/backlog/{task.BacklogItemId}/tasks/{taskId}/artifacts/{a.Id}"
            })
            .ToList();

        // Create rollback record
        var rollbackRecord = new RollbackRecordEntity
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            BacklogItemId = null,
            Trigger = trigger,
            Timestamp = DateTime.UtcNow,
            StateBeforeJson = JsonSerializer.Serialize(stateBefore, SharedJsonOptions.SnakeCaseLower),
            ActionTakenJson = JsonSerializer.Serialize(actionTaken, SharedJsonOptions.SnakeCaseLower),
            PreservedArtifactsJson = JsonSerializer.Serialize(preservedArtifacts, SharedJsonOptions.SnakeCaseLower),
            RecoveryOptionsJson = JsonSerializer.Serialize(GetRecoveryOptions(trigger), SharedJsonOptions.SnakeCaseLower),
            Notes = notes
        };

        _db.RollbackRecords.Add(rollbackRecord);

        // Update task state
        var previousState = task.State;
        task.State = targetStage;
        task.UpdatedAt = DateTime.UtcNow;

        // Reset iteration counters if rolling back to earlier stages
        if (targetStage <= PipelineState.Implementing)
        {
            task.ImplementationRetries = 0;
        }

        await _db.SaveChangesAsync(ct);

        // Emit SSE event
        await EmitRollbackEventAsync(rollbackRecord, stateBefore, actionTaken, preservedArtifacts);

        _logger.LogInformation("Task {TaskId} rolled back from {PreviousState} to {TargetState}",
            taskId, previousState, targetStage);

        return rollbackRecord;
    }

    public async Task<RollbackRecordEntity> RollbackBacklogItemAsync(
        Guid backlogItemId,
        BacklogItemState targetState,
        RollbackTrigger trigger,
        string? notes = null,
        CancellationToken ct = default)
    {
        var backlogItem = await _db.BacklogItems
            .Include(b => b.Artifacts)
            .FirstOrDefaultAsync(b => b.Id == backlogItemId, ct)
            ?? throw new InvalidOperationException($"Backlog item {backlogItemId} not found");

        _logger.LogInformation("Rolling back backlog item {BacklogItemId} to state {State} due to {Trigger}",
            backlogItemId, targetState, trigger);

        // Capture state before rollback
        var stateBefore = new RollbackStateBefore
        {
            Branch = null,
            Commit = null,
            FilesChanged = []
        };

        var actionTaken = new RollbackActionTaken
        {
            WorktreeRemoved = false,
            BranchDeleted = false,
            CommitsReverted = []
        };

        // Preserve artifact references
        var preservedArtifacts = backlogItem.Artifacts
            .Select(a => new PreservedArtifactInfo
            {
                Stage = a.ProducedInBacklogState?.ToString() ?? "Unknown",
                Path = $"/api/repositories/{backlogItem.RepositoryId}/backlog/{backlogItemId}/artifacts/{a.Id}"
            })
            .ToList();

        // Create rollback record
        var rollbackRecord = new RollbackRecordEntity
        {
            Id = Guid.NewGuid(),
            TaskId = null,
            BacklogItemId = backlogItemId,
            Trigger = trigger,
            Timestamp = DateTime.UtcNow,
            StateBeforeJson = JsonSerializer.Serialize(stateBefore, SharedJsonOptions.SnakeCaseLower),
            ActionTakenJson = JsonSerializer.Serialize(actionTaken, SharedJsonOptions.SnakeCaseLower),
            PreservedArtifactsJson = JsonSerializer.Serialize(preservedArtifacts, SharedJsonOptions.SnakeCaseLower),
            RecoveryOptionsJson = JsonSerializer.Serialize(GetRecoveryOptions(trigger), SharedJsonOptions.SnakeCaseLower),
            Notes = notes
        };

        _db.RollbackRecords.Add(rollbackRecord);

        // Update backlog item state
        var previousState = backlogItem.State;
        backlogItem.State = targetState;
        backlogItem.UpdatedAt = DateTime.UtcNow;

        // Reset iteration counters
        if (targetState == BacklogItemState.New)
        {
            backlogItem.RefiningIterations = 0;
        }

        await _db.SaveChangesAsync(ct);

        // Emit SSE event
        await EmitRollbackEventAsync(rollbackRecord, stateBefore, actionTaken, preservedArtifacts);

        _logger.LogInformation("Backlog item {BacklogItemId} rolled back from {PreviousState} to {TargetState}",
            backlogItemId, previousState, targetState);

        return rollbackRecord;
    }

    public async Task<IReadOnlyList<RollbackRecordEntity>> GetRollbackRecordsAsync(
        Guid taskId,
        CancellationToken ct = default)
    {
        return await _db.RollbackRecords
            .Where(r => r.TaskId == taskId)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<RollbackRecordEntity>> GetBacklogRollbackRecordsAsync(
        Guid backlogItemId,
        CancellationToken ct = default)
    {
        return await _db.RollbackRecords
            .Where(r => r.BacklogItemId == backlogItemId)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(ct);
    }

    private static List<string> GetRecoveryOptions(RollbackTrigger trigger)
    {
        return trigger switch
        {
            RollbackTrigger.MaxRetriesExceeded =>
            [
                "Retry with human guidance",
                "Modify acceptance criteria",
                "Abort entire task"
            ],
            RollbackTrigger.HumanRejected =>
            [
                "Revise implementation based on feedback",
                "Request clarification on requirements",
                "Abort entire task"
            ],
            RollbackTrigger.RegressionDetected =>
            [
                "Fix regression and retry",
                "Revert to last-known-good state",
                "Abort entire task"
            ],
            RollbackTrigger.ManualAbort =>
            [
                "Resume from checkpoint",
                "Start fresh",
                "Abort entire task"
            ],
            _ =>
            [
                "Retry with human guidance",
                "Abort entire task"
            ]
        };
    }

    private async Task EmitRollbackEventAsync(
        RollbackRecordEntity record,
        RollbackStateBefore stateBefore,
        RollbackActionTaken actionTaken,
        List<PreservedArtifactInfo> preservedArtifacts)
    {
        var dto = new RollbackDto(
            record.Id,
            record.TaskId,
            record.BacklogItemId,
            record.Trigger,
            record.Timestamp,
            new Features.Events.RollbackStateBefore(
                stateBefore.Branch,
                stateBefore.Commit,
                stateBefore.FilesChanged
            ),
            new Features.Events.RollbackActionTaken(
                actionTaken.WorktreeRemoved,
                actionTaken.BranchDeleted,
                actionTaken.CommitsReverted
            ),
            preservedArtifacts.Select(p => new PreservedArtifact(p.Stage, p.Path)).ToList(),
            JsonSerializer.Deserialize<List<string>>(record.RecoveryOptionsJson, SharedJsonOptions.SnakeCaseLower) ?? []
        );

        await _sseService.EmitRollbackInitiatedAsync(dto);
    }

    // Internal DTOs for JSON serialization
    private record RollbackStateBefore
    {
        public string? Branch { get; init; }
        public string? Commit { get; init; }
        public List<string> FilesChanged { get; init; } = [];
    }

    private record RollbackActionTaken
    {
        public bool WorktreeRemoved { get; init; }
        public bool BranchDeleted { get; init; }
        public List<string> CommitsReverted { get; init; } = [];
    }

    private record PreservedArtifactInfo
    {
        public string Stage { get; init; } = "";
        public string Path { get; init; } = "";
    }
}
