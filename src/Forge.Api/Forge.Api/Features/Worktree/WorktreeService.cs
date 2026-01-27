using System.Diagnostics;

namespace Forge.Api.Features.Worktree;

/// <summary>
/// Service for managing git worktrees for subtask isolation.
/// </summary>
public interface IWorktreeService
{
    /// <summary>
    /// Creates a new git worktree for a subtask.
    /// </summary>
    /// <param name="repositoryPath">Path to the main repository</param>
    /// <param name="subtaskId">ID of the subtask</param>
    /// <returns>Path to the created worktree</returns>
    Task<WorktreeResult> CreateWorktreeAsync(string repositoryPath, Guid subtaskId, CancellationToken ct = default);

    /// <summary>
    /// Removes a git worktree and its branch.
    /// </summary>
    Task<WorktreeResult> RemoveWorktreeAsync(string repositoryPath, Guid subtaskId, CancellationToken ct = default);

    /// <summary>
    /// Gets the worktree path for a subtask.
    /// </summary>
    string GetWorktreePath(string repositoryPath, Guid subtaskId);

    /// <summary>
    /// Gets the branch name for a subtask.
    /// </summary>
    string GetBranchName(Guid subtaskId);

    /// <summary>
    /// Checks if a worktree exists for a subtask.
    /// </summary>
    Task<bool> WorktreeExistsAsync(string repositoryPath, Guid subtaskId, CancellationToken ct = default);

    /// <summary>
    /// Gets the current commit SHA of a worktree.
    /// </summary>
    Task<string?> GetCurrentCommitAsync(string worktreePath, CancellationToken ct = default);

    /// <summary>
    /// Gets the list of changed files in a worktree.
    /// </summary>
    Task<IReadOnlyList<string>> GetChangedFilesAsync(string worktreePath, CancellationToken ct = default);

    /// <summary>
    /// Reverts all uncommitted changes in a worktree.
    /// </summary>
    Task<WorktreeResult> RevertChangesAsync(string worktreePath, CancellationToken ct = default);
}

/// <summary>
/// Result of a worktree operation.
/// </summary>
public record WorktreeResult(
    bool Success,
    string? Path,
    string? BranchName,
    string? Error
);

/// <summary>
/// Implementation of IWorktreeService.
/// </summary>
public class WorktreeService : IWorktreeService
{
    private readonly ILogger<WorktreeService> _logger;
    private const string WorktreePrefix = "worktree-subtask-";
    private const string BranchPrefix = "subtask/";

    public WorktreeService(ILogger<WorktreeService> logger)
    {
        _logger = logger;
    }

