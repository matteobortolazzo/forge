using Forge.Api.Shared;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Forge.Api.Features.Agents;

/// <summary>
/// Loads and caches agent configurations from YAML files.
/// </summary>
public interface IAgentConfigLoader
{
    /// <summary>
    /// Gets all default agent configurations.
    /// </summary>
    IReadOnlyList<AgentConfig> GetDefaultConfigs();

    /// <summary>
    /// Gets all variant agent configurations.
    /// </summary>
    IReadOnlyList<AgentConfig> GetVariantConfigs();

    /// <summary>
    /// Gets the default agent configuration for a specific task state.
    /// </summary>
    AgentConfig? GetDefaultForTaskState(PipelineState state);

    /// <summary>
    /// Gets the default agent configuration for a specific backlog item state.
    /// </summary>
    AgentConfig? GetDefaultForBacklogState(BacklogItemState state);

    /// <summary>
    /// Gets all variants for a specific task state.
    /// </summary>
    IReadOnlyList<AgentConfig> GetVariantsForTaskState(PipelineState state);

    /// <summary>
    /// Gets all variants for a specific backlog item state.
    /// </summary>
    IReadOnlyList<AgentConfig> GetVariantsForBacklogState(BacklogItemState state);

    /// <summary>
    /// Reloads all configurations from disk.
    /// </summary>
    void Reload();
}

/// <summary>
/// Implementation of IAgentConfigLoader that loads from file system.
/// </summary>
public class AgentConfigLoader : IAgentConfigLoader
{
    private readonly string _agentsBasePath;
    private readonly ILogger<AgentConfigLoader> _logger;
    private readonly IDeserializer _deserializer;
    private readonly object _lock = new();

    private List<AgentConfig> _defaultConfigs = [];
    private List<AgentConfig> _variantConfigs = [];
    private bool _loaded;

    public AgentConfigLoader(IConfiguration configuration, ILogger<AgentConfigLoader> logger)
    {
        // Default to agents/ directory relative to content root, can be overridden
        var contentRoot = configuration["CONTENT_ROOT"] ?? Directory.GetCurrentDirectory();
        _agentsBasePath = configuration["AGENTS_PATH"] ?? Path.Combine(contentRoot, "..", "..", "..", "agents");
        _logger = logger;

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public IReadOnlyList<AgentConfig> GetDefaultConfigs()
    {
        EnsureLoaded();
        return _defaultConfigs.AsReadOnly();
    }

    public IReadOnlyList<AgentConfig> GetVariantConfigs()
    {
        EnsureLoaded();
        return _variantConfigs.AsReadOnly();
    }

    public AgentConfig? GetDefaultForTaskState(PipelineState state)
    {
        EnsureLoaded();
        return _defaultConfigs.FirstOrDefault(c => c.TaskState == state);
    }

    public AgentConfig? GetDefaultForBacklogState(BacklogItemState state)
    {
        EnsureLoaded();
        return _defaultConfigs.FirstOrDefault(c => c.BacklogState == state);
    }

    public IReadOnlyList<AgentConfig> GetVariantsForTaskState(PipelineState state)
    {
        EnsureLoaded();
        return _variantConfigs.Where(c => c.TaskState == state).ToList().AsReadOnly();
    }

    public IReadOnlyList<AgentConfig> GetVariantsForBacklogState(BacklogItemState state)
    {
        EnsureLoaded();
        return _variantConfigs.Where(c => c.BacklogState == state).ToList().AsReadOnly();
    }

    public void Reload()
    {
        lock (_lock)
        {
            _defaultConfigs.Clear();
            _variantConfigs.Clear();
            _loaded = false;
            EnsureLoaded();
        }
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;

        lock (_lock)
        {
            if (_loaded) return;

            LoadConfigs();
            _loaded = true;
        }
    }

    private void LoadConfigs()
    {
        var defaultsPath = Path.Combine(_agentsBasePath, "defaults");
        var variantsPath = Path.Combine(_agentsBasePath, "variants");

        _logger.LogInformation("Loading agent configs from {BasePath}", _agentsBasePath);

        // Load defaults
        if (Directory.Exists(defaultsPath))
        {
            foreach (var file in Directory.GetFiles(defaultsPath, "*.yml"))
            {
                try
                {
                    var config = LoadConfigFromFile(file);
                    if (config != null)
                    {
                        _defaultConfigs.Add(config);
                        _logger.LogDebug("Loaded default agent config: {Id} from {File}", config.Id, file);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load agent config from {File}", file);
                }
            }
        }
        else
        {
            _logger.LogWarning("Default agents directory not found: {Path}", defaultsPath);
        }

        // Load variants
        if (Directory.Exists(variantsPath))
        {
            foreach (var file in Directory.GetFiles(variantsPath, "*.yml"))
            {
                try
                {
                    var config = LoadConfigFromFile(file);
                    if (config != null)
                    {
                        _variantConfigs.Add(config);
                        _logger.LogDebug("Loaded variant agent config: {Id} from {File}", config.Id, file);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load agent config from {File}", file);
                }
            }
        }
        else
        {
            _logger.LogDebug("Variant agents directory not found: {Path}", variantsPath);
        }

        _logger.LogInformation(
            "Loaded {DefaultCount} default and {VariantCount} variant agent configs",
            _defaultConfigs.Count,
            _variantConfigs.Count);
    }

    private AgentConfig? LoadConfigFromFile(string filePath)
    {
        var yaml = File.ReadAllText(filePath);
        var config = _deserializer.Deserialize<AgentConfig>(yaml);

        if (config == null)
        {
            _logger.LogWarning("Empty or invalid config in {File}", filePath);
            return null;
        }

        config.SourceFile = filePath;
        return config;
    }
}
