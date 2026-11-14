namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ConflictFinding"/> values.</summary>
public static class ConflictFindingFactory
{
    /// <summary>Creates a finding.</summary>
    /// <param name="path">The conflicted path.</param>
    /// <param name="kind">Why it conflicts.</param>
    /// <param name="local">What the local side held.</param>
    /// <param name="remote">What the cloud held.</param>
    public static ConflictFinding Create(RelativeSyncPath path, ConflictKind kind, SideSnapshot local, SideSnapshot remote) => new(path, kind, local, remote);
}
