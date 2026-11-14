namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncSchedulerOptions"/> values.</summary>
public static class SyncSchedulerOptionsFactory
{
    private static readonly TimeSpan DefaultCheckEvery = TimeSpan.FromSeconds(30);

    /// <summary>Creates the default options: a 2 minute startup delay and a check every 30 seconds.</summary>
    public static SyncSchedulerOptions CreateDefault() => new(TimeSpan.FromMinutes(2), DefaultCheckEvery);

    /// <summary>Creates options. A negative startup delay becomes zero; a check period of zero or less becomes 30 seconds.</summary>
    /// <param name="startupDelay">The wait before the first pass.</param>
    /// <param name="checkEvery">How often the scheduler looks at the clock.</param>
    public static SyncSchedulerOptions Create(TimeSpan startupDelay, TimeSpan checkEvery) => new(startupDelay < TimeSpan.Zero ? TimeSpan.Zero : startupDelay, checkEvery > TimeSpan.Zero ? checkEvery : DefaultCheckEvery);
}
