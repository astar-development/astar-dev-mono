namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="LocalScanSummary"/> values.</summary>
public static class LocalScanSummaryFactory
{
    /// <summary>Counts the changes by kind. Negative <paramref name="touched"/> and <paramref name="hashed"/> become zero.</summary>
    /// <param name="changes">The changes.</param>
    /// <param name="touched">How many files were only touched.</param>
    /// <param name="hashed">How many files were hashed.</param>
    public static LocalScanSummary Create(IEnumerable<LocalChange> changes, int touched, int hashed)
    {
        var list = (changes ?? []).ToList();

        return new LocalScanSummary(list.Count(change => change is LocalChange.Added), list.Count(change => change is LocalChange.Modified), list.Count(change => change is LocalChange.Deleted), list.Count(change => change is LocalChange.Deferred), list.Count(change => change is LocalChange.Unreadable), Math.Max(touched, 0), Math.Max(hashed, 0));
    }
}
