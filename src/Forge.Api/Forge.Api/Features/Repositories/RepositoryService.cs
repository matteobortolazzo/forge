using System.Diagnostics;
using Forge.Api.Data;
using Forge.Api.Data.Entities;
using Forge.Api.Features.Events;
using Microsoft.EntityFrameworkCore;

namespace Forge.Api.Features.Repositories;

public interface IRepositoryService
{
    Task<IReadOnlyList<RepositoryDto>> GetAllAsync();
    Task<RepositoryDto?> GetByIdAsync(Guid id);
    Task<RepositoryDto> CreateAsync(CreateRepositoryDto dto);
    Task<RepositoryDto?> UpdateAsync(Guid id, UpdateRepositoryDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<RepositoryDto?> RefreshAsync(Guid id);
    Task<RepositoryDto?> SetDefaultAsync(Guid id);
    Task<RepositoryDto?> GetDefaultAsync();
}

public class RepositoryService(ForgeDbContext db, ISseService sse, ILogger<RepositoryService> logger) : IRepositoryService
{
    public async Task<IReadOnlyList<RepositoryDto>> GetAllAsync()
    {
        var entities = await db.Repositories
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.IsDefault)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();

        // Get task counts for each repository
        var taskCounts = await db.Tasks
            .Where(t => t.Repository.IsActive)
            .GroupBy(t => t.RepositoryId)
            .Select(g => new { RepositoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RepositoryId, x => x.Count);

        return entities.Select(e => RepositoryDto.FromEntity(e, taskCounts.GetValueOrDefault(e.Id, 0))).ToList();
    }

    public async Task<RepositoryDto?> GetByIdAsync(Guid id)
    {
        var entity = await db.Repositories
            .Include(r => r.Tasks)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

        if (entity is null) return null;

        // Refresh git info if stale (older than 5 minutes)
        if (entity.LastRefreshedAt is null || DateTime.UtcNow - entity.LastRefreshedAt > TimeSpan.FromMinutes(5))
        {
            RefreshGitInfo(entity);
            await db.SaveChangesAsync();
        }

        return RepositoryDto.FromEntity(entity, entity.Tasks.Count);
    }

    public async Task<RepositoryDto> CreateAsync(CreateRepositoryDto dto)
    {
        // Normalize path
        var normalizedPath = Path.GetFullPath(dto.Path);

        // Check if path already exists
        var existing = await db.Repositories
            .FirstOrDefaultAsync(r => r.Path == normalizedPath);

        if (existing is not null)
        {
            if (existing.IsActive)
            {
                throw new InvalidOperationException($"Repository with path '{normalizedPath}' already exists.");
            }
            // Reactivate soft-deleted repository
            existing.IsActive = true;
            existing.Name = dto.Name;
            existing.UpdatedAt = DateTime.UtcNow;
            RefreshGitInfo(existing);

            if (dto.SetAsDefault)
            {
                await ClearDefaultAsync();
                existing.IsDefault = true;
            }

            await db.SaveChangesAsync();

            var reactivatedDto = RepositoryDto.FromEntity(existing, 0);
            await sse.EmitRepositoryCreatedAsync(reactivatedDto);
            return reactivatedDto;
        }

        // Verify path exists and is a directory
        if (!Directory.Exists(normalizedPath))
        {
            throw new InvalidOperationException($"Directory '{normalizedPath}' does not exist.");
        }

        var entity = new RepositoryEntity
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Path = normalizedPath,
            IsDefault = dto.SetAsDefault,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Refresh git info
        RefreshGitInfo(entity);

        if (dto.SetAsDefault)
        {
            await ClearDefaultAsync();
        }

        db.Repositories.Add(entity);
        await db.SaveChangesAsync();

        var result = RepositoryDto.FromEntity(entity, 0);
        await sse.EmitRepositoryCreatedAsync(result);
        return result;
    }

    public async Task<RepositoryDto?> UpdateAsync(Guid id, UpdateRepositoryDto dto)
    {
        var entity = await db.Repositories
            .Include(r => r.Tasks)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

        if (entity is null) return null;

        if (dto.Name is not null)
        {
            entity.Name = dto.Name;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var result = RepositoryDto.FromEntity(entity, entity.Tasks.Count);
        await sse.EmitRepositoryUpdatedAsync(result);
        return result;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await db.Repositories
            .Include(r => r.Tasks)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

        if (entity is null) return false;

        // Check if there are any tasks
        if (entity.Tasks.Count > 0)
        {
            throw new InvalidOperationException(
                $"Cannot delete repository with {entity.Tasks.Count} task(s). Delete tasks first or move them to another repository.");
        }

        // Soft delete
        entity.IsActive = false;
        entity.IsDefault = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await sse.EmitRepositoryDeletedAsync(id);
        return true;
    }

    public async Task<RepositoryDto?> RefreshAsync(Guid id)
    {
        var entity = await db.Repositories
            .Include(r => r.Tasks)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

        if (entity is null) return null;

        RefreshGitInfo(entity);
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var result = RepositoryDto.FromEntity(entity, entity.Tasks.Count);
        await sse.EmitRepositoryUpdatedAsync(result);
        return result;
    }

    public async Task<RepositoryDto?> SetDefaultAsync(Guid id)
    {
        var entity = await db.Repositories
            .Include(r => r.Tasks)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

        if (entity is null) return null;

        // Clear any existing default
        await ClearDefaultAsync();

        entity.IsDefault = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var result = RepositoryDto.FromEntity(entity, entity.Tasks.Count);
        await sse.EmitRepositoryUpdatedAsync(result);
        return result;
    }

    public async Task<RepositoryDto?> GetDefaultAsync()
    {
        var entity = await db.Repositories
            .Include(r => r.Tasks)
            .FirstOrDefaultAsync(r => r.IsDefault && r.IsActive);

        return entity is null ? null : RepositoryDto.FromEntity(entity, entity.Tasks.Count);
    }

    private async Task ClearDefaultAsync()
    {
        var currentDefault = await db.Repositories
            .FirstOrDefaultAsync(r => r.IsDefault && r.IsActive);

        if (currentDefault is not null)
        {
            currentDefault.IsDefault = false;
            currentDefault.UpdatedAt = DateTime.UtcNow;
        }
    }

    private void RefreshGitInfo(RepositoryEntity entity)
    {
        var gitDir = Path.Combine(entity.Path, ".git");
        entity.IsGitRepository = Directory.Exists(gitDir);

        if (!entity.IsGitRepository)
        {
            entity.Branch = null;
            entity.CommitHash = null;
            entity.RemoteUrl = null;
            entity.IsDirty = null;
            entity.LastRefreshedAt = DateTime.UtcNow;
            return;
        }

        entity.Branch = RunGitCommand(entity.Path, "rev-parse --abbrev-ref HEAD");
        entity.CommitHash = RunGitCommand(entity.Path, "rev-parse --short HEAD");
        entity.RemoteUrl = RunGitCommand(entity.Path, "config --get remote.origin.url");
        var statusOutput = RunGitCommand(entity.Path, "status --porcelain");
        entity.IsDirty = !string.IsNullOrWhiteSpace(statusOutput);
        entity.LastRefreshedAt = DateTime.UtcNow;
    }

    private string? RunGitCommand(string workingDirectory, string arguments)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);

            if (process.ExitCode != 0)
            {
                logger.LogDebug("Git command '{Arguments}' exited with code {ExitCode}", arguments, process.ExitCode);
                return null;
            }

            return string.IsNullOrWhiteSpace(output) ? null : output;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to run git command: {Arguments}", arguments);
            return null;
        }
    }
}
