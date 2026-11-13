using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Adds the attention queue to the resync signal: open conflicts read as <see cref="PathQuarantineState.Conflicted"/> and held deletions as <see cref="PathQuarantineState.HeldByGuard"/> (#1405).</summary>
public sealed class AttentionPathQuarantine(ResyncPathQuarantine resync, ISyncAttentionReader attentionReader) : IPathQuarantine
{
    /// <inheritdoc />
    public Task<Result<QuarantineSnapshot, ErrorResponse>> ReadAsync(AccountId accountId, CancellationToken cancellationToken) =>
        attentionReader is null ? throw new NotImplementedException($"{nameof(attentionReader)} is not yet read.") : resync.ReadAsync(accountId, cancellationToken);
}
