namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="QuarantineSnapshot"/> values.</summary>
public static class QuarantineSnapshotFactory
{
    /// <summary>Creates a snapshot. When two entries name the same path ignoring case, a non-clear state beats a clear one; otherwise the first is kept.</summary>
    /// <param name="entries">The stored path states.</param>
    public static QuarantineSnapshot Create(IEnumerable<KeyValuePair<RelativeSyncPath, PathQuarantineState>> entries)
    {
        var byPath = new Dictionary<RelativeSyncPath, PathQuarantineState>(RelativeSyncPathComparer.Instance);
        foreach (var entry in (entries ?? []).Where(entry => !byPath.TryGetValue(entry.Key, out var kept) || kept.AllowsSyncAction()))
            byPath[entry.Key] = entry.Value;

        return new QuarantineSnapshot(byPath.AsReadOnly());
    }

    /// <summary>Creates a snapshot for an account whose resync is running: every path with no stronger stored state, and not skipped, reads as <see cref="PathQuarantineState.ResyncInProgress"/>.</summary>
    /// <param name="entries">The stored path states.</param>
    public static QuarantineSnapshot CreateResyncing(IEnumerable<KeyValuePair<RelativeSyncPath, PathQuarantineState>> entries) => throw new NotImplementedException($"{nameof(entries)} are not yet read.");

    /// <summary>Creates a snapshot with no stored states.</summary>
    public static QuarantineSnapshot CreateEmpty() => Create([]);
}
