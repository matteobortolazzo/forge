using Forge.Api.Data;
using Forge.Api.Features.Events;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace Forge.Api.Features.Tasks;

/// <summary>
/// Service for managing parent task derived state.
/// Extracted to avoid duplication between TaskService and SchedulerService.
/// </summary>
public interface IParentStateService
{
    /// <summary>
    /// Updates parent's derived state when a child's state changes.
    /// Can be called with either child ID or parent ID.
    /// </summary>
    Task UpdateFromChildAsync(Guid childId);

    /// <summary>
    /// Updates parent's derived state directly by parent ID.
    /// </summary>
    Task UpdateByParentIdAsync(Guid parentId);
}

/// <summary>
/// Implementation of IParentStateService.
/// </summary>
public class ParentStateService(
    ForgeDbContext db,
    ISseService sse,
    ILogger<ParentStateService> logger) : IParentStateService
{
    public async Task UpdateFromChildAsync(Guid childId)
    {
        var child = await db.Tasks.FindAsync(childId);
        if (child?.ParentId is null) return;

        await UpdateByParentIdAsync(child.ParentId.Value);
    }

    public async Task UpdateByParentIdAsync(Guid parentId)
    {
        var parent = await db.Tasks
            .Include(t => t.Children)
            .FirstOrDefaultAsync(t => t.Id == parentId);

        if (parent is null) return;

        var childStates = parent.Children.Select(c => c.State);
        var newDerivedState = TaskService.ComputeDerivedState(childStates);

        if (parent.DerivedState != newDerivedState)
        {
            parent.DerivedState = newDerivedState;
            parent.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var progress = TaskService.ComputeProgress(childStates);
            var childDtos = parent.Children.Select(c => TaskDto.FromEntity(c)).ToList();
            var parentDto = TaskDto.FromEntity(parent, childDtos, progress);
            await sse.EmitTaskUpdatedAsync(parentDto);

            logger.LogInformation("Parent task {ParentId} derived state updated to {DerivedState}",
                parentId, newDerivedState);
        }
    }
}
