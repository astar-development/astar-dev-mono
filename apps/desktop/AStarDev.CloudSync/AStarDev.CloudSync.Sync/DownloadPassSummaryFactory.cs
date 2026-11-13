namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="DownloadPassSummary"/> values.</summary>
public static class DownloadPassSummaryFactory
{
    /// <summary>Creates the summary. The items the guard held count as <see cref="DownloadPassSummary.HeldByGuard"/> whether or not the caller listed them as outcomes.</summary>
    /// <param name="outcomes">One outcome per executed item.</param>
    /// <param name="held">The pairs whose deletions the guard held.</param>
    /// <returns>The summary.</returns>
    public static DownloadPassSummary Create(IEnumerable<DownloadItemOutcome> outcomes, IEnumerable<HeldLocalDeletions> held) => new([.. outcomes ?? []], [.. held ?? []]);
}
