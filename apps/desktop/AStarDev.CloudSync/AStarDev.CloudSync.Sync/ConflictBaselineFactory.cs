namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ConflictBaseline"/> values.</summary>
public static class ConflictBaselineFactory
{
    /// <summary>Creates the baseline.</summary>
    /// <param name="localHash">The local SHA-256 at sync time.</param>
    /// <param name="remoteTag">The cloud content tag at sync time.</param>
    public static ConflictBaseline Create(ContentHash localHash, ContentTag remoteTag) => new(localHash, remoteTag);
}
