namespace AStarDev.CloudSync.Sync;

/// <summary>Reads on <see cref="DownloadPassSummary"/>.</summary>
public static class DownloadPassSummaryExtensions
{
    /// <summary>Whether the pass changed anything on disk or held something back. A pass that only deleted or moved items has changes, so it never reads as "No changes" (#1155).</summary>
    /// <param name="summary">The summary.</param>
    /// <returns>True when anything was downloaded, created, moved, deleted, held or sent to the attention queue.</returns>
    public static bool HasChanges(this DownloadPassSummary summary) => summary.Downloaded + summary.FoldersCreated + summary.Moved + summary.DeletedLocally + summary.HeldByGuard + summary.SentToConflicts > 0;
}
