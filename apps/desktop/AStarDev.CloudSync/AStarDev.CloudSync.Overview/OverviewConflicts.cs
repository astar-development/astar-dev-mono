using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Overview;

/// <summary>Reads every account's open conflicts (#1773) for the overview. Held deletions are not conflicts and are left out. An account whose open items cannot be read is warned about without details and counts as having none.</summary>
/// <param name="reader">Reads each account's open attention items.</param>
/// <param name="logger">Reports open items that could not be read.</param>
internal sealed class OverviewConflicts(ISyncAttentionReader reader, ILogger<OverviewConflicts> logger)
{
    /// <summary>Reads the open conflicts of the accounts.</summary>
    /// <param name="accountIds">The accounts to read.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    public async Task<IReadOnlyList<SyncAttentionItem>> ReadAsync(IReadOnlyList<AccountId> accountIds, CancellationToken cancellationToken)
    {
        List<SyncAttentionItem> conflicts = [];

        foreach (var accountId in accountIds)
            conflicts.AddRange(await reader.GetOpenAsync(accountId, cancellationToken).MatchAsync(ConflictsOf, _ => Unreadable()).ConfigureAwait(false));

        return conflicts;
    }

    private static IReadOnlyList<SyncAttentionItem> ConflictsOf(IReadOnlyList<SyncAttentionItem> items) => [.. items.OfType<SyncAttentionItem.Conflict>()];

    private IReadOnlyList<SyncAttentionItem> Unreadable()
    {
        LogMessage.Warning(logger, nameof(OverviewConflicts), "An account's open conflicts could not be read and are shown as none.");

        return [];
    }
}
