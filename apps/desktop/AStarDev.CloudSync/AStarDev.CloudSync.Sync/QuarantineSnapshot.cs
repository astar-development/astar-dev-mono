namespace AStarDev.CloudSync.Sync;

/// <summary>The quarantined paths of one account at the start of a pass. Paths are relative to the account's sync root and matched ignoring case. A path with no entry is clear unless the skip filter matches it.</summary>
public sealed record QuarantineSnapshot
{
    internal QuarantineSnapshot(IReadOnlyDictionary<RelativeSyncPath, PathQuarantineState> entries, PathQuarantineState baseline)
    {
        Entries = entries;
        Baseline = baseline;
    }

    /// <summary>The stored states, keyed ignoring case.</summary>
    public IReadOnlyDictionary<RelativeSyncPath, PathQuarantineState> Entries { get; }

    /// <summary>The state of a path that is not skipped and has no stronger stored state: <see cref="PathQuarantineState.Clear"/>, or <see cref="PathQuarantineState.ResyncInProgress"/> while the account's resync runs.</summary>
    public PathQuarantineState Baseline { get; }
}
