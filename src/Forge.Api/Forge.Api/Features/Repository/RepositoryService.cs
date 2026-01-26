using System.Diagnostics;

namespace Forge.Api.Features.Repository;

public class RepositoryService(IConfiguration configuration, ILogger<RepositoryService> logger)
{
    public RepositoryInfoDto GetRepositoryInfo()
    {
        var repoPath = configuration["REPOSITORY_PATH"] ?? Environment.CurrentDirectory;
        var name = System.IO.Path.GetFileName(repoPath) ?? "unknown";
        var gitDir = System.IO.Path.Combine(repoPath, ".git");
        var isGitRepo = Directory.Exists(gitDir);

        if (!isGitRepo)
        {
            return new RepositoryInfoDto(
                Name: name,
                Path: repoPath,
                Branch: null,
                CommitHash: null,
                RemoteUrl: null,
                IsDirty: false,
                IsGitRepository: false);
        }

        var branch = RunGitCommand(repoPath, "rev-parse --abbrev-ref HEAD");
        var commitHash = RunGitCommand(repoPath, "rev-parse --short HEAD");
        var remoteUrl = RunGitCommand(repoPath, "config --get remote.origin.url");
        var statusOutput = RunGitCommand(repoPath, "status --porcelain");
        var isDirty = !string.IsNullOrWhiteSpace(statusOutput);

        return new RepositoryInfoDto(
            Name: name,
            Path: repoPath,
            Branch: branch,
            CommitHash: commitHash,
            RemoteUrl: remoteUrl,
            IsDirty: isDirty,
            IsGitRepository: true);
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
