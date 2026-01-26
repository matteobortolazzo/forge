namespace Forge.Api.Features.Scheduler;

/// <summary>
/// Singleton that holds the scheduler's enabled state across scoped service instances.
/// </summary>
public class SchedulerState
{
    private readonly Lock _lock = new();
    private bool _isEnabled = true;

    public bool IsEnabled
    {
        get
        {
            lock (_lock)
            {
                return _isEnabled;
            }
        }
    }

    public void Enable()
    {
        lock (_lock)
        {
            _isEnabled = true;
        }
    }

    public void Disable()
    {
        lock (_lock)
        {
            _isEnabled = false;
        }
    }
}
