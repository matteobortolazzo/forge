using System.Text.Json.Serialization;

namespace Forge.Api.Shared;

/// <summary>
/// Pipeline states for task workflow. Serialized as PascalCase.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PipelineState
{
    Backlog,
    Planning,
    Implementing,
    Reviewing,
    Testing,
    PrReady,
    Done
}

/// <summary>
/// Priority levels for tasks. Serialized as lowercase.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Priority>))]
public enum Priority
{
    [JsonStringEnumMemberName("low")]
    Low,
    [JsonStringEnumMemberName("medium")]
    Medium,
    [JsonStringEnumMemberName("high")]
    High,
    [JsonStringEnumMemberName("critical")]
    Critical
}

/// <summary>
/// Log types for agent output. Serialized as camelCase.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<LogType>))]
public enum LogType
{
    [JsonStringEnumMemberName("info")]
    Info,
    [JsonStringEnumMemberName("toolUse")]
    ToolUse,
    [JsonStringEnumMemberName("toolResult")]
    ToolResult,
    [JsonStringEnumMemberName("error")]
    Error,
    [JsonStringEnumMemberName("thinking")]
    Thinking
}
