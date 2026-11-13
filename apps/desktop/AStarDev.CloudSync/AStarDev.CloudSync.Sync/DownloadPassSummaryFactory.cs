namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="DownloadPassSummary"/> values.</summary>
public static class DownloadPassSummaryFactory
{
    /// <summary>Counts the outcomes. The items the guard held count as <see cref="DownloadPassSummary.HeldByGuard"/> whether or not the caller listed them as outcomes.</summary>
    /// <param name="outcomes">One outcome per executed item.</param>
    /// <param name="held">The pairs whose deletions the guard held.</param>
    /// <returns>The summary.</returns>
    public static DownloadPassSummary Create(IEnumerable<DownloadItemOutcome> outcomes, IEnumerable<HeldLocalDeletions> held)
    {
        IReadOnlyList<DownloadItemOutcome> all = [.. outcomes ?? []];
        IReadOnlyList<HeldLocalDeletions> heldPairs = [.. held ?? []];
        var guardHeld = Math.Max(all.OfType<DownloadItemOutcome.HeldByGuard>().Count(), heldPairs.Sum(pair => pair.Paths.Count));

        return new DownloadPassSummary(
            all.OfType<DownloadItemOutcome.Downloaded>().Count(),
            all.OfType<DownloadItemOutcome.Downloaded>().Sum(outcome => outcome.Bytes),
            all.OfType<DownloadItemOutcome.FolderCreated>().Count(),
            all.OfType<DownloadItemOutcome.Moved>().Count(),
            all.OfType<DownloadItemOutcome.DeletedLocally>().Count(),
            guardHeld,
            all.OfType<DownloadItemOutcome.SentToConflicts>().Count(),
            all.OfType<DownloadItemOutcome.Skipped>().Count(),
            all.OfType<DownloadItemOutcome.Failed>().Count(),
            heldPairs);
    }
}
