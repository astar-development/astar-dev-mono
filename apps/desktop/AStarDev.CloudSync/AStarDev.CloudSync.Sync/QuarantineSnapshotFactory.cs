namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="QuarantineSnapshot"/> values.</summary>
public static class QuarantineSnapshotFactory
{
    /// <summary>Creates a snapshot. When two entries name the same path ignoring case, the first is kept.</summary>
    /// <param name="entries">The stored path states.</param>
    public static QuarantineSnapshot Create(IEnumerable<KeyValuePair<RelativeSyncPath, PathQuarantineState>> entries)
    {
        var byPath = new Dictionary<RelativeSyncPath, PathQuarantineState>(RelativeSyncPathComparer.Instance);
        foreach (var entry in entries ?? [])
            byPath.TryAdd(entry.Key, entry.Value);

        return new QuarantineSnapshot(byPath.AsReadOnly());
    }

    /// <summary>Creates a snapshot with no stored states.</summary>
    public static QuarantineSnapshot CreateEmpty() => Create([]);
}
