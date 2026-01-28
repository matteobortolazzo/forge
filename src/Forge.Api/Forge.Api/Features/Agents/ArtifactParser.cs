using System.Text.RegularExpressions;
using Forge.Api.Shared;

namespace Forge.Api.Features.Agents;

/// <summary>
/// Parses agent output to extract structured artifacts and recommendations.
/// </summary>
public interface IArtifactParser
{
    /// <summary>
    /// Extracts the artifact content from agent output for a task.
    /// </summary>
    ParsedArtifact? ParseTaskArtifact(string agentOutput, AgentConfig config);

    /// <summary>
    /// Extracts the artifact content from agent output for a backlog item.
    /// </summary>
    ParsedBacklogArtifact? ParseBacklogArtifact(string agentOutput, AgentConfig config);

    /// <summary>
    /// Extracts the recommended next state from agent output.
    /// </summary>
    PipelineState? ParseRecommendedNextState(string agentOutput);

    /// <summary>
    /// Extracts the recommended next backlog state from agent output.
    /// </summary>
    BacklogItemState? ParseRecommendedNextBacklogState(string agentOutput);

    /// <summary>
    /// Extracts the confidence score from agent output.
    /// </summary>
    decimal? ParseConfidenceScore(string agentOutput);

    /// <summary>
    /// Checks if the agent requested human input.
    /// </summary>
    (bool Requested, string? Reason) ParseHumanInputRequest(string agentOutput);

    /// <summary>
    /// Extracts the verdict from a simplification review.
    /// </summary>
    string? ParseSimplificationVerdict(string agentOutput);
}

/// <summary>
/// Parsed artifact from agent output for tasks.
/// </summary>
public record ParsedArtifact(
    ArtifactType Type,
    string Content,
    decimal? ConfidenceScore = null,
    bool HumanInputRequested = false,
    string? HumanInputReason = null
);

/// <summary>
/// Parsed artifact from agent output for backlog items.
/// </summary>
public record ParsedBacklogArtifact(
    ArtifactType Type,
    string Content,
    decimal? ConfidenceScore = null,
    bool HumanInputRequested = false,
    string? HumanInputReason = null
);

/// <summary>
/// Implementation of IArtifactParser.
/// </summary>
public partial class ArtifactParser : IArtifactParser
{
    private readonly ILogger<ArtifactParser> _logger;

    // Section headers that mark the start of an artifact
    private static readonly string[] ArtifactHeaders =
    [
        "# Implementation Plan",
        "# Implementation Summary",
        "# Code Review",
        "# Angular Code Review",
        "# Testing Report",
        "# Research Findings",
        "# Task Split",
        "# Simplification Review",
        "# Verification Report",
        "# Refined Specification",
    ];

    public ArtifactParser(ILogger<ArtifactParser> logger)
    {
        _logger = logger;
    }

    public ParsedArtifact? ParseTaskArtifact(string agentOutput, AgentConfig config)
    {
        if (string.IsNullOrWhiteSpace(agentOutput))
            return null;

        // Determine expected artifact type from config
        var artifactType = GetTaskArtifactType(config);

        // Try to find the artifact section in the output
        var content = ExtractArtifactContent(agentOutput);

        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("No structured artifact found in agent output");
            // Fall back to using the entire output as the artifact
            content = agentOutput;
        }

        // Extract confidence score and human input request
        var confidenceScore = ParseConfidenceScore(agentOutput);
        var (humanInputRequested, humanInputReason) = ParseHumanInputRequest(agentOutput);

        _logger.LogDebug("Parsed task artifact of type {Type} with {Length} characters, confidence: {Confidence}, human input: {HumanInput}",
            artifactType, content.Length, confidenceScore, humanInputRequested);

