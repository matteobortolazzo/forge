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
    Task<ResolvedAgentConfig> SelectAgentForTaskAsync(TaskEntity task, string repositoryPath);

    /// <summary>
    /// Selects the best agent configuration for a backlog item based on state and context.
    /// </summary>
    Task<ResolvedAgentConfig> SelectAgentForBacklogItemAsync(BacklogItemEntity backlogItem, string repositoryPath);

    /// <summary>
    /// Gets all artifacts for a task.
    /// </summary>
    Task<IReadOnlyList<AgentArtifactEntity>> GetTaskArtifactsAsync(Guid taskId);

    /// <summary>
    /// Gets all artifacts for a backlog item.
    /// </summary>
    Task<IReadOnlyList<AgentArtifactEntity>> GetBacklogArtifactsAsync(Guid backlogItemId);

    /// <summary>
    /// Stores an artifact produced by an agent for a task.
    /// </summary>
    Task<AgentArtifactEntity> StoreTaskArtifactAsync(
        Guid taskId,
        PipelineState producedInState,
        ArtifactType artifactType,
        string content,
        string? agentId,
        decimal? confidenceScore = null,
        bool humanInputRequested = false,
        string? humanInputReason = null);

    /// <summary>
    /// Stores an artifact produced by an agent for a backlog item.
    /// </summary>
    Task<AgentArtifactEntity> StoreBacklogArtifactAsync(
        Guid backlogItemId,
        BacklogItemState producedInState,
        ArtifactType artifactType,
        string content,
        string? agentId,
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

    public async Task<ResolvedAgentConfig> SelectAgentForTaskAsync(TaskEntity task, string repositoryPath)
    {
        // 1. Get the default agent for this state
        var defaultConfig = _configLoader.GetDefaultForTaskState(task.State);
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
        var selectedConfig = await SelectTaskVariantAsync(task.State, language, framework, repositoryPath)
                            ?? defaultConfig;

        _logger.LogInformation(
            "Selected agent {AgentId} for task {TaskId} in state {State}",
            selectedConfig.Id, task.Id, task.State);

        // 4. Get artifacts from previous stages
        var artifacts = await GetTaskArtifactsAsync(task.Id);

        // 5. Build the prompt
        var resolvedPrompt = _promptBuilder.BuildTaskPrompt(selectedConfig.Prompt, task, artifacts);

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
        var artifactType = DetermineArtifactTypeForTask(selectedConfig);

        return new ResolvedAgentConfig
        {
            Config = selectedConfig,
            ResolvedPrompt = resolvedPrompt,
            McpServers = mcpServers,
            MaxTurns = selectedConfig.MaxTurns,
            ExpectedArtifactType = artifactType
        };
    }

    public async Task<ResolvedAgentConfig> SelectAgentForBacklogItemAsync(BacklogItemEntity backlogItem, string repositoryPath)
    {
        // 1. Get the default agent for this backlog item state
        var defaultConfig = _configLoader.GetDefaultForBacklogState(backlogItem.State);
        if (defaultConfig == null)
        {
            throw new InvalidOperationException($"No default agent configuration found for backlog state: {backlogItem.State}");
        }

        // 2. Detect context if not already set
        var language = backlogItem.DetectedLanguage;
        var framework = backlogItem.DetectedFramework;

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
                "Detected context for backlog item {BacklogItemId}: language={Language}, framework={Framework}",
                backlogItem.Id, language ?? "unknown", framework ?? "unknown");
        }

        // 3. Try to find a matching variant
        var selectedConfig = await SelectBacklogVariantAsync(backlogItem.State, language, framework, repositoryPath)
                            ?? defaultConfig;

        _logger.LogInformation(
            "Selected agent {AgentId} for backlog item {BacklogItemId} in state {State}",
            selectedConfig.Id, backlogItem.Id, backlogItem.State);

        // 4. Get artifacts from previous stages
        var artifacts = await GetBacklogArtifactsAsync(backlogItem.Id);

        // 5. Build the prompt
        var resolvedPrompt = _promptBuilder.BuildBacklogPrompt(selectedConfig.Prompt, backlogItem, artifacts);

        // 6. Merge MCP servers
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
        var artifactType = DetermineArtifactTypeForBacklog(backlogItem.State);

        return new ResolvedAgentConfig
        {
            Config = selectedConfig,
            ResolvedPrompt = resolvedPrompt,
            McpServers = mcpServers,
            MaxTurns = selectedConfig.MaxTurns,
            ExpectedArtifactType = artifactType
        };
    }

    public async Task<IReadOnlyList<AgentArtifactEntity>> GetTaskArtifactsAsync(Guid taskId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ForgeDbContext>();

        return await db.AgentArtifacts
            .Where(a => a.TaskId == taskId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AgentArtifactEntity>> GetBacklogArtifactsAsync(Guid backlogItemId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ForgeDbContext>();

        return await db.AgentArtifacts
            .Where(a => a.BacklogItemId == backlogItemId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<AgentArtifactEntity> StoreTaskArtifactAsync(
        Guid taskId,
        PipelineState producedInState,
        ArtifactType artifactType,
        string content,
        string? agentId,
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
            artifact.BacklogItemId,
            artifact.ProducedInState,
            artifact.ProducedInBacklogState,
            artifact.ArtifactType,
            artifact.Content,
            artifact.CreatedAt,
            artifact.AgentId,
            artifact.ConfidenceScore
        );
        await _sseService.EmitArtifactCreatedAsync(artifactDto);

        return artifact;
    }

    public async Task<AgentArtifactEntity> StoreBacklogArtifactAsync(
        Guid backlogItemId,
        BacklogItemState producedInState,
        ArtifactType artifactType,
        string content,
        string? agentId,
        decimal? confidenceScore = null,
        bool humanInputRequested = false,
        string? humanInputReason = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ForgeDbContext>();

        var artifact = new AgentArtifactEntity
        {
            Id = Guid.NewGuid(),
            BacklogItemId = backlogItemId,
            ProducedInBacklogState = producedInState,
            ArtifactType = artifactType,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            AgentId = agentId,
            ConfidenceScore = confidenceScore,
            HumanInputRequested = humanInputRequested,
            HumanInputReason = humanInputReason
        };

        db.AgentArtifacts.Add(artifact);
        await db.SaveChangesAsync();

        _logger.LogInformation(
            "Stored artifact {ArtifactId} of type {Type} for backlog item {BacklogItemId}, confidence: {Confidence}",
            artifact.Id, artifactType, backlogItemId, confidenceScore);

        // Emit SSE event
        var artifactDto = new ArtifactDto(
            artifact.Id,
            artifact.TaskId,
            artifact.BacklogItemId,
            artifact.ProducedInState,
            artifact.ProducedInBacklogState,
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

    private async Task<AgentConfig?> SelectTaskVariantAsync(
        PipelineState state,
        string? language,
        string? framework,
        string repositoryPath)
    {
        var variants = _configLoader.GetVariantsForTaskState(state);
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

    private async Task<AgentConfig?> SelectBacklogVariantAsync(
        BacklogItemState state,
        string? language,
        string? framework,
        string repositoryPath)
    {
        var variants = _configLoader.GetVariantsForBacklogState(state);
        if (variants.Count == 0)
            return null;

        foreach (var variant in variants)
        {
            if (variant.Match == null)
                continue;

            if (!string.IsNullOrEmpty(variant.Match.Framework))
            {
                if (framework != null &&
                    framework.Equals(variant.Match.Framework, StringComparison.OrdinalIgnoreCase))
                {
                    return variant;
                }
            }

            if (!string.IsNullOrEmpty(variant.Match.Language))
            {
                if (language != null &&
                    language.Equals(variant.Match.Language, StringComparison.OrdinalIgnoreCase))
                {
                    return variant;
                }
            }

            if (variant.Match.Files != null && variant.Match.Files.Count > 0)
            {
                if (await _contextDetector.FilesPresentAsync(repositoryPath, variant.Match.Files))
                {
                    return variant;
                }
            }
        }

        return null;
    }

    private static ArtifactType DetermineArtifactTypeForTask(AgentConfig config)
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

        return config.TaskState switch
        {
            PipelineState.Research => ArtifactType.ResearchFindings,
            PipelineState.Planning => ArtifactType.Plan,
            PipelineState.Implementing => ArtifactType.Implementation,
            PipelineState.Simplifying => ArtifactType.SimplificationReview,
            PipelineState.Verifying => ArtifactType.VerificationReport,
            PipelineState.Reviewing => ArtifactType.Review,
            _ => ArtifactType.General
        };
    }

    private static ArtifactType DetermineArtifactTypeForBacklog(BacklogItemState state)
    {
        return state switch
        {
            BacklogItemState.Refining => ArtifactType.General,
            BacklogItemState.Splitting => ArtifactType.TaskSplit,
            _ => ArtifactType.General
        };
    }
}
