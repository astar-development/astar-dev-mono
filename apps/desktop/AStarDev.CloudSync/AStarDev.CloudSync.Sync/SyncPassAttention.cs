using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>The sync run's part of the attention queue (#1534): it closes conflicts that settled themselves before the passes plan, and records what the passes found afterwards.</summary>
/// <param name="reader">Reads the open conflicts.</param>
/// <param name="writer">Records conflicts and held deletions in one write.</param>
/// <param name="closer">Closes a conflict that settled itself.</param>
/// <param name="baseline">Reads the baseline row of a conflicted path.</param>
/// <param name="remoteIndex">Reads the cloud item the baseline row points at.</param>
/// <param name="time">Stamps what the passes found.</param>
/// <param name="logger">Reports how many conflicts were closed, with no paths.</param>
internal sealed class SyncPassAttention(ISyncAttentionReader reader, ISyncAttentionWriter writer, ISyncAttentionCloser closer, ISyncBaselineReader baseline, IRemoteIndexReader remoteIndex, TimeProvider time, ILogger<SyncPassAttention> logger)
{
    private const string Location = nameof(SyncPassAttention);

    internal async Task<Result<Unit, ErrorResponse>> ClearAsync(AccountId accountId, LocalChangeSet changes, CancellationToken cancellationToken)
    {
        var changed = changes.Changes.Select(change => change.Path).ToHashSet(RelativeSyncPathComparer.Instance);
        var open = await reader.GetOpenAsync(accountId, cancellationToken).ConfigureAwait(false);

        return await open.BindAsync(items => CloseSettledAsync(accountId, [.. items.OfType<SyncAttentionItem.Conflict>()], changed, cancellationToken)).ConfigureAwait(false);
    }

    internal Task<Result<Unit, ErrorResponse>> RecordAsync(AccountId accountId, IReadOnlyList<ConflictFinding> conflicts, IReadOnlyList<HeldLocalDeletions> held, CancellationToken cancellationToken)
    {
        var detectedAt = time.GetUtcNow();
        List<SyncAttentionItem> items =
        [
            .. conflicts.Select(finding => SyncAttentionItemFactory.CreateConflict(new ConflictId(Guid.NewGuid()), accountId, finding.Path, finding.Kind, finding.Local, finding.Remote, detectedAt)),
            .. held.Select(set => SyncAttentionItemFactory.CreateHeldDeletions(new HeldDeletionId(Guid.NewGuid()), accountId, set.PairId, DeletionDirectionFactory.CreateDeleteLocal(), set.Paths, detectedAt)),
        ];

        return items.Count == 0 ? Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance)) : writer.RecordAsync(items, cancellationToken);
    }

    private async Task<Result<Unit, ErrorResponse>> CloseSettledAsync(AccountId accountId, IReadOnlyList<SyncAttentionItem.Conflict> conflicts, HashSet<RelativeSyncPath> changed, CancellationToken cancellationToken)
    {
        var closedCount = 0;
        foreach (var conflict in conflicts)
        {
            var settled = await HasSettledAsync(accountId, conflict, changed, cancellationToken).ConfigureAwait(false);
            var closed = await settled.BindAsync(done => done ? closer.CloseAsync(conflict.Id, AttentionResolutionFactory.CreateAutoCleared(), cancellationToken).MatchAsync(_ => Result.Success<bool, ErrorResponse>(true), Result.Failure<bool, ErrorResponse>) : Task.FromResult(Result.Success<bool, ErrorResponse>(false))).ConfigureAwait(false);
            var step = closed.Match(wasClosed => Result.Success<int, ErrorResponse>(wasClosed ? 1 : 0), Result.Failure<int, ErrorResponse>);
            if (step.Match(_ => false, _ => true))
                return step.Match(_ => Result.Success<Unit, ErrorResponse>(Unit.Instance), Result.Failure<Unit, ErrorResponse>);

            closedCount += step.Match(count => count, _ => 0);
        }

        LogCleared(closedCount);

        return Result.Success<Unit, ErrorResponse>(Unit.Instance);
    }

    private void LogCleared(int count)
    {
        if (count == 0)
            return;

        var message = $"Auto-cleared {count} conflicts because one side returned to the baseline.";
        LogMessage.Information(logger, Location, message);
    }

    private async Task<Result<bool, ErrorResponse>> HasSettledAsync(AccountId accountId, SyncAttentionItem.Conflict conflict, HashSet<RelativeSyncPath> changed, CancellationToken cancellationToken)
    {
        var entry = await baseline.GetAsync(accountId, conflict.Path, cancellationToken).ConfigureAwait(false);

        return await entry.BindAsync(found => found.Match(
            known => known.Kind is SyncBaselineKind.File file ? SideReturnedAsync(accountId, conflict, known, file, changed, cancellationToken) : Task.FromResult(Result.Success<bool, ErrorResponse>(false)),
            () => Task.FromResult(Result.Success<bool, ErrorResponse>(false)))).ConfigureAwait(false);
    }

    private Task<Result<bool, ErrorResponse>> SideReturnedAsync(AccountId accountId, SyncAttentionItem.Conflict conflict, SyncBaselineEntry known, SyncBaselineKind.File file, HashSet<RelativeSyncPath> changed, CancellationToken cancellationToken) =>
        conflict.Local is SideSnapshot.Present && !changed.Contains(conflict.Path)
            ? Task.FromResult(Result.Success<bool, ErrorResponse>(true))
            : remoteIndex.GetAsync(accountId, known.ItemId, cancellationToken).MatchAsync(item => Result.Success<bool, ErrorResponse>(item.Match(found => found.Kind is RemoteItemKind.File current && current.CTag == file.CTag, () => false)), Result.Failure<bool, ErrorResponse>);
}