    public async Task<WorktreeResult> CreateWorktreeAsync(string repositoryPath, Guid subtaskId, CancellationToken ct = default)
    {
        var worktreePath = GetWorktreePath(repositoryPath, subtaskId);
        var branchName = GetBranchName(subtaskId);

        try
        {
            // Check if worktree already exists
            if (Directory.Exists(worktreePath))
            {
                _logger.LogWarning("Worktree already exists at {Path}", worktreePath);
                return new WorktreeResult(true, worktreePath, branchName, null);
            }

            // Create the worktree with a new branch
            var createResult = await RunGitCommandAsync(
                repositoryPath,
                $"worktree add \"{worktreePath}\" -b {branchName}",
                ct);

            if (!createResult.Success)
            {
                _logger.LogError("Failed to create worktree: {Error}", createResult.Error);
                return new WorktreeResult(false, null, null, createResult.Error);
            }

            _logger.LogInformation("Created worktree at {Path} with branch {Branch}", worktreePath, branchName);
            return new WorktreeResult(true, worktreePath, branchName, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception creating worktree for subtask {SubtaskId}", subtaskId);
            return new WorktreeResult(false, null, null, ex.Message);
        }
    }

    public async Task<WorktreeResult> RemoveWorktreeAsync(string repositoryPath, Guid subtaskId, CancellationToken ct = default)
    {
        var worktreePath = GetWorktreePath(repositoryPath, subtaskId);
        var branchName = GetBranchName(subtaskId);

        try
        {
            // Remove the worktree
            if (Directory.Exists(worktreePath))
            {
                var removeResult = await RunGitCommandAsync(
                    repositoryPath,
                    $"worktree remove \"{worktreePath}\" --force",
                    ct);

                if (!removeResult.Success)
                {
                    _logger.LogWarning("Failed to remove worktree via git: {Error}. Trying manual removal.", removeResult.Error);
                    // Try manual removal as fallback
                    try
                    {
                        Directory.Delete(worktreePath, recursive: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to manually remove worktree directory");
                    }
                }
            }

            // Delete the branch
            var branchResult = await RunGitCommandAsync(
                repositoryPath,
                $"branch -D {branchName}",
                ct);

            if (!branchResult.Success)
            {
                _logger.LogWarning("Failed to delete branch {Branch}: {Error}", branchName, branchResult.Error);
                // Not a critical error - branch may already be deleted or merged
            }

            _logger.LogInformation("Removed worktree at {Path} and branch {Branch}", worktreePath, branchName);
            return new WorktreeResult(true, worktreePath, branchName, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception removing worktree for subtask {SubtaskId}", subtaskId);
            return new WorktreeResult(false, null, null, ex.Message);
        }
    }

    public string GetWorktreePath(string repositoryPath, Guid subtaskId)
    {
        var repoParent = Directory.GetParent(repositoryPath)?.FullName ?? repositoryPath;
        return Path.Combine(repoParent, $"{WorktreePrefix}{subtaskId:N}");
    }

    public string GetBranchName(Guid subtaskId)
    {
        return $"{BranchPrefix}{subtaskId:N}";
    }

    public async Task<bool> WorktreeExistsAsync(string repositoryPath, Guid subtaskId, CancellationToken ct = default)
    {
        var worktreePath = GetWorktreePath(repositoryPath, subtaskId);

        if (!Directory.Exists(worktreePath))
            return false;

        // Verify it's a valid git worktree
        var result = await RunGitCommandAsync(repositoryPath, "worktree list --porcelain", ct);
        return result.Success && result.Output?.Contains(worktreePath, StringComparison.OrdinalIgnoreCase) == true;
    }

    public async Task<string?> GetCurrentCommitAsync(string worktreePath, CancellationToken ct = default)
    {
        var result = await RunGitCommandAsync(worktreePath, "rev-parse HEAD", ct);
        return result.Success ? result.Output?.Trim() : null;
    }

    public async Task<IReadOnlyList<string>> GetChangedFilesAsync(string worktreePath, CancellationToken ct = default)
    {
        var result = await RunGitCommandAsync(worktreePath, "diff --name-only HEAD", ct);
        if (!result.Success || string.IsNullOrEmpty(result.Output))
            return [];

        return result.Output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    public async Task<WorktreeResult> RevertChangesAsync(string worktreePath, CancellationToken ct = default)
    {
        try
        {
            // Reset all changes
            var resetResult = await RunGitCommandAsync(worktreePath, "checkout -- .", ct);
            if (!resetResult.Success)
            {
                return new WorktreeResult(false, worktreePath, null, resetResult.Error);
            }

            // Clean untracked files
            var cleanResult = await RunGitCommandAsync(worktreePath, "clean -fd", ct);
            if (!cleanResult.Success)
            {
                _logger.LogWarning("Failed to clean untracked files: {Error}", cleanResult.Error);
            }

            return new WorktreeResult(true, worktreePath, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception reverting changes in worktree {Path}", worktreePath);
            return new WorktreeResult(false, worktreePath, null, ex.Message);
        }
    }

    private async Task<GitCommandResult> RunGitCommandAsync(string workingDirectory, string arguments, CancellationToken ct)
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

            var outputTask = process.StandardOutput.ReadToEndAsync(ct);
            var errorTask = process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                _logger.LogDebug("Git command failed: git {Args} -> {Error}", arguments, error);
                return new GitCommandResult(false, output, error);
            }

            return new GitCommandResult(true, output, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception running git command: {Args}", arguments);
            return new GitCommandResult(false, null, ex.Message);
        }
    }

    private record GitCommandResult(bool Success, string? Output, string? Error);
}
