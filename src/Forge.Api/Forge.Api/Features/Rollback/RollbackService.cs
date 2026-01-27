using System.Text.Json;
using Forge.Api.Data;
using Forge.Api.Data.Entities;
using Forge.Api.Features.Events;
using Forge.Api.Features.Worktree;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace Forge.Api.Features.Rollback;

/// <summary>
/// Service for handling pipeline rollback procedures.
/// </summary>
public interface IRollbackService
{
    /// <summary>
    /// Rolls back a subtask, reverting its worktree and preserving artifacts.
    /// </summary>
    Task<RollbackRecordEntity> RollbackSubtaskAsync(
        Guid subtaskId,
        RollbackTrigger trigger,
        string? notes = null,
        CancellationToken ct = default);

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
    /// Gets rollback records for a task.
    /// </summary>
    Task<IReadOnlyList<RollbackRecordEntity>> GetRollbackRecordsAsync(
        Guid taskId,
        CancellationToken ct = default);
}

/// <summary>
/// Implementation of IRollbackService.
/// </summary>
public class RollbackService : IRollbackService
{
    private readonly ForgeDbContext _db;
    private readonly IWorktreeService _worktreeService;
    private readonly ISseService _sseService;
    private readonly ILogger<RollbackService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public RollbackService(
        ForgeDbContext db,
        IWorktreeService worktreeService,
        ISseService sseService,
        ILogger<RollbackService> logger)
    {
        _db = db;
        _worktreeService = worktreeService;
        _sseService = sseService;
        _logger = logger;
    }

    public async Task<RollbackRecordEntity> RollbackSubtaskAsync(
        Guid subtaskId,
        RollbackTrigger trigger,
        string? notes = null,
        CancellationToken ct = default)
    {
        var subtask = await _db.Subtasks
            .Include(s => s.Artifacts)
            .FirstOrDefaultAsync(s => s.Id == subtaskId, ct)
            ?? throw new InvalidOperationException($"Subtask {subtaskId} not found");

        var task = await _db.Tasks.FindAsync([subtask.ParentTaskId], ct)
            ?? throw new InvalidOperationException($"Parent task {subtask.ParentTaskId} not found");

        _logger.LogInformation("Rolling back subtask {SubtaskId} due to {Trigger}", subtaskId, trigger);

        // Capture state before rollback
        var stateBefore = new RollbackStateBefore
        {
            Branch = subtask.BranchName,
            Commit = subtask.WorktreePath != null
                ? await _worktreeService.GetCurrentCommitAsync(subtask.WorktreePath, ct)
                : null,
            FilesChanged = subtask.WorktreePath != null
                ? (await _worktreeService.GetChangedFilesAsync(subtask.WorktreePath, ct)).ToList()
                : []
        };

        // Perform rollback actions
        var actionTaken = new RollbackActionTaken
        {
            WorktreeRemoved = false,
            BranchDeleted = false,
            CommitsReverted = []
        };

        // Remove worktree if it exists
        if (!string.IsNullOrEmpty(subtask.WorktreePath))
        {
            // Get repository path from task context or environment
            var repoPath = Environment.GetEnvironmentVariable("REPOSITORY_PATH")
                ?? Environment.CurrentDirectory;

            var result = await _worktreeService.RemoveWorktreeAsync(repoPath, subtaskId, ct);
            actionTaken = actionTaken with
            {
                WorktreeRemoved = result.Success,
                BranchDeleted = result.Success
            };
        }

        // Preserve artifact references
        var preservedArtifacts = subtask.Artifacts
            .Select(a => new PreservedArtifactInfo
            {
                Stage = a.ProducedInState.ToString(),
                Path = $"/api/tasks/{subtask.ParentTaskId}/artifacts/{a.Id}"
            })
            .ToList();

        // Create rollback record
        var rollbackRecord = new RollbackRecordEntity
        {
            Id = Guid.NewGuid(),
            TaskId = subtask.ParentTaskId,
            SubtaskId = subtaskId,
            Trigger = trigger,
            Timestamp = DateTime.UtcNow,
            StateBeforeJson = JsonSerializer.Serialize(stateBefore, JsonOptions),
            ActionTakenJson = JsonSerializer.Serialize(actionTaken, JsonOptions),
            PreservedArtifactsJson = JsonSerializer.Serialize(preservedArtifacts, JsonOptions),
            RecoveryOptionsJson = JsonSerializer.Serialize(GetRecoveryOptions(trigger), JsonOptions),
            Notes = notes
        };

        _db.RollbackRecords.Add(rollbackRecord);

        // Update subtask status
        subtask.Status = SubtaskStatus.Failed;
        subtask.FailureReason = $"Rolled back due to {trigger}: {notes}";
        subtask.WorktreePath = null;
        subtask.BranchName = null;

        await _db.SaveChangesAsync(ct);

        // Emit SSE event
        await EmitRollbackEventAsync(rollbackRecord, stateBefore, actionTaken, preservedArtifacts);

        _logger.LogInformation("Subtask {SubtaskId} rolled back successfully", subtaskId);

        return rollbackRecord;
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
                Stage = a.ProducedInState.ToString(),
                Path = $"/api/tasks/{taskId}/artifacts/{a.Id}"
            })
            .ToList();

        // Create rollback record
        var rollbackRecord = new RollbackRecordEntity
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            SubtaskId = null,
            Trigger = trigger,
            Timestamp = DateTime.UtcNow,
            StateBeforeJson = JsonSerializer.Serialize(stateBefore, JsonOptions),
            ActionTakenJson = JsonSerializer.Serialize(actionTaken, JsonOptions),
            PreservedArtifactsJson = JsonSerializer.Serialize(preservedArtifacts, JsonOptions),
            RecoveryOptionsJson = JsonSerializer.Serialize(GetRecoveryOptions(trigger), JsonOptions),
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
            task.SimplificationIterations = 0;
        }

        await _db.SaveChangesAsync(ct);

        // Emit SSE event
        await EmitRollbackEventAsync(rollbackRecord, stateBefore, actionTaken, preservedArtifacts);

        _logger.LogInformation("Task {TaskId} rolled back from {PreviousState} to {TargetState}",
            taskId, previousState, targetStage);

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

    private static List<string> GetRecoveryOptions(RollbackTrigger trigger)
    {
        return trigger switch
        {
            RollbackTrigger.MaxRetriesExceeded =>
            [
                "Retry with human guidance",
                "Modify acceptance criteria",
                "Skip subtask, continue pipeline",
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
            record.SubtaskId,
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
            JsonSerializer.Deserialize<List<string>>(record.RecoveryOptionsJson, JsonOptions) ?? []
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
