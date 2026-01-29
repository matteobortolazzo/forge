using System.Text;
using System.Text.RegularExpressions;
using Forge.Api.Data.Entities;

namespace Forge.Api.Features.Agents;

/// <summary>
/// Builds prompts by interpolating templates with task or backlog item data and artifacts.
/// </summary>
public interface IPromptBuilder
{
    /// <summary>
    /// Builds a prompt from a template and task data.
    /// </summary>
    string BuildTaskPrompt(string template, TaskEntity task, IReadOnlyList<AgentArtifactEntity> artifacts, string? repositoryPath = null);

    /// <summary>
    /// Builds a prompt from a template and backlog item data.
    /// </summary>
    string BuildBacklogPrompt(string template, BacklogItemEntity backlogItem, IReadOnlyList<AgentArtifactEntity> artifacts, string? repositoryPath = null);
}

/// <summary>
/// Implementation of IPromptBuilder.
/// </summary>
public partial class PromptBuilder : IPromptBuilder
{
    private readonly ILogger<PromptBuilder> _logger;

    public PromptBuilder(ILogger<PromptBuilder> logger)
    {
        _logger = logger;
    }

    public string BuildTaskPrompt(string template, TaskEntity task, IReadOnlyList<AgentArtifactEntity> artifacts, string? repositoryPath = null)
    {
        var result = template;

        // Replace task placeholders
        result = ReplaceTaskPlaceholders(result, task);

        // Replace context placeholders
        result = ReplaceContextPlaceholders(result, task.DetectedLanguage, task.DetectedFramework, repositoryPath);

        // Replace specific artifact type placeholders (e.g., {artifacts.research}, {artifacts.plan})
        result = ReplaceSpecificArtifactPlaceholders(result, artifacts);

        // Replace general artifacts placeholder
        result = ReplaceArtifactsPlaceholder(result, artifacts);

        _logger.LogDebug("Built task prompt with {Length} characters", result.Length);

        return result;
    }

    public string BuildBacklogPrompt(string template, BacklogItemEntity backlogItem, IReadOnlyList<AgentArtifactEntity> artifacts, string? repositoryPath = null)
    {
        var result = template;

        // Replace backlog item placeholders
        result = ReplaceBacklogItemPlaceholders(result, backlogItem);

        // Replace context placeholders
        result = ReplaceContextPlaceholders(result, backlogItem.DetectedLanguage, backlogItem.DetectedFramework, repositoryPath);

        // Replace specific artifact type placeholders (e.g., {artifacts.research}, {artifacts.plan})
        result = ReplaceSpecificArtifactPlaceholders(result, artifacts);

        // Replace general artifacts placeholder
        result = ReplaceArtifactsPlaceholder(result, artifacts);

        _logger.LogDebug("Built backlog item prompt with {Length} characters", result.Length);

        return result;
    }

    private static string ReplaceTaskPlaceholders(string template, TaskEntity task)
    {
        var result = template;

        // {task.title}
        result = TaskTitleRegex().Replace(result, task.Title);

        // {task.description}
        result = TaskDescriptionRegex().Replace(result, task.Description);

        // {task.state}
        result = TaskStateRegex().Replace(result, task.State.ToString());

        // {task.priority}
        result = TaskPriorityRegex().Replace(result, task.Priority.ToString());

        // {task.id}
        result = TaskIdRegex().Replace(result, task.Id.ToString());

        // {task.language}
        result = TaskLanguageRegex().Replace(result, task.DetectedLanguage ?? "unknown");

        // {task.framework}
        result = TaskFrameworkRegex().Replace(result, task.DetectedFramework ?? "unknown");

        return result;
    }

    private static string ReplaceBacklogItemPlaceholders(string template, BacklogItemEntity backlogItem)
    {
        var result = template;

        // {backlogItem.title}
        result = BacklogItemTitleRegex().Replace(result, backlogItem.Title);

        // {backlogItem.description}
        result = BacklogItemDescriptionRegex().Replace(result, backlogItem.Description);

        // {backlogItem.state}
        result = BacklogItemStateRegex().Replace(result, backlogItem.State.ToString());

        // {backlogItem.priority}
        result = BacklogItemPriorityRegex().Replace(result, backlogItem.Priority.ToString());

        // {backlogItem.id}
        result = BacklogItemIdRegex().Replace(result, backlogItem.Id.ToString());

        // {backlogItem.language}
        result = BacklogItemLanguageRegex().Replace(result, backlogItem.DetectedLanguage ?? "unknown");

        // {backlogItem.framework}
        result = BacklogItemFrameworkRegex().Replace(result, backlogItem.DetectedFramework ?? "unknown");

        // {backlogItem.acceptanceCriteria}
        result = BacklogItemAcceptanceCriteriaRegex().Replace(result, backlogItem.AcceptanceCriteria ?? "*No acceptance criteria defined.*");

        // {backlogItem.refiningIterations}
        result = BacklogItemRefiningIterationsRegex().Replace(result, backlogItem.RefiningIterations.ToString());

        return result;
    }

