namespace AStarDev.CloudSync.Sync;

/// <summary>Reads a path's state from a <see cref="QuarantineSnapshot"/>.</summary>
public static class QuarantineSnapshotExtensions
{
    /// <summary>The state of the path: <see cref="PathQuarantineState.Skipped"/> when <see cref="SyncSkipFilter"/> matches it, otherwise its stored state, otherwise <see cref="PathQuarantineState.Clear"/>.</summary>
    /// <param name="snapshot">The account's snapshot.</param>
    /// <param name="path">The path relative to the sync root.</param>
    public static PathQuarantineState StateOf(this QuarantineSnapshot snapshot, RelativeSyncPath path)
    {
        if (SyncSkipFilter.IsSkipped(path))
            return PathQuarantineStateFactory.CreateSkipped();

        return snapshot.Entries.TryGetValue(path, out var stored) ? stored : PathQuarantineStateFactory.CreateClear();
    }
}
