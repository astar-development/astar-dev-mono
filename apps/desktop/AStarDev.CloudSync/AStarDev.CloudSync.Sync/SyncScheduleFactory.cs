namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncSchedule"/> values. The intervals are a fixed list.</summary>
public static class SyncScheduleFactory
{
    /// <summary>The interval used when none was chosen, and when a requested interval is not in the list: one hour.</summary>
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromHours(1);

    /// <summary>The intervals the user can choose: 15 minutes, 30 minutes, 1 hour, 2 hours and 4 hours.</summary>
    public static IReadOnlyList<TimeSpan> Intervals { get; } = [TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30), TimeSpan.FromHours(1), TimeSpan.FromHours(2), TimeSpan.FromHours(4)];

    /// <summary>Creates an every-interval schedule. An interval that is not in <see cref="Intervals"/> becomes <see cref="DefaultInterval"/>.</summary>
    /// <param name="interval">The time between two syncs of one account.</param>
    public static SyncSchedule CreateEvery(TimeSpan interval) => throw new NotImplementedException($"{interval}");

    /// <summary>Creates the manual-only schedule.</summary>
    public static SyncSchedule CreateManualOnly() => new SyncSchedule.ManualOnly();

    /// <summary>Creates the default schedule: every hour.</summary>
    public static SyncSchedule CreateDefault() => throw new NotImplementedException();
}
