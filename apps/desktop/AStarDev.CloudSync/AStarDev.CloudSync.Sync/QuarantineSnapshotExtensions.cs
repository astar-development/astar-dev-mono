namespace AStarDev.CloudSync.Sync;

/// <summary>Reads a path's state from a <see cref="QuarantineSnapshot"/>.</summary>
public static class QuarantineSnapshotExtensions
{
    private const char Separator = '/';

    /// <summary>The state of the path: <see cref="PathQuarantineState.Skipped"/> when the path is empty or <see cref="SyncSkipFilter"/> matches it; otherwise the first non-clear state stored for the path itself or, walking up, for one of its folders; otherwise <see cref="PathQuarantineState.Clear"/>.</summary>
    /// <param name="snapshot">The account's snapshot.</param>
    /// <param name="path">The path relative to the sync root.</param>
    public static PathQuarantineState StateOf(this QuarantineSnapshot snapshot, RelativeSyncPath path)
    {
        if (string.IsNullOrEmpty(path.Value) || SyncSkipFilter.IsSkipped(path))
            return PathQuarantineStateFactory.CreateSkipped();

        return SelfAndAncestors(path.Value)
            .Select(candidate => snapshot.Entries.TryGetValue(new RelativeSyncPath(candidate), out var stored) ? stored : PathQuarantineStateFactory.CreateClear())
            .FirstOrDefault(state => !state.AllowsSyncAction(), PathQuarantineStateFactory.CreateClear());
    }

    private static IEnumerable<string> SelfAndAncestors(string path)
    {
        for (var end = path.Length; end > 0; end = path.LastIndexOf(Separator, end - 1))
            yield return path[..end];
    }
}
