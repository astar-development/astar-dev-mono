namespace AStarDev.CloudSync.Sync;

/// <summary>Timing of the <see cref="ISyncScheduler"/>.</summary>
public sealed record SyncSchedulerOptions
{
    internal SyncSchedulerOptions(TimeSpan startupDelay, TimeSpan checkEvery)
    {
        StartupDelay = startupDelay;
        CheckEvery = checkEvery;
    }

    /// <summary>The wait before the first pass after the app starts, so startup stays fast.</summary>
    public TimeSpan StartupDelay { get; }

    /// <summary>How often the scheduler looks at the clock, the accounts and the schedule.</summary>
    public TimeSpan CheckEvery { get; }
}
