namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="QuarantineSnapshot"/> values.</summary>
public static class QuarantineSnapshotFactory
{
    /// <summary>Creates a snapshot. When two entries name the same path ignoring case, a non-clear state beats a clear one; otherwise the first is kept.</summary>
    /// <param name="entries">The stored path states.</param>
    public static QuarantineSnapshot Create(IEnumerable<KeyValuePair<RelativeSyncPath, PathQuarantineState>> entries) => Build(entries, PathQuarantineStateFactory.CreateClear());

    private static QuarantineSnapshot Build(IEnumerable<KeyValuePair<RelativeSyncPath, PathQuarantineState>> entries, PathQuarantineState baseline)
    {
        var byPath = new Dictionary<RelativeSyncPath, PathQuarantineState>(RelativeSyncPathComparer.Instance);
        foreach (var entry in (entries ?? []).Where(entry => !byPath.TryGetValue(entry.Key, out var kept) || kept.AllowsSyncAction()))
            byPath[entry.Key] = entry.Value;

        return new QuarantineSnapshot(byPath.AsReadOnly(), baseline);
    }

    /// <summary>Creates a snapshot for an account whose resync is running: every path with no stronger stored state, and not skipped, reads as <see cref="PathQuarantineState.ResyncInProgress"/>. Entries are merged as in <see cref="Create"/>.</summary>
    /// <param name="entries">The stored path states.</param>
    public static QuarantineSnapshot CreateResyncing(IEnumerable<KeyValuePair<RelativeSyncPath, PathQuarantineState>> entries) => Build(entries, PathQuarantineStateFactory.CreateResyncInProgress());

    /// <summary>Creates a snapshot that keeps the basis' <see cref="QuarantineSnapshot.Baseline"/> and stored states and adds more states. A stronger stored state beats a clear one; between two non-clear states the basis' entry is kept.</summary>
    /// <param name="basis">The snapshot to extend.</param>
    /// <param name="entries">The states to add.</param>
    public static QuarantineSnapshot Extend(QuarantineSnapshot basis, IEnumerable<KeyValuePair<RelativeSyncPath, PathQuarantineState>> entries) => Build(basis.Entries.Concat(entries ?? []), basis.Baseline);

    /// <summary>Creates a snapshot with no stored states.</summary>
    public static QuarantineSnapshot CreateEmpty() => Create([]);
}
