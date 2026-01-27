using Forge.Api.Data;
using Forge.Api.Data.Entities;
using Forge.Api.Features.Events;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace Forge.Api.Features.Agents;

/// <summary>
/// Orchestrates agent selection, prompt assembly, and artifact management.
/// </summary>
public interface IOrchestratorService
{
    /// <summary>
    /// Selects the best agent configuration for a task based on state and context.
    /// </summary>
    Task<ResolvedAgentConfig> SelectAgentAsync(TaskEntity task, string repositoryPath);

    /// <summary>
    /// Selects the best agent configuration for a subtask.
    /// </summary>
    Task<ResolvedAgentConfig> SelectAgentForSubtaskAsync(SubtaskEntity subtask, TaskEntity parentTask, string repositoryPath);

    /// <summary>
    /// Gets all artifacts for a task.
    /// </summary>
    Task<IReadOnlyList<AgentArtifactEntity>> GetArtifactsAsync(Guid taskId);

    /// <summary>
    /// Gets all artifacts for a subtask.
    /// </summary>
    Task<IReadOnlyList<AgentArtifactEntity>> GetSubtaskArtifactsAsync(Guid subtaskId);

    /// <summary>
    /// Stores an artifact produced by an agent.
    /// </summary>
    Task<AgentArtifactEntity> StoreArtifactAsync(
        Guid taskId,
        PipelineState producedInState,
        ArtifactType artifactType,
        string content,
        string? agentId,
        Guid? subtaskId = null,
        decimal? confidenceScore = null,
        bool humanInputRequested = false,
        string? humanInputReason = null);

    /// <summary>
    /// Updates the task with detected context and recommended state.
    /// </summary>
    Task UpdateTaskContextAsync(Guid taskId, string? language, string? framework, PipelineState? recommendedState);

    /// <summary>
    /// Updates task confidence and human input tracking.
    /// </summary>
    Task UpdateTaskConfidenceAsync(Guid taskId, decimal? confidenceScore, bool humanInputRequested, string? humanInputReason);
}

/// <summary>
/// Implementation of IOrchestratorService.
/// </summary>
public class OrchestratorService : IOrchestratorService
{
    private readonly IAgentConfigLoader _configLoader;
    private readonly IContextDetector _contextDetector;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISseService _sseService;
    private readonly ILogger<OrchestratorService> _logger;

    public OrchestratorService(
        IAgentConfigLoader configLoader,
        IContextDetector contextDetector,
        IPromptBuilder promptBuilder,
        IServiceScopeFactory scopeFactory,
        ISseService sseService,
        ILogger<OrchestratorService> logger)
    {
        _configLoader = configLoader;
        _contextDetector = contextDetector;
        _promptBuilder = promptBuilder;
        _scopeFactory = scopeFactory;
        _sseService = sseService;
        _logger = logger;
    }

    public async Task<ResolvedAgentConfig> SelectAgentAsync(TaskEntity task, string repositoryPath)
    {
        // 1. Get the default agent for this state
        var defaultConfig = _configLoader.GetDefaultForState(task.State);
        if (defaultConfig == null)
        {
            throw new InvalidOperationException($"No default agent configuration found for state: {task.State}");
        }

        // 2. Detect context if not already set on the task
        var language = task.DetectedLanguage;
        var framework = task.DetectedFramework;

        if (string.IsNullOrEmpty(language) || string.IsNullOrEmpty(framework))
        {
            if (string.IsNullOrEmpty(language))
            {
                language = await _contextDetector.DetectLanguageAsync(repositoryPath);
            }
            if (string.IsNullOrEmpty(framework))
            {
                framework = await _contextDetector.DetectFrameworkAsync(repositoryPath);
            }

            _logger.LogInformation(
                "Detected context for task {TaskId}: language={Language}, framework={Framework}",
                task.Id, language ?? "unknown", framework ?? "unknown");
        }

        // 3. Try to find a matching variant
        var selectedConfig = await SelectVariantAsync(task.State, language, framework, repositoryPath)
                            ?? defaultConfig;

        _logger.LogInformation(
            "Selected agent {AgentId} for task {TaskId} in state {State}",
            selectedConfig.Id, task.Id, task.State);

        // 4. Get artifacts from previous stages
        var artifacts = await GetArtifactsAsync(task.Id);

        // 5. Build the prompt
        var resolvedPrompt = _promptBuilder.BuildPrompt(selectedConfig.Prompt, task, artifacts);

        // 6. Merge MCP servers from variant and default
        var mcpServers = selectedConfig.McpServers?.ToList() ?? [];
        if (selectedConfig.IsVariant && defaultConfig.McpServers != null)
        {
            foreach (var server in defaultConfig.McpServers)
            {
                if (!mcpServers.Contains(server))
                {
                    mcpServers.Add(server);
                }
            }
        }

        // 7. Determine artifact type
        var artifactType = DetermineArtifactType(selectedConfig);

        return new ResolvedAgentConfig
        {
            Config = selectedConfig,
            ResolvedPrompt = resolvedPrompt,
            McpServers = mcpServers,
            MaxTurns = selectedConfig.MaxTurns,
            ExpectedArtifactType = artifactType
        };
    }

