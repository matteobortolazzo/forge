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
        var result = template;

        // Replace task placeholders
        result = ReplaceTaskPlaceholders(result, task);

        // Replace artifacts placeholder
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
            Shared.ArtifactType.Plan => "Implementation Plan",
            Shared.ArtifactType.Implementation => "Implementation Summary",
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
}
