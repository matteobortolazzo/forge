namespace Forge.Api.Features.Agents;

/// <summary>
/// Detects repository context (language, framework) for agent selection.
/// </summary>
public interface IContextDetector
{
    /// <summary>
    /// Detects the primary programming language in the repository.
    /// </summary>
    Task<string?> DetectLanguageAsync(string repositoryPath);

    /// <summary>
    /// Detects the primary framework used in the repository.
    /// </summary>
    Task<string?> DetectFrameworkAsync(string repositoryPath);

    /// <summary>
    /// Checks if any of the specified file patterns exist in the repository.
    /// </summary>
    Task<bool> FilesPresentAsync(string repositoryPath, IEnumerable<string> patterns);
}

/// <summary>
/// Implementation of IContextDetector that scans the repository file system.
/// </summary>
public class ContextDetector : IContextDetector
{
    private readonly ILogger<ContextDetector> _logger;

    // Framework indicators (checked in order of specificity)
    private static readonly (string Framework, string[] Indicators)[] FrameworkIndicators =
    [
        ("angular", ["angular.json"]),
        ("react", ["package.json:react", "next.config.js", "next.config.ts"]),
        ("vue", ["vue.config.js", "nuxt.config.js", "package.json:vue"]),
        ("dotnet", ["*.csproj", "*.sln", "Program.cs"]),
        ("django", ["manage.py", "settings.py"]),
        ("rails", ["Gemfile", "config/routes.rb"]),
        ("spring", ["pom.xml:spring", "build.gradle:spring"]),
        ("express", ["package.json:express"]),
        ("fastapi", ["main.py:fastapi"]),
    ];

    // Language indicators by file extension (most specific first)
    private static readonly (string Language, string[] Extensions)[] LanguageIndicators =
    [
        ("typescript", [".ts", ".tsx"]),
        ("javascript", [".js", ".jsx", ".mjs"]),
        ("csharp", [".cs"]),
        ("python", [".py"]),
        ("java", [".java"]),
        ("go", [".go"]),
        ("rust", [".rs"]),
        ("ruby", [".rb"]),
        ("php", [".php"]),
    ];

    public ContextDetector(ILogger<ContextDetector> logger)
    {
        _logger = logger;
    }

    public async Task<string?> DetectLanguageAsync(string repositoryPath)
    {
        if (!Directory.Exists(repositoryPath))
        {
            _logger.LogWarning("Repository path does not exist: {Path}", repositoryPath);
            return null;
        }

        return await Task.Run(() =>
        {
            var extensionCounts = new Dictionary<string, int>();

            try
            {
                // Count files by extension in src directories (more relevant than all files)
                var searchPaths = new[] { "src", "lib", "app", "." };
                foreach (var searchPath in searchPaths)
                {
                    var fullPath = Path.Combine(repositoryPath, searchPath);
                    if (!Directory.Exists(fullPath)) continue;

                    foreach (var file in Directory.EnumerateFiles(fullPath, "*.*", SearchOption.AllDirectories))
                    {
                        // Skip common non-code directories
                        if (file.Contains("/node_modules/") ||
                            file.Contains("/bin/") ||
                            file.Contains("/obj/") ||
                            file.Contains("/.git/") ||
                            file.Contains("/dist/") ||
                            file.Contains("/build/"))
                        {
                            continue;
                        }

                        var ext = Path.GetExtension(file).ToLowerInvariant();
                        if (!string.IsNullOrEmpty(ext))
                        {
                            extensionCounts[ext] = extensionCounts.GetValueOrDefault(ext, 0) + 1;
                        }
                    }

                    // If we found files in src, that's enough
                    if (extensionCounts.Count > 0 && searchPath == "src")
                        break;
                }

                // Find the most common language based on extension counts
                foreach (var (language, extensions) in LanguageIndicators)
                {
                    var count = extensions.Sum(ext => extensionCounts.GetValueOrDefault(ext, 0));
                    if (count >= 3) // Need at least 3 files to be confident
                    {
                        _logger.LogDebug("Detected language: {Language} with {Count} files", language, count);
                        return language;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting language in {Path}", repositoryPath);
            }

            return null;
        });
    }

    public async Task<string?> DetectFrameworkAsync(string repositoryPath)
    {
        if (!Directory.Exists(repositoryPath))
        {
            _logger.LogWarning("Repository path does not exist: {Path}", repositoryPath);
            return null;
        }

        return await Task.Run(() =>
        {
            try
            {
                foreach (var (framework, indicators) in FrameworkIndicators)
                {
                    foreach (var indicator in indicators)
                    {
                        // Handle file:content format (e.g., "package.json:react")
                        var parts = indicator.Split(':');
                        var pattern = parts[0];
                        var contentMatch = parts.Length > 1 ? parts[1] : null;

                        // Check for file existence with glob pattern
                        var files = pattern.Contains('*')
                            ? Directory.GetFiles(repositoryPath, pattern, SearchOption.TopDirectoryOnly)
                            : File.Exists(Path.Combine(repositoryPath, pattern))
                                ? [Path.Combine(repositoryPath, pattern)]
                                : Array.Empty<string>();

                        if (files.Length == 0) continue;

                        // If no content match required, we found it
                        if (contentMatch == null)
                        {
                            _logger.LogDebug("Detected framework: {Framework} via {File}", framework, pattern);
                            return framework;
                        }

                        // Check file content for the match string
                        foreach (var file in files)
                        {
                            try
                            {
                                var content = File.ReadAllText(file);
                                if (content.Contains(contentMatch, StringComparison.OrdinalIgnoreCase))
                                {
                                    _logger.LogDebug("Detected framework: {Framework} via {File} containing {Match}",
                                        framework, pattern, contentMatch);
                                    return framework;
                                }
                            }
                            catch
                            {
                                // Ignore file read errors
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting framework in {Path}", repositoryPath);
            }

            return null;
        });
    }

    public async Task<bool> FilesPresentAsync(string repositoryPath, IEnumerable<string> patterns)
    {
        if (!Directory.Exists(repositoryPath))
            return false;

        return await Task.Run(() =>
        {
            foreach (var pattern in patterns)
            {
                try
                {
                    if (pattern.Contains('*'))
                    {
                        // Glob pattern
                        if (Directory.GetFiles(repositoryPath, pattern, SearchOption.AllDirectories).Length > 0)
                            return true;
                    }
                    else
                    {
                        // Exact file
                        if (File.Exists(Path.Combine(repositoryPath, pattern)))
                            return true;
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }

            return false;
        });
    }
}