    public async Task<IReadOnlyList<AgentArtifactEntity>> GetArtifactsAsync(Guid taskId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ForgeDbContext>();

        return await db.AgentArtifacts
            .Where(a => a.TaskId == taskId && a.SubtaskId == null)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AgentArtifactEntity>> GetSubtaskArtifactsAsync(Guid subtaskId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ForgeDbContext>();

        return await db.AgentArtifacts
            .Where(a => a.SubtaskId == subtaskId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<ResolvedAgentConfig> SelectAgentForSubtaskAsync(SubtaskEntity subtask, TaskEntity parentTask, string repositoryPath)
    {
        // For subtasks, use the subtask's current stage to select the agent
        var defaultConfig = _configLoader.GetDefaultForState(subtask.CurrentStage);
        if (defaultConfig == null)
        {
            throw new InvalidOperationException($"No default agent configuration found for state: {subtask.CurrentStage}");
        }

        // Use parent task's detected context
        var language = parentTask.DetectedLanguage;
        var framework = parentTask.DetectedFramework;

        if (string.IsNullOrEmpty(language) || string.IsNullOrEmpty(framework))
        {
            if (string.IsNullOrEmpty(language))
            {
                language = await _contextDetector.DetectLanguageAsync(repositoryPath);
            }
            if (string.IsNullOrEmpty(framework))
            {
                framework = await _contextDetector.DetectFrameworkAsync(repositoryPath);
            }
        }

        // Try to find a matching variant
        var selectedConfig = await SelectVariantAsync(subtask.CurrentStage, language, framework, repositoryPath)
                            ?? defaultConfig;

        _logger.LogInformation(
            "Selected agent {AgentId} for subtask {SubtaskId} in stage {Stage}",
            selectedConfig.Id, subtask.Id, subtask.CurrentStage);

        // Get artifacts from parent task and this subtask
        var taskArtifacts = await GetArtifactsAsync(parentTask.Id);
        var subtaskArtifacts = await GetSubtaskArtifactsAsync(subtask.Id);
        var allArtifacts = taskArtifacts.Concat(subtaskArtifacts).OrderBy(a => a.CreatedAt).ToList();

        // Build prompt with subtask context
        var resolvedPrompt = _promptBuilder.BuildPrompt(selectedConfig.Prompt, parentTask, subtask, allArtifacts, repositoryPath);

        // Merge MCP servers
        var mcpServers = selectedConfig.McpServers?.ToList() ?? [];
        if (selectedConfig.IsVariant && defaultConfig.McpServers != null)
        {
            foreach (var server in defaultConfig.McpServers)
            {
                if (!mcpServers.Contains(server))
                {
                    mcpServers.Add(server);
                }
            }
        }

        var artifactType = DetermineArtifactType(selectedConfig);

        return new ResolvedAgentConfig
        {
            Config = selectedConfig,
            ResolvedPrompt = resolvedPrompt,
            McpServers = mcpServers,
            MaxTurns = selectedConfig.MaxTurns,
            ExpectedArtifactType = artifactType
        };
    }

    public async Task<AgentArtifactEntity> StoreArtifactAsync(
        Guid taskId,
        PipelineState producedInState,
        ArtifactType artifactType,
        string content,
        string? agentId,
        Guid? subtaskId = null,
        decimal? confidenceScore = null,
        bool humanInputRequested = false,
        string? humanInputReason = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ForgeDbContext>();

        var artifact = new AgentArtifactEntity
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ProducedInState = producedInState,
            ArtifactType = artifactType,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            AgentId = agentId,
            SubtaskId = subtaskId,
            ConfidenceScore = confidenceScore,
            HumanInputRequested = humanInputRequested,
            HumanInputReason = humanInputReason
        };

        db.AgentArtifacts.Add(artifact);
        await db.SaveChangesAsync();

        _logger.LogInformation(
            "Stored artifact {ArtifactId} of type {Type} for task {TaskId}, confidence: {Confidence}",
            artifact.Id, artifactType, taskId, confidenceScore);

        // Emit SSE event
        var artifactDto = new ArtifactDto(
            artifact.Id,
            artifact.TaskId,
            artifact.ProducedInState,
            artifact.ArtifactType,
            artifact.Content,
            artifact.CreatedAt,
            artifact.AgentId,
            artifact.ConfidenceScore
        );
        await _sseService.EmitArtifactCreatedAsync(artifactDto);

        return artifact;
    }

    public async Task UpdateTaskContextAsync(
        Guid taskId,
        string? language,
        string? framework,
        PipelineState? recommendedState)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ForgeDbContext>();

        var task = await db.Tasks.FindAsync(taskId);
        if (task == null)
        {
            _logger.LogWarning("Task {TaskId} not found for context update", taskId);
            return;
        }

        var changed = false;

        if (language != null && task.DetectedLanguage != language)
        {
            task.DetectedLanguage = language;
            changed = true;
        }

        if (framework != null && task.DetectedFramework != framework)
        {
            task.DetectedFramework = framework;
            changed = true;
        }

        if (recommendedState.HasValue && task.RecommendedNextState != recommendedState)
        {
            task.RecommendedNextState = recommendedState;
            changed = true;
        }

        if (changed)
        {
            task.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            _logger.LogDebug(
                "Updated task {TaskId} context: language={Language}, framework={Framework}, recommendedState={State}",
                taskId, language, framework, recommendedState);
        }
    }

    public async Task UpdateTaskConfidenceAsync(
        Guid taskId,
        decimal? confidenceScore,
        bool humanInputRequested,
        string? humanInputReason)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ForgeDbContext>();

        var task = await db.Tasks.FindAsync(taskId);
        if (task == null)
        {
            _logger.LogWarning("Task {TaskId} not found for confidence update", taskId);
            return;
        }

        task.ConfidenceScore = confidenceScore;
        task.HumanInputRequested = humanInputRequested;
        task.HumanInputReason = humanInputReason;
        task.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        _logger.LogDebug(
            "Updated task {TaskId} confidence: score={Score}, humanInputRequested={Requested}",
            taskId, confidenceScore, humanInputRequested);
    }

