using System.Runtime.InteropServices;
using Claude.CodeSdk.Exceptions;

namespace Claude.CodeSdk.Internal;

/// <summary>
/// Locates the Claude Code CLI executable on the system.
/// </summary>
internal static class CliLocator
{
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Finds the CLI executable path.
    /// </summary>
    /// <param name="customPath">Optional custom path to check first.</param>
    /// <returns>The path to the CLI executable.</returns>
    /// <exception cref="CliNotFoundException">Thrown when the CLI cannot be found.</exception>
    public static string FindCli(string? customPath = null)
    {
        var searchedPaths = new List<string>();

        // 1. Check custom path first
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            searchedPaths.Add(customPath);
            if (File.Exists(customPath))
            {
                return customPath;
            }
        }

        // 2. Search in PATH
        var pathExecutable = FindInPath();
        if (pathExecutable is not null)
        {
            return pathExecutable;
        }
        searchedPaths.Add("PATH environment variable");

        // 3. Check common installation locations
        var commonPaths = GetCommonPaths();
        foreach (var path in commonPaths)
        {
            searchedPaths.Add(path);
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new CliNotFoundException(searchedPaths);
    }

    private static string? FindInPath()
    {
        var executableName = IsWindows ? "claude.cmd" : "claude";
        var pathEnv = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrEmpty(pathEnv))
        {
            return null;
        }

        var separator = IsWindows ? ';' : ':';
        var paths = pathEnv.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var dir in paths)
        {
            var fullPath = Path.Combine(dir, executableName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            // Also try without extension on Windows (direct executable)
            if (IsWindows)
            {
                var exePath = Path.Combine(dir, "claude.exe");
                if (File.Exists(exePath))
                {
                    return exePath;
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> GetCommonPaths()
    {
        if (IsWindows)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            yield return Path.Combine(appData, "npm", "claude.cmd");
            yield return Path.Combine(appData, "npm", "claude.exe");

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            yield return Path.Combine(localAppData, "npm", "claude.cmd");
            yield return Path.Combine(localAppData, "npm", "claude.exe");

            // Common global npm location
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            yield return Path.Combine(programFiles, "nodejs", "claude.cmd");
        }
        else
        {
            // Unix-like systems
            yield return "/usr/local/bin/claude";
            yield return "/usr/bin/claude";
            yield return "/opt/homebrew/bin/claude";

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(home, ".npm-global", "bin", "claude");
            yield return Path.Combine(home, ".local", "bin", "claude");
            yield return Path.Combine(home, ".nvm", "versions", "node", "current", "bin", "claude");

            // Common nvm paths (check for any version)
            var nvmDir = Path.Combine(home, ".nvm", "versions", "node");
            if (Directory.Exists(nvmDir))
            {
                foreach (var versionDir in Directory.EnumerateDirectories(nvmDir))
                {
                    yield return Path.Combine(versionDir, "bin", "claude");
                }
            }
        }
    }
}
