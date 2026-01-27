using Forge.Api.Shared;
using YamlDotNet.Serialization;

namespace Forge.Api.Features.Agents;

/// <summary>
/// Agent configuration loaded from YAML files.
/// </summary>
public class AgentConfig
{
    /// <summary>
    /// Unique identifier for this agent configuration (e.g., "planning-default").
    /// </summary>
    [YamlMember(Alias = "id")]
    public required string Id { get; set; }

    /// <summary>
    /// Human-readable name for the agent.
    /// </summary>
    [YamlMember(Alias = "name")]
    public required string Name { get; set; }

    /// <summary>
    /// The pipeline state this agent handles.
    /// </summary>
    [YamlMember(Alias = "state")]
    public PipelineState State { get; set; }

    /// <summary>
    /// Description of what this agent does.
    /// </summary>
    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    /// <summary>
    /// The prompt template with placeholders like {task.title}, {task.description}, {artifacts}.
    /// </summary>
    [YamlMember(Alias = "prompt")]
    public required string Prompt { get; set; }

    /// <summary>
    /// ID of the base agent configuration this extends (for variants).
    /// </summary>
    [YamlMember(Alias = "extends")]
    public string? Extends { get; set; }

    /// <summary>
    /// Match rules for selecting this variant.
    /// </summary>
    [YamlMember(Alias = "match")]
    public AgentMatchRules? Match { get; set; }

    /// <summary>
    /// Expected output configuration.
    /// </summary>
    [YamlMember(Alias = "output")]
    public AgentOutputConfig? Output { get; set; }

    /// <summary>
    /// List of MCP server names to enable for this agent.
    /// </summary>
    [YamlMember(Alias = "mcp_servers")]
    public List<string>? McpServers { get; set; }

    /// <summary>
    /// Maximum conversation turns for this agent (default: 50).
    /// </summary>
    [YamlMember(Alias = "max_turns")]
    public int MaxTurns { get; set; } = 50;

    /// <summary>
    /// Whether this is a variant configuration.
    /// </summary>
    public bool IsVariant => !string.IsNullOrEmpty(Extends);

    /// <summary>
    /// The source file path (set by loader).
    /// </summary>
    public string? SourceFile { get; set; }
}

/// <summary>
/// Rules for matching tasks to agent variants.
/// </summary>
public class AgentMatchRules
{
    /// <summary>
    /// Match by detected framework (e.g., "angular", "dotnet").
    /// </summary>
    [YamlMember(Alias = "framework")]
    public string? Framework { get; set; }

    /// <summary>
    /// Match by detected language (e.g., "typescript", "csharp").
    /// </summary>
    [YamlMember(Alias = "language")]
    public string? Language { get; set; }

    /// <summary>
    /// Match if any of these files exist in the repository.
    /// </summary>
    [YamlMember(Alias = "files")]
    public List<string>? Files { get; set; }
}

/// <summary>
/// Configuration for expected agent output.
/// </summary>
public class AgentOutputConfig
{
    /// <summary>
    /// The artifact type this agent produces.
    /// </summary>
    [YamlMember(Alias = "type")]
    public string? Type { get; set; }

    /// <summary>
    /// Expected markdown schema/structure description.
    /// </summary>
    [YamlMember(Alias = "schema")]
    public string? Schema { get; set; }
}

/// <summary>
/// Resolved agent configuration with base config merged.
/// </summary>
public class ResolvedAgentConfig
{
    public required AgentConfig Config { get; init; }
    public required string ResolvedPrompt { get; init; }
    public List<string> McpServers { get; init; } = [];
    public int MaxTurns { get; init; }
    public ArtifactType ExpectedArtifactType { get; init; }
}