    private async Task<AgentConfig?> SelectVariantAsync(
        PipelineState state,
        string? language,
        string? framework,
        string repositoryPath)
    {
        var variants = _configLoader.GetVariantsForState(state);
        if (variants.Count == 0)
            return null;

        foreach (var variant in variants)
        {
            if (variant.Match == null)
                continue;

            // Check framework match
            if (!string.IsNullOrEmpty(variant.Match.Framework))
            {
                if (framework != null &&
                    framework.Equals(variant.Match.Framework, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Variant {VariantId} matched by framework: {Framework}",
                        variant.Id, framework);
                    return variant;
                }
            }

            // Check language match
            if (!string.IsNullOrEmpty(variant.Match.Language))
            {
                if (language != null &&
                    language.Equals(variant.Match.Language, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Variant {VariantId} matched by language: {Language}",
                        variant.Id, language);
                    return variant;
                }
            }

            // Check file patterns
            if (variant.Match.Files != null && variant.Match.Files.Count > 0)
            {
                if (await _contextDetector.FilesPresentAsync(repositoryPath, variant.Match.Files))
                {
                    _logger.LogDebug("Variant {VariantId} matched by file patterns", variant.Id);
                    return variant;
                }
            }
        }

        return null;
    }

    private static ArtifactType DetermineArtifactType(AgentConfig config)
    {
        if (config.Output?.Type != null)
        {
            return config.Output.Type.ToLowerInvariant() switch
            {
                "task_split" => ArtifactType.TaskSplit,
                "research_findings" => ArtifactType.ResearchFindings,
                "plan" => ArtifactType.Plan,
                "implementation" => ArtifactType.Implementation,
                "simplification_review" => ArtifactType.SimplificationReview,
                "verification_report" => ArtifactType.VerificationReport,
                "review" => ArtifactType.Review,
                "test" => ArtifactType.Test,
                _ => ArtifactType.General
            };
        }

        return config.State switch
        {
            PipelineState.Split => ArtifactType.TaskSplit,
            PipelineState.Research => ArtifactType.ResearchFindings,
            PipelineState.Planning => ArtifactType.Plan,
            PipelineState.Implementing => ArtifactType.Implementation,
            PipelineState.Simplifying => ArtifactType.SimplificationReview,
            PipelineState.Verifying => ArtifactType.VerificationReport,
            PipelineState.Reviewing => ArtifactType.Review,
            _ => ArtifactType.General
        };
    }
}
