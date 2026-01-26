namespace Forge.Api.Features.Scheduler;

public class SchedulerOptions
{
    public const string SectionName = "Scheduler";

    public bool Enabled { get; set; } = true;
    public int PollingIntervalSeconds { get; set; } = 5;
    public int DefaultMaxRetries { get; set; } = 3;
}
