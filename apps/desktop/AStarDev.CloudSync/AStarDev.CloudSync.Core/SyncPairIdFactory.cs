namespace AStarDev.CloudSync.Core;

/// <summary>Creates <see cref="SyncPairId"/> values.</summary>
public static class SyncPairIdFactory
{
    /// <summary>Creates a new, unique sync pair id.</summary>
    public static SyncPairId Create() => new(Guid.NewGuid());
}
