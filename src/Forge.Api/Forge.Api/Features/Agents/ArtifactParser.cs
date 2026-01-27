using System.Text.RegularExpressions;
using Forge.Api.Shared;

namespace Forge.Api.Features.Agents;

/// <summary>
/// Parses agent output to extract structured artifacts and recommendations.
/// </summary>
public interface IArtifactParser
{
    /// <summary>
    /// Extracts the artifact content from agent output.
    /// </summary>
    ParsedArtifact? ParseArtifact(string agentOutput, AgentConfig config);

    /// <summary>
    /// Extracts the recommended next state from agent output.
    /// </summary>
    PipelineState? ParseRecommendedNextState(string agentOutput);
}

/// <summary>
/// Parsed artifact from agent output.
/// </summary>
public record ParsedArtifact(
    ArtifactType Type,
    string Content
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
    ];

    public ArtifactParser(ILogger<ArtifactParser> logger)
    {
        _logger = logger;
    }

    public ParsedArtifact? ParseArtifact(string agentOutput, AgentConfig config)
    {
        if (string.IsNullOrWhiteSpace(agentOutput))
            return null;

        // Determine expected artifact type from config
        var artifactType = GetArtifactType(config);

        // Try to find the artifact section in the output
        var content = ExtractArtifactContent(agentOutput);

        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("No structured artifact found in agent output");
            // Fall back to using the entire output as the artifact
            content = agentOutput;
        }

        _logger.LogDebug("Parsed artifact of type {Type} with {Length} characters", artifactType, content.Length);

        return new ParsedArtifact(artifactType, content);
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

    private static ArtifactType GetArtifactType(AgentConfig config)
    {
        // First check explicit output type in config
        if (config.Output?.Type != null)
        {
            return config.Output.Type.ToLowerInvariant() switch
            {
                "plan" => ArtifactType.Plan,
                "implementation" => ArtifactType.Implementation,
                "review" => ArtifactType.Review,
                "test" => ArtifactType.Test,
                _ => ArtifactType.General
            };
        }

        // Fall back to inferring from state
        return config.State switch
        {
            PipelineState.Planning => ArtifactType.Plan,
            PipelineState.Implementing => ArtifactType.Implementation,
            PipelineState.Reviewing => ArtifactType.Review,
            PipelineState.Testing => ArtifactType.Test,
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
}
