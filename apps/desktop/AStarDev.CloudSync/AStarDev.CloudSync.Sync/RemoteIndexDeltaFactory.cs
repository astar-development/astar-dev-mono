namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RemoteIndexDelta"/> values.</summary>
public static class RemoteIndexDeltaFactory
{
    /// <summary>Creates a delta. Missing changes become an empty list.</summary>
    public static RemoteIndexDelta Create(IEnumerable<RemoteChange>? changes) => new([.. changes ?? []]);
}
