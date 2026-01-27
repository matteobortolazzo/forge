using System.Text.Json;
using Forge.Api.Data;
using Forge.Api.Data.Entities;
using Forge.Api.Features.Events;
using Forge.Api.Features.Worktree;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace Forge.Api.Features.Subtasks;

/// <summary>
/// Service for managing subtasks.
/// </summary>
public class SubtaskService
{
    private readonly ForgeDbContext _db;
    private readonly ISseService _sseService;
    private readonly IWorktreeService _worktreeService;
    private readonly ILogger<SubtaskService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SubtaskService(
        ForgeDbContext db,
        ISseService sseService,
        IWorktreeService worktreeService,
        ILogger<SubtaskService> logger)
    {
        _db = db;
        _sseService = sseService;
        _worktreeService = worktreeService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SubtaskDto>> GetSubtasksAsync(Guid taskId)
    {
        var subtasks = await _db.Subtasks
            .Where(s => s.ParentTaskId == taskId)
            .OrderBy(s => s.ExecutionOrder)
            .ToListAsync();

        return subtasks.Select(MapToDto).ToList();
    }

    public async Task<SubtaskDto?> GetSubtaskAsync(Guid taskId, Guid subtaskId)
    {
        var subtask = await _db.Subtasks
            .FirstOrDefaultAsync(s => s.ParentTaskId == taskId && s.Id == subtaskId);

        return subtask == null ? null : MapToDto(subtask);
    }

    public async Task<SubtaskDto> CreateSubtaskAsync(Guid taskId, CreateSubtaskDto dto)
    {
        var task = await _db.Tasks.FindAsync(taskId)
            ?? throw new InvalidOperationException($"Task {taskId} not found");

        // Get next execution order if not specified
        var executionOrder = dto.ExecutionOrder ?? (await _db.Subtasks
            .Where(s => s.ParentTaskId == taskId)
            .MaxAsync(s => (int?)s.ExecutionOrder) ?? 0) + 1;

        var subtask = new SubtaskEntity
        {
            Id = Guid.NewGuid(),
            ParentTaskId = taskId,
            Title = dto.Title,
            Description = dto.Description,
            AcceptanceCriteriaJson = JsonSerializer.Serialize(dto.AcceptanceCriteria ?? [], JsonOptions),
            EstimatedScope = dto.EstimatedScope,
            DependenciesJson = JsonSerializer.Serialize(dto.Dependencies ?? [], JsonOptions),
            ExecutionOrder = executionOrder,
            Status = SubtaskStatus.Pending,
            CurrentStage = PipelineState.Research,
            CreatedAt = DateTime.UtcNow
        };

        _db.Subtasks.Add(subtask);
        await _db.SaveChangesAsync();

        var subtaskDto = MapToDto(subtask);
        await _sseService.EmitSubtaskCreatedAsync(MapToSseDto(subtaskDto));

        _logger.LogInformation("Created subtask {SubtaskId} for task {TaskId}", subtask.Id, taskId);

        return subtaskDto;
    }

    public async Task<SubtaskDto?> UpdateSubtaskAsync(Guid taskId, Guid subtaskId, UpdateSubtaskDto dto)
    {
        var subtask = await _db.Subtasks
            .FirstOrDefaultAsync(s => s.ParentTaskId == taskId && s.Id == subtaskId);

        if (subtask == null)
            return null;

        if (dto.Title != null)
            subtask.Title = dto.Title;

        if (dto.Description != null)
            subtask.Description = dto.Description;

        if (dto.AcceptanceCriteria != null)
            subtask.AcceptanceCriteriaJson = JsonSerializer.Serialize(dto.AcceptanceCriteria, JsonOptions);

        if (dto.EstimatedScope.HasValue)
            subtask.EstimatedScope = dto.EstimatedScope.Value;

        if (dto.Dependencies != null)
            subtask.DependenciesJson = JsonSerializer.Serialize(dto.Dependencies, JsonOptions);

        if (dto.ExecutionOrder.HasValue)
            subtask.ExecutionOrder = dto.ExecutionOrder.Value;

        if (dto.Status.HasValue)
        {
            var previousStatus = subtask.Status;
            subtask.Status = dto.Status.Value;

            if (dto.Status == SubtaskStatus.InProgress && previousStatus != SubtaskStatus.InProgress)
            {
                subtask.StartedAt = DateTime.UtcNow;
            }
            else if (dto.Status == SubtaskStatus.Completed && previousStatus != SubtaskStatus.Completed)
            {
                subtask.CompletedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();

        return MapToDto(subtask);
    }

    public async Task<bool> DeleteSubtaskAsync(Guid taskId, Guid subtaskId)
    {
        var subtask = await _db.Subtasks
            .FirstOrDefaultAsync(s => s.ParentTaskId == taskId && s.Id == subtaskId);

        if (subtask == null)
            return false;

        // Remove worktree if exists
        if (!string.IsNullOrEmpty(subtask.WorktreePath))
        {
            var repoPath = Environment.GetEnvironmentVariable("REPOSITORY_PATH") ?? Environment.CurrentDirectory;
            await _worktreeService.RemoveWorktreeAsync(repoPath, subtaskId);
        }

        _db.Subtasks.Remove(subtask);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted subtask {SubtaskId} from task {TaskId}", subtaskId, taskId);

        return true;
    }

    public async Task<SubtaskDto?> StartSubtaskAsync(Guid taskId, Guid subtaskId)
    {
        var subtask = await _db.Subtasks
            .FirstOrDefaultAsync(s => s.ParentTaskId == taskId && s.Id == subtaskId);

        if (subtask == null)
            return null;

        // Check dependencies are completed
        var dependencies = JsonSerializer.Deserialize<List<Guid>>(subtask.DependenciesJson, JsonOptions) ?? [];
        if (dependencies.Count > 0)
        {
            var incompleteCount = await _db.Subtasks
                .CountAsync(s => dependencies.Contains(s.Id) && s.Status != SubtaskStatus.Completed);

            if (incompleteCount > 0)
            {
                throw new InvalidOperationException($"Cannot start subtask: {incompleteCount} dependencies not completed");
            }
        }

        // Create worktree
        var repoPath = Environment.GetEnvironmentVariable("REPOSITORY_PATH") ?? Environment.CurrentDirectory;
        var worktreeResult = await _worktreeService.CreateWorktreeAsync(repoPath, subtaskId);

        if (!worktreeResult.Success)
        {
            throw new InvalidOperationException($"Failed to create worktree: {worktreeResult.Error}");
        }

        subtask.Status = SubtaskStatus.InProgress;
        subtask.StartedAt = DateTime.UtcNow;
        subtask.WorktreePath = worktreeResult.Path;
        subtask.BranchName = worktreeResult.BranchName;

        await _db.SaveChangesAsync();

        var subtaskDto = MapToDto(subtask);
        await _sseService.EmitSubtaskStartedAsync(MapToSseDto(subtaskDto));

        _logger.LogInformation("Started subtask {SubtaskId} with worktree at {Path}", subtaskId, worktreeResult.Path);

        return subtaskDto;
    }

    public async Task<SubtaskDto?> RetrySubtaskAsync(Guid taskId, Guid subtaskId)
    {
        var subtask = await _db.Subtasks
            .FirstOrDefaultAsync(s => s.ParentTaskId == taskId && s.Id == subtaskId);

        if (subtask == null)
            return null;

        if (subtask.Status != SubtaskStatus.Failed)
        {
            throw new InvalidOperationException("Can only retry failed subtasks");
        }

        // Reset state for retry
        subtask.Status = SubtaskStatus.Pending;
        subtask.FailureReason = null;
        subtask.ImplementationRetries++;
        subtask.CurrentStage = PipelineState.Research;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Reset subtask {SubtaskId} for retry (attempt {Attempt})",
            subtaskId, subtask.ImplementationRetries);

        return MapToDto(subtask);
    }

    private static SubtaskDto MapToDto(SubtaskEntity entity)
    {
        var acceptanceCriteria = JsonSerializer.Deserialize<List<string>>(
            entity.AcceptanceCriteriaJson, JsonOptions) ?? [];
        var dependencies = JsonSerializer.Deserialize<List<Guid>>(
            entity.DependenciesJson, JsonOptions) ?? [];

        return new SubtaskDto(
            entity.Id,
            entity.ParentTaskId,
            entity.Title,
            entity.Description,
            acceptanceCriteria,
            entity.EstimatedScope,
            dependencies,
            entity.ExecutionOrder,
            entity.Status,
            entity.CurrentStage,
            entity.WorktreePath,
            entity.BranchName,
            entity.ConfidenceScore,
            entity.ImplementationRetries,
            entity.SimplificationIterations,
            entity.CreatedAt,
            entity.StartedAt,
            entity.CompletedAt,
            entity.FailureReason
        );
    }

    private static Events.SubtaskDto MapToSseDto(SubtaskDto dto)
    {
        return new Events.SubtaskDto(
            dto.Id,
            dto.ParentTaskId,
            dto.Title,
            dto.Description,
            dto.AcceptanceCriteria,
            dto.EstimatedScope,
            dto.Dependencies,
            dto.ExecutionOrder,
            dto.Status,
            dto.WorktreePath,
            dto.ConfidenceScore,
            dto.CreatedAt,
            dto.StartedAt,
            dto.CompletedAt,
            dto.FailureReason
        );
    }
}
