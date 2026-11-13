using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Applies the per-pair mass-deletion guard to the deletions a local scan found (#1399). A pair that trips the guard gets no Deleted change; its missing paths are saved as a held set in the attention queue, so they read as held in the path quarantine, and the pair is listed in the result. Other pairs report their deletions as usual. A hold that cannot be saved fails the scan, so no deletion leaks past the guard.</summary>
/// <param name="finder">Finds the baseline items that are gone.</param>
/// <param name="attentionWriter">Records the held sets.</param>
/// <param name="time">Stamps the held sets.</param>
internal sealed class LocalDeletionGuard(LocalDeletionFinder finder, ISyncAttentionWriter attentionWriter, TimeProvider time)
{
    internal async Task<GuardedDeletions> FindAsync(AccountId accountId, IDirectoryInfo root, IReadOnlyList<SelectionRule> rules, IReadOnlySet<string> blocked, CancellationToken cancellationToken)
    {
        var scan = await finder.FindAsync(accountId, root, rules, blocked, cancellationToken).ConfigureAwait(false);
        var held = scan.Pending.GroupBy(deletion => deletion.Pair).Where(group => MassDeletionGuard.Evaluate(scan.Totals.GetValueOrDefault(group.Key), group.Count()) is MassDeletionVerdict.Held).ToList();
        var heldPairs = held.Select(group => group.Key).ToHashSet();
        var items = held.Select(group => SyncAttentionItemFactory.CreateHeldDeletions(new HeldDeletionId(Guid.NewGuid()), accountId, group.Key, DeletionDirectionFactory.CreateDeleteRemote(), group.Select(deletion => deletion.Path), time.GetUtcNow())).ToList();
        if (items.Count > 0)
            (await attentionWriter.RecordAsync(items, cancellationToken).ConfigureAwait(false)).OrThrow();

        var deleted = scan.Pending.Where(deletion => !heldPairs.Contains(deletion.Pair)).Select(deletion => LocalChangeFactory.CreateDeleted(deletion.Path, deletion.ItemId));
        var holds = held.Select(group => MassDeletionSuspectedFactory.Create(group.Key, group.Count(), scan.Totals.GetValueOrDefault(group.Key)));

        return new GuardedDeletions([.. scan.Unreadable, .. deleted], [.. holds]);
    }
}
