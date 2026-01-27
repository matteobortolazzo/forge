using System.Text;
using System.Text.RegularExpressions;
using Forge.Api.Data.Entities;

namespace Forge.Api.Features.Agents;

/// <summary>
/// Builds prompts by interpolating templates with task data and artifacts.
/// </summary>
public interface IPromptBuilder
{
    /// <summary>
    /// Builds a prompt from a template and task data.
    /// </summary>
    string BuildPrompt(string template, TaskEntity task, IReadOnlyList<AgentArtifactEntity> artifacts);

    /// <summary>
    /// Builds a prompt from a template with subtask context.
    /// </summary>
    string BuildPrompt(string template, TaskEntity task, SubtaskEntity? subtask, IReadOnlyList<AgentArtifactEntity> artifacts, string? repositoryPath = null);
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

    public string BuildPrompt(string template, TaskEntity task, IReadOnlyList<AgentArtifactEntity> artifacts)
    {
        return BuildPrompt(template, task, null, artifacts, null);
    }

    public string BuildPrompt(string template, TaskEntity task, SubtaskEntity? subtask, IReadOnlyList<AgentArtifactEntity> artifacts, string? repositoryPath = null)
    {
        var result = template;

        // Replace task placeholders
        result = ReplaceTaskPlaceholders(result, task);

        // Replace subtask placeholders (if subtask is provided)
        if (subtask != null)
        {
            result = ReplaceSubtaskPlaceholders(result, subtask);
        }

        // Replace context placeholders
        result = ReplaceContextPlaceholders(result, repositoryPath);

        // Replace specific artifact type placeholders (e.g., {artifacts.research}, {artifacts.plan})
        result = ReplaceSpecificArtifactPlaceholders(result, artifacts);

        // Replace general artifacts placeholder
        result = ReplaceArtifactsPlaceholder(result, artifacts);

        _logger.LogDebug("Built prompt with {Length} characters", result.Length);

        return result;
    }

    private static string ReplaceTaskPlaceholders(string template, TaskEntity task)
    {
        // Simple placeholder replacement using regex
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

    private static string ReplaceSubtaskPlaceholders(string template, SubtaskEntity subtask)
    {
        var result = template;

        // {subtask.title}
        result = SubtaskTitleRegex().Replace(result, subtask.Title);

        // {subtask.description}
        result = SubtaskDescriptionRegex().Replace(result, subtask.Description);

        // {subtask.acceptance_criteria}
        try
        {
            var criteria = System.Text.Json.JsonSerializer.Deserialize<List<string>>(subtask.AcceptanceCriteriaJson) ?? [];
            var criteriaText = criteria.Count > 0
                ? string.Join("\n", criteria.Select((c, i) => $"{i + 1}. {c}"))
                : "*No acceptance criteria defined.*";
            result = SubtaskAcceptanceCriteriaRegex().Replace(result, criteriaText);
        }
        catch
        {
            result = SubtaskAcceptanceCriteriaRegex().Replace(result, "*Invalid acceptance criteria format.*");
        }

        // {subtask.scope}
        result = SubtaskScopeRegex().Replace(result, subtask.EstimatedScope.ToString());

        // {subtask.status}
        result = SubtaskStatusRegex().Replace(result, subtask.Status.ToString());

        return result;
    }

    private static string ReplaceContextPlaceholders(string template, string? repositoryPath)
    {
        var result = template;

        // {context.repo_path}
        result = ContextRepoPathRegex().Replace(result, repositoryPath ?? Environment.CurrentDirectory);

        return result;
    }

    private static string ReplaceSpecificArtifactPlaceholders(string template, IReadOnlyList<AgentArtifactEntity> artifacts)
    {
        var result = template;

        // Find most recent artifact of each type
        var latestByType = artifacts
            .GroupBy(a => a.ArtifactType)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.CreatedAt).First());

        // {artifacts.research}
        if (latestByType.TryGetValue(Shared.ArtifactType.ResearchFindings, out var research))
        {
            result = ArtifactsResearchRegex().Replace(result, research.Content.Trim());
        }
        else
        {
            result = ArtifactsResearchRegex().Replace(result, "*No research findings available.*");
        }

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

        // {artifacts.simplification}
        if (latestByType.TryGetValue(Shared.ArtifactType.SimplificationReview, out var simplification))
        {
            result = ArtifactsSimplificationRegex().Replace(result, simplification.Content.Trim());
        }
        else
        {
            result = ArtifactsSimplificationRegex().Replace(result, "*No simplification review available.*");
        }

        // {artifacts.review}
        if (latestByType.TryGetValue(Shared.ArtifactType.Review, out var review))
        {
            result = ArtifactsReviewRegex().Replace(result, review.Content.Trim());
        }
        else
        {
            result = ArtifactsReviewRegex().Replace(result, "*No code review available.*");
        }

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
                artifactsSection.AppendLine($"### {GetArtifactTypeLabel(artifact.ArtifactType)} (from {artifact.ProducedInState} stage)");
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
            Shared.ArtifactType.ResearchFindings => "Research Findings",
            Shared.ArtifactType.Plan => "Implementation Plan",
            Shared.ArtifactType.Implementation => "Implementation Summary",
            Shared.ArtifactType.SimplificationReview => "Simplification Review",
            Shared.ArtifactType.VerificationReport => "Verification Report",
            Shared.ArtifactType.Review => "Code Review",
            Shared.ArtifactType.Test => "Testing Report",
            Shared.ArtifactType.General => "Notes",
            _ => type.ToString()
        };
    }

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

    // Subtask placeholders
    [GeneratedRegex(@"\{subtask\.title\}", RegexOptions.IgnoreCase)]
    private static partial Regex SubtaskTitleRegex();

    [GeneratedRegex(@"\{subtask\.description\}", RegexOptions.IgnoreCase)]
    private static partial Regex SubtaskDescriptionRegex();

    [GeneratedRegex(@"\{subtask\.acceptance_criteria\}", RegexOptions.IgnoreCase)]
    private static partial Regex SubtaskAcceptanceCriteriaRegex();

    [GeneratedRegex(@"\{subtask\.scope\}", RegexOptions.IgnoreCase)]
    private static partial Regex SubtaskScopeRegex();

    [GeneratedRegex(@"\{subtask\.status\}", RegexOptions.IgnoreCase)]
    private static partial Regex SubtaskStatusRegex();

    // Context placeholders
    [GeneratedRegex(@"\{context\.repo_path\}", RegexOptions.IgnoreCase)]
    private static partial Regex ContextRepoPathRegex();

    // Specific artifact type placeholders
    [GeneratedRegex(@"\{artifacts\.research\}", RegexOptions.IgnoreCase)]
    private static partial Regex ArtifactsResearchRegex();

    [GeneratedRegex(@"\{artifacts\.plan\}", RegexOptions.IgnoreCase)]
    private static partial Regex ArtifactsPlanRegex();

    [GeneratedRegex(@"\{artifacts\.implementation\}", RegexOptions.IgnoreCase)]
    private static partial Regex ArtifactsImplementationRegex();

    [GeneratedRegex(@"\{artifacts\.simplification\}", RegexOptions.IgnoreCase)]
    private static partial Regex ArtifactsSimplificationRegex();

    [GeneratedRegex(@"\{artifacts\.review\}", RegexOptions.IgnoreCase)]
    private static partial Regex ArtifactsReviewRegex();
}
