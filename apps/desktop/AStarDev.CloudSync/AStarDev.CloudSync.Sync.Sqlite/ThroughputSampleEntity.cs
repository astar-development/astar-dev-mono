namespace AStarDev.CloudSync.Sync.Sqlite;

internal sealed class ThroughputSampleEntity
{
    public long Id { get; set; }

    public long AtUtcTicks { get; set; }

    public long Bytes { get; set; }

    public long ElapsedTicks { get; set; }
}
