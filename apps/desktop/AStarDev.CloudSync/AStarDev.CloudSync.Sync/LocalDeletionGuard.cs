using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Applies the per-pair mass-deletion guard to the deletions a local scan found (#1399). A pair that trips the guard gets no Deleted change; its missing paths are saved as a held set in the attention queue, so they read as held in the path quarantine, and the pair is listed in the result. Other pairs report their deletions as usual. A pair that is below the threshold again has its open held set for local deletions closed, so its paths stop reading as held (#1540). A hold that cannot be saved, an open set that cannot be read or a set that cannot be closed fails the scan, so no deletion leaks past the guard.</summary>
/// <param name="finder">Finds the baseline items that are gone.</param>
/// <param name="attentionWriter">Records the held sets.</param>
/// <param name="attentionReader">Reads the open held sets.</param>
/// <param name="attentionCloser">Closes the held set of a pair that has recovered.</param>
/// <param name="time">Stamps the held sets.</param>
internal sealed class LocalDeletionGuard(LocalDeletionFinder finder, ISyncAttentionWriter attentionWriter, ISyncAttentionReader attentionReader, ISyncAttentionCloser attentionCloser, TimeProvider time)
{
    internal async Task<GuardedDeletions> FindAsync(AccountId accountId, IDirectoryInfo root, IReadOnlyList<SelectionRule> rules, IReadOnlySet<string> blocked, CancellationToken cancellationToken)
    {
        var scan = await finder.FindAsync(accountId, root, rules, blocked, cancellationToken).ConfigureAwait(false);
        var held = scan.Pending.GroupBy(deletion => deletion.Pair).Where(group => MassDeletionGuard.Evaluate(scan.Totals.GetValueOrDefault(group.Key), group.Count()) is MassDeletionVerdict.Held).ToList();
        var heldPairs = held.Select(group => group.Key).ToHashSet();
        var items = held.Select(group => SyncAttentionItemFactory.CreateHeldDeletions(new HeldDeletionId(Guid.NewGuid()), accountId, group.Key, DeletionDirectionFactory.CreateDeleteRemote(), group.Select(deletion => deletion.Path), time.GetUtcNow())).ToList();
        if (items.Count > 0)
            (await attentionWriter.RecordAsync(items, cancellationToken).ConfigureAwait(false)).OrThrow();

        await CloseRecoveredAsync(accountId, heldPairs, cancellationToken).ConfigureAwait(false);

        var deleted = scan.Pending.Where(deletion => !heldPairs.Contains(deletion.Pair)).Select(deletion => LocalChangeFactory.CreateDeleted(deletion.Path, deletion.ItemId));
        var holds = held.Select(group => MassDeletionSuspectedFactory.Create(group.Key, group.Count(), scan.Totals.GetValueOrDefault(group.Key)));

        return new GuardedDeletions([.. scan.Unreadable, .. deleted], [.. holds]);
    }

    private async Task CloseRecoveredAsync(AccountId accountId, HashSet<SyncPairId> heldPairs, CancellationToken cancellationToken)
    {
        var open = (await attentionReader.GetOpenAsync(accountId, cancellationToken).ConfigureAwait(false)).OrThrow();
        foreach (var stale in open.OfType<SyncAttentionItem.HeldDeletions>().Where(held => held.Direction is DeletionDirection.DeleteRemote && !heldPairs.Contains(held.PairId)))
            (await attentionCloser.CloseAsync(stale.Id, AttentionResolutionFactory.CreateRestoreItems(), cancellationToken).ConfigureAwait(false)).OrThrow();
    }
}
