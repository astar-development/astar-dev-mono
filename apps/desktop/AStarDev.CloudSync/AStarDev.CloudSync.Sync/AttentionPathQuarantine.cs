using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Adds the attention queue to the resync signal: open conflicts read as <see cref="PathQuarantineState.Conflicted"/> and held deletions as <see cref="PathQuarantineState.HeldByGuard"/> (#1405). A path that is both reads as conflicted. A read that fails is returned as the error, never as clear.</summary>
public sealed class AttentionPathQuarantine(ResyncPathQuarantine resync, ISyncAttentionReader attentionReader) : IPathQuarantine
{
    /// <inheritdoc />
    public async Task<Result<QuarantineSnapshot, ErrorResponse>> ReadAsync(AccountId accountId, CancellationToken cancellationToken) =>
        await resync.ReadAsync(accountId, cancellationToken)
            .BindAsync(basis => attentionReader.GetOpenAsync(accountId, cancellationToken).MatchAsync(items => Result.Success<QuarantineSnapshot, ErrorResponse>(QuarantineSnapshotFactory.Extend(basis, Entries(items))), Result.Failure<QuarantineSnapshot, ErrorResponse>))
            .ConfigureAwait(false);

    private static IEnumerable<KeyValuePair<RelativeSyncPath, PathQuarantineState>> Entries(IReadOnlyList<SyncAttentionItem> items) =>
        items.OfType<SyncAttentionItem.Conflict>().Select(conflict => KeyValuePair.Create(conflict.Path, PathQuarantineStateFactory.CreateConflicted()))
            .Concat(items.OfType<SyncAttentionItem.HeldDeletions>().SelectMany(held => held.Paths).Select(path => KeyValuePair.Create(path, PathQuarantineStateFactory.CreateHeldByGuard())));
}
