namespace Forge.E2E.Console.Models;

/// <summary>
/// Configuration for the Forge API connection.
/// </summary>
public sealed class ForgeApiOptions
{
    public const string SectionName = "ForgeApi";

    public string BaseUrl { get; set; } = "http://localhost:5000";
}

/// <summary>
/// Configuration for the test repository.
/// </summary>
public sealed class TestRepositoryOptions
{
    public const string SectionName = "TestRepository";

    public string Path { get; set; } = "/tmp/forge-e2e-test-repo";
    public bool CreateIfMissing { get; set; } = true;
}

/// <summary>
/// Timeout configuration for different pipeline phases.
/// </summary>
public sealed class TimeoutOptions
{
    public const string SectionName = "Timeouts";

    public int RefiningMinutes { get; set; } = 5;
    public int SplittingMinutes { get; set; } = 5;
    public int PlanningMinutes { get; set; } = 10;
    public int ImplementingMinutes { get; set; } = 15;
}

/// <summary>
/// E2E test behavior options.
/// </summary>
public sealed class E2EOptions
{
    public const string SectionName = "E2E";

    public bool AutoApproveGates { get; set; } = true;
    public bool AutoAnswerQuestions { get; set; } = true;
}