        return new ParsedArtifact(artifactType, content, confidenceScore, humanInputRequested, humanInputReason);
    }

    public ParsedBacklogArtifact? ParseBacklogArtifact(string agentOutput, AgentConfig config)
    {
        if (string.IsNullOrWhiteSpace(agentOutput))
            return null;

        // Determine expected artifact type from config
        var artifactType = GetBacklogArtifactType(config);

        // Try to find the artifact section in the output
        var content = ExtractArtifactContent(agentOutput);

        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("No structured artifact found in agent output");
            content = agentOutput;
        }

        // Extract confidence score and human input request
        var confidenceScore = ParseConfidenceScore(agentOutput);
        var (humanInputRequested, humanInputReason) = ParseHumanInputRequest(agentOutput);

        _logger.LogDebug("Parsed backlog artifact of type {Type} with {Length} characters, confidence: {Confidence}, human input: {HumanInput}",
            artifactType, content.Length, confidenceScore, humanInputRequested);

        return new ParsedBacklogArtifact(artifactType, content, confidenceScore, humanInputRequested, humanInputReason);
    }

    public PipelineState? ParseRecommendedNextState(string agentOutput)
    {
        if (string.IsNullOrWhiteSpace(agentOutput))
            return null;

        // Look for "Recommended Next State" section
        var match = RecommendedStateRegex().Match(agentOutput);
        if (!match.Success)
        {
            _logger.LogDebug("No recommended next state found in output");
            return null;
        }

        var stateText = match.Groups[1].Value.Trim();

        // Parse the state - it might be in a sentence or just the state name
        foreach (var state in Enum.GetValues<PipelineState>())
        {
            if (stateText.Contains(state.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Parsed recommended next state: {State}", state);
                return state;
            }
        }

        _logger.LogDebug("Could not parse state from: {Text}", stateText);
        return null;
    }

    public BacklogItemState? ParseRecommendedNextBacklogState(string agentOutput)
    {
        if (string.IsNullOrWhiteSpace(agentOutput))
            return null;

        // Look for "Recommended Next State" section
        var match = RecommendedStateRegex().Match(agentOutput);
        if (!match.Success)
        {
            _logger.LogDebug("No recommended next backlog state found in output");
            return null;
        }

        var stateText = match.Groups[1].Value.Trim();

        // Parse the state
        foreach (var state in Enum.GetValues<BacklogItemState>())
        {
            if (stateText.Contains(state.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Parsed recommended next backlog state: {State}", state);
                return state;
            }
        }

        _logger.LogDebug("Could not parse backlog state from: {Text}", stateText);
        return null;
    }

    public decimal? ParseConfidenceScore(string agentOutput)
    {
        if (string.IsNullOrWhiteSpace(agentOutput))
            return null;

        // Look for confidence_score in YAML format
        var match = ConfidenceScoreRegex().Match(agentOutput);
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out var score))
        {
            // Ensure score is between 0 and 1
            if (score is >= 0 and <= 1)
            {
                _logger.LogDebug("Parsed confidence score: {Score}", score);
                return score;
            }
        }

        return null;
    }

    public (bool Requested, string? Reason) ParseHumanInputRequest(string agentOutput)
    {
        if (string.IsNullOrWhiteSpace(agentOutput))
            return (false, null);

        // Look for human_input_requested in YAML format
        var requestedMatch = HumanInputRequestedRegex().Match(agentOutput);
        if (requestedMatch.Success)
        {
            var value = requestedMatch.Groups[1].Value.Trim().ToLowerInvariant();
            if (value is "true" or "yes")
            {
                // Try to find the reason
                var reasonMatch = HumanInputReasonRegex().Match(agentOutput);
                var reason = reasonMatch.Success ? reasonMatch.Groups[1].Value.Trim() : null;

                _logger.LogDebug("Human input requested: {Reason}", reason);
                return (true, reason);
            }
        }

        return (false, null);
    }

    public string? ParseSimplificationVerdict(string agentOutput)
    {
        if (string.IsNullOrWhiteSpace(agentOutput))
            return null;

        // Look for verdict in YAML format
        var match = VerdictRegex().Match(agentOutput);
        if (match.Success)
        {
            var verdict = match.Groups[1].Value.Trim().Trim('"');
            _logger.LogDebug("Parsed verdict: {Verdict}", verdict);
            return verdict;
        }

        return null;
    }

    private static ArtifactType GetTaskArtifactType(AgentConfig config)
    {
        // First check explicit output type in config
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

        // Fall back to inferring from task state
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

    private static ArtifactType GetBacklogArtifactType(AgentConfig config)
    {
        // First check explicit output type in config
        if (config.Output?.Type != null)
        {
            return config.Output.Type.ToLowerInvariant() switch
            {
                "task_split" => ArtifactType.TaskSplit,
                "refined_spec" => ArtifactType.General, // Refining agent output
                _ => ArtifactType.General
            };
        }

        // Fall back to inferring from backlog state
        return config.BacklogState switch
        {
            BacklogItemState.Refining => ArtifactType.General,
            BacklogItemState.Splitting => ArtifactType.TaskSplit,
            _ => ArtifactType.General
        };
    }

    private string ExtractArtifactContent(string output)
    {
        // Find the start of the artifact by looking for known headers
        var artifactStart = -1;
        var headerUsed = "";

        foreach (var header in ArtifactHeaders)
        {
            var idx = output.IndexOf(header, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0 && (artifactStart < 0 || idx < artifactStart))
            {
                artifactStart = idx;
                headerUsed = header;
            }
        }

        if (artifactStart < 0)
        {
            // No known header found - try to find any markdown header
            var headerMatch = MarkdownHeaderRegex().Match(output);
            if (headerMatch.Success)
            {
                artifactStart = headerMatch.Index;
            }
        }

        if (artifactStart < 0)
            return output;

        // Extract from the header to the end (or to the next major section)
        var content = output[artifactStart..];

        _logger.LogDebug("Extracted artifact starting with: {Header}", headerUsed);

        return content.Trim();
    }

    [GeneratedRegex(@"## Recommended Next State\s*\n+(.+?)(?:\n\n|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex RecommendedStateRegex();

    [GeneratedRegex(@"^# .+$", RegexOptions.Multiline)]
    private static partial Regex MarkdownHeaderRegex();

    // New patterns for confidence, human input, and verdict parsing
    [GeneratedRegex(@"confidence_score:\s*([\d.]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ConfidenceScoreRegex();

    [GeneratedRegex(@"human_input_requested:\s*(\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex HumanInputRequestedRegex();

    [GeneratedRegex(@"human_input_reason:\s*[""']?([^""'\n]+)[""']?", RegexOptions.IgnoreCase)]
    private static partial Regex HumanInputReasonRegex();

    [GeneratedRegex(@"verdict:\s*[""']?(\w+)[""']?", RegexOptions.IgnoreCase)]
    private static partial Regex VerdictRegex();
}
