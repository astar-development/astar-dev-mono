using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Keeps the attention queue in step with the download guard (#1400, #1551). The sync pass runner saves the pairs a plan holds as held sets of local deletions (#1534). An open set of local deletions whose pair the guard would no longer hold is closed, so its paths stop reading as held: the pair is planned again with those paths released, and the set is closed only when that plan does not hold the pair. Sets of remote deletions belong to the local scan and are never closed here. Nothing is closed while a resync is in progress, because deletions cannot be judged then.</summary>
/// <param name="attentionReader">Reads the open held sets.</param>
/// <param name="attentionCloser">Closes the held set of a pair that has recovered.</param>
/// <param name="planner">Judges whether a pair would still be held.</param>
internal sealed class DownloadHolds(ISyncAttentionReader attentionReader, ISyncAttentionCloser attentionCloser, IDownloadPlanner planner)
{
    internal Task<Result<Unit, ErrorResponse>> SettleAsync(AccountId accountId, DownloadPlanRequest request, DownloadPlan plan, CancellationToken cancellationToken) =>
        CloseRecoveredAsync(accountId, request, plan, cancellationToken);

    private static Result<Unit, ErrorResponse> Done() => Result.Success<Unit, ErrorResponse>(Unit.Instance);

    private Task<Result<Unit, ErrorResponse>> CloseRecoveredAsync(AccountId accountId, DownloadPlanRequest request, DownloadPlan plan, CancellationToken cancellationToken) =>
        request.Quarantine.Baseline is PathQuarantineState.Clear
            ? attentionReader.GetOpenAsync(accountId, cancellationToken).BindAsync(open => CloseAsync(Recovered(open, request, plan), cancellationToken))
            : Task.FromResult(Done());

    private IReadOnlyList<SyncAttentionItem.HeldDeletions> Recovered(IReadOnlyList<SyncAttentionItem> open, DownloadPlanRequest request, DownloadPlan plan)
    {
        var heldNow = plan.Held.Select(held => held.PairId).ToHashSet();
        var candidates = open.OfType<SyncAttentionItem.HeldDeletions>().Where(held => held.Direction is DeletionDirection.DeleteLocal && !heldNow.Contains(held.PairId)).ToList();
        if (candidates.Count == 0)
            return [];

        var released = candidates.SelectMany(held => held.Paths).ToHashSet(RelativeSyncPathComparer.Instance);
        var quarantine = QuarantineSnapshotFactory.Create(request.Quarantine.Entries.Where(entry => entry.Value is not PathQuarantineState.HeldByGuard || !released.Contains(entry.Key)));
        var stillHeld = planner.Plan(DownloadPlanRequestFactory.Create(request.Remote, request.Baseline, request.Local, request.Rules, quarantine, request.MissingLocally)).Held.Select(held => held.PairId).ToHashSet();

        return [.. candidates.Where(held => !stillHeld.Contains(held.PairId))];
    }

    private async Task<Result<Unit, ErrorResponse>> CloseAsync(IReadOnlyList<SyncAttentionItem.HeldDeletions> recovered, CancellationToken cancellationToken)
    {
        foreach (var stale in recovered)
        {
            var closed = await attentionCloser.CloseAsync(stale.Id, AttentionResolutionFactory.CreateRestoreItems(), cancellationToken).ConfigureAwait(false);
            if (closed.Match(_ => false, _ => true))
                return closed;
        }

        return Done();
    }
}