    private static string ReplaceContextPlaceholders(string template, string? language, string? framework, string? repositoryPath)
    {
        var result = template;

        // {context.repo_path}
        result = ContextRepoPathRegex().Replace(result, repositoryPath ?? Environment.CurrentDirectory);

        // {context.language}
        result = ContextLanguageRegex().Replace(result, language ?? "unknown");

        // {context.framework}
        result = ContextFrameworkRegex().Replace(result, framework ?? "unknown");

        return result;
    }

    private static string ReplaceSpecificArtifactPlaceholders(string template, IReadOnlyList<AgentArtifactEntity> artifacts)
    {
        var result = template;

        // Find most recent artifact of each type
        var latestByType = artifacts
            .GroupBy(a => a.ArtifactType)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.CreatedAt).First());

        // {artifacts.split}
        if (latestByType.TryGetValue(Shared.ArtifactType.TaskSplit, out var split))
        {
            result = ArtifactsSplitRegex().Replace(result, split.Content.Trim());
        }
        else
        {
            result = ArtifactsSplitRegex().Replace(result, "*No task split available.*");
        }

        // {artifacts.research} - kept for backwards compatibility but no longer produced
        result = ArtifactsResearchRegex().Replace(result, "*Research is now part of Planning phase.*");

        // {artifacts.plan}
        if (latestByType.TryGetValue(Shared.ArtifactType.Plan, out var plan))
        {
            result = ArtifactsPlanRegex().Replace(result, plan.Content.Trim());
        }
        else
        {
            result = ArtifactsPlanRegex().Replace(result, "*No implementation plan available.*");
        }

        // {artifacts.implementation}
        if (latestByType.TryGetValue(Shared.ArtifactType.Implementation, out var implementation))
        {
            result = ArtifactsImplementationRegex().Replace(result, implementation.Content.Trim());
        }
        else
        {
            result = ArtifactsImplementationRegex().Replace(result, "*No implementation available.*");
        }

        // {artifacts.simplification} - kept for backwards compatibility but no longer produced
        result = ArtifactsSimplificationRegex().Replace(result, "*Simplification is now part of Implementing phase.*");

        // {artifacts.verification} - kept for backwards compatibility but no longer produced
        result = ArtifactsVerificationRegex().Replace(result, "*Verification is now part of Implementing phase.*");

        // {artifacts.review} - kept for backwards compatibility but no longer produced
        result = ArtifactsReviewRegex().Replace(result, "*Review is now done by user on git provider.*");

        return result;
    }

    private string ReplaceArtifactsPlaceholder(string template, IReadOnlyList<AgentArtifactEntity> artifacts)
    {
        if (!template.Contains("{artifacts}"))
            return template;

        var artifactsSection = new StringBuilder();

        if (artifacts.Count == 0)
        {
            artifactsSection.AppendLine("*No previous artifacts available.*");
        }
        else
        {
            foreach (var artifact in artifacts.OrderBy(a => a.CreatedAt))
            {
                var stateLabel = artifact.ProducedInState.HasValue
                    ? artifact.ProducedInState.Value.ToString()
                    : artifact.ProducedInBacklogState?.ToString() ?? "Unknown";

                artifactsSection.AppendLine($"### {GetArtifactTypeLabel(artifact.ArtifactType)} (from {stateLabel} stage)");
                artifactsSection.AppendLine();
                artifactsSection.AppendLine(artifact.Content.Trim());
                artifactsSection.AppendLine();
                artifactsSection.AppendLine("---");
                artifactsSection.AppendLine();
            }
        }

        return template.Replace("{artifacts}", artifactsSection.ToString().TrimEnd());
    }

    private static string GetArtifactTypeLabel(Shared.ArtifactType type)
    {
        return type switch
        {
            Shared.ArtifactType.TaskSplit => "Task Split",
            Shared.ArtifactType.Plan => "Implementation Plan",
            Shared.ArtifactType.Implementation => "Implementation Summary",
            Shared.ArtifactType.Test => "Testing Report",
            Shared.ArtifactType.General => "Notes",
            _ => type.ToString()
        };
    }

    // Task placeholders
    [GeneratedRegex(@"\{task\.title\}", RegexOptions.IgnoreCase)]
    private static partial Regex TaskTitleRegex();

    [GeneratedRegex(@"\{task\.description\}", RegexOptions.IgnoreCase)]
    private static partial Regex TaskDescriptionRegex();

    [GeneratedRegex(@"\{task\.state\}", RegexOptions.IgnoreCase)]
    private static partial Regex TaskStateRegex();

    [GeneratedRegex(@"\{task\.priority\}", RegexOptions.IgnoreCase)]
    private static partial Regex TaskPriorityRegex();

    [GeneratedRegex(@"\{task\.id\}", RegexOptions.IgnoreCase)]
    private static partial Regex TaskIdRegex();

    [GeneratedRegex(@"\{task\.language\}", RegexOptions.IgnoreCase)]
    private static partial Regex TaskLanguageRegex();

    [GeneratedRegex(@"\{task\.framework\}", RegexOptions.IgnoreCase)]
    private static partial Regex TaskFrameworkRegex();

    // Backlog item placeholders
    [GeneratedRegex(@"\{backlogItem\.title\}", RegexOptions.IgnoreCase)]
    private static partial Regex BacklogItemTitleRegex();

    [GeneratedRegex(@"\{backlogItem\.description\}", RegexOptions.IgnoreCase)]
    private static partial Regex BacklogItemDescriptionRegex();

    [GeneratedRegex(@"\{backlogItem\.state\}", RegexOptions.IgnoreCase)]
    private static partial Regex BacklogItemStateRegex();

    [GeneratedRegex(@"\{backlogItem\.priority\}", RegexOptions.IgnoreCase)]
    private static partial Regex BacklogItemPriorityRegex();

    [GeneratedRegex(@"\{backlogItem\.id\}", RegexOptions.IgnoreCase)]
    private static partial Regex BacklogItemIdRegex();

    [GeneratedRegex(@"\{backlogItem\.language\}", RegexOptions.IgnoreCase)]
    private static partial Regex BacklogItemLanguageRegex();

    [GeneratedRegex(@"\{backlogItem\.framework\}", RegexOptions.IgnoreCase)]
    private static partial Regex BacklogItemFrameworkRegex();

    [GeneratedRegex(@"\{backlogItem\.acceptanceCriteria\}", RegexOptions.IgnoreCase)]
    private static partial Regex BacklogItemAcceptanceCriteriaRegex();

    [GeneratedRegex(@"\{backlogItem\.refiningIterations\}", RegexOptions.IgnoreCase)]
    private static partial Regex BacklogItemRefiningIterationsRegex();

    // Context placeholders
    [GeneratedRegex(@"\{context\.repo_path\}", RegexOptions.IgnoreCase)]
    private static partial Regex ContextRepoPathRegex();

    [GeneratedRegex(@"\{context\.language\}", RegexOptions.IgnoreCase)]
    private static partial Regex ContextLanguageRegex();

    [GeneratedRegex(@"\{context\.framework\}", RegexOptions.IgnoreCase)]
    private static partial Regex ContextFrameworkRegex();

    // Specific artifact type placeholders
    [GeneratedRegex(@"\{artifacts\.split\}", RegexOptions.IgnoreCase)]
    private static partial Regex ArtifactsSplitRegex();

    [GeneratedRegex(@"\{artifacts\.research\}", RegexOptions.IgnoreCase)]
    private static partial Regex ArtifactsResearchRegex();

    [GeneratedRegex(@"\{artifacts\.plan\}", RegexOptions.IgnoreCase)]
    private static partial Regex ArtifactsPlanRegex();

    [GeneratedRegex(@"\{artifacts\.implementation\}", RegexOptions.IgnoreCase)]
    private static partial Regex ArtifactsImplementationRegex();

    [GeneratedRegex(@"\{artifacts\.simplification\}", RegexOptions.IgnoreCase)]
    private static partial Regex ArtifactsSimplificationRegex();

    [GeneratedRegex(@"\{artifacts\.verification\}", RegexOptions.IgnoreCase)]
    private static partial Regex ArtifactsVerificationRegex();

    [GeneratedRegex(@"\{artifacts\.review\}", RegexOptions.IgnoreCase)]
    private static partial Regex ArtifactsReviewRegex();
}
