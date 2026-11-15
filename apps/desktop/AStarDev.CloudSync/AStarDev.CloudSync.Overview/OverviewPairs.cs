using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Overview;

/// <summary>Gives the overview's sync pairs their real state (#1774). A pair's file count is the baseline files under its folder, its last sync is its account's, and its status is the account's syncing, failed or paused state; otherwise the open conflicts under the pair's folder decide between has-conflicts and up to date. A file count that cannot be read is warned about without details and shown as no files.</summary>
/// <param name="counter">Counts the baseline files under a folder.</param>
/// <param name="logger">Reports file counts that could not be read.</param>
internal sealed class OverviewPairs(ISyncBaselineCounter counter, ILogger<OverviewPairs> logger)
{
    /// <summary>Returns the pairs with their state filled in.</summary>
    /// <param name="pairs">The account's pairs as stored.</param>
    /// <param name="accountStatus">The account's status.</param>
    /// <param name="lastSync">When the account last synced.</param>
    /// <param name="conflicts">The account's open conflicts.</param>
    /// <param name="cancellationToken">Cancels the counts.</param>
    public async Task<IReadOnlyList<SyncPair>> StateAsync(IReadOnlyList<SyncPair> pairs, SyncStatus accountStatus, Option<DateTimeOffset> lastSync, IReadOnlyList<SyncAttentionItem.Conflict> conflicts, CancellationToken cancellationToken)
    {
        List<SyncPair> stated = [];

        foreach (var pair in pairs)
            stated.Add(SyncPairFactory.WithState(pair, SyncPairStateFactory.Create(StatusOf(pair, accountStatus, conflicts), await FilesAsync(pair, cancellationToken).ConfigureAwait(false), lastSync)));

        return stated;
    }

    private static SyncStatus StatusOf(SyncPair pair, SyncStatus accountStatus, IReadOnlyList<SyncAttentionItem.Conflict> conflicts)
    {
        if (accountStatus is not (SyncStatus.UpToDate or SyncStatus.HasConflicts))
            return accountStatus;

        var inside = conflicts.Count(conflict => IsInside(conflict.Path.Value, pair.LocalFolder.Value));

        return inside > 0 ? SyncStatusFactory.CreateHasConflicts(inside) : SyncStatusFactory.CreateUpToDate();
    }

    private static bool IsInside(string? path, string? folder)
    {
        var inner = path ?? string.Empty;
        var outer = folder ?? string.Empty;

        return outer.Length == 0 || string.Equals(inner, outer, StringComparison.OrdinalIgnoreCase) || inner.StartsWith(outer + "/", StringComparison.OrdinalIgnoreCase);
    }

    private Task<int> FilesAsync(SyncPair pair, CancellationToken cancellationToken) =>
        counter.CountFilesUnderAsync(pair.AccountId, pair.LocalFolder, cancellationToken).MatchAsync(files => files, _ => Unreadable());

    private int Unreadable()
    {
        LogMessage.Warning(logger, nameof(OverviewPairs), "A sync pair's file count could not be read and is shown as no files.");

        return 0;
    }
}
