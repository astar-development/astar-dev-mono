namespace AStarDev.CloudSync.Sync.Sqlite;

internal sealed class SyncScheduleEntity
{
    public int Id { get; set; }

    public int? IntervalMinutes { get; set; }
}
