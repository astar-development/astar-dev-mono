namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncPassId"/> values.</summary>
public static class SyncPassIdFactory
{
    /// <summary>Creates a pass id.</summary>
    public static SyncPassId Create(Guid value) => new(value);
}
