namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="LocalChangeSet"/> values.</summary>
public static class LocalChangeSetFactory
{
    /// <summary>Creates a set. The changes are sorted by path ignoring case; when two changes name the same path ignoring case, the first is kept (the sync pipeline rule).</summary>
    /// <param name="changes">The changes.</param>
    /// <param name="touched">How many files were only touched.</param>
    /// <param name="hashed">How many files were hashed.</param>
    public static LocalChangeSet Create(IEnumerable<LocalChange> changes, int touched, int hashed)
    {
        IReadOnlyList<LocalChange> unique = [.. (changes ?? []).GroupBy(change => change.Path).Select(group => group.First()).OrderBy(change => change.Path.Value, StringComparer.OrdinalIgnoreCase)];

        return new LocalChangeSet(unique, LocalScanSummaryFactory.Create(unique, touched, hashed));
    }
}
