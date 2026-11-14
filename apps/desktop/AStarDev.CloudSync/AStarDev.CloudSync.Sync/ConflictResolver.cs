using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
internal sealed class ConflictResolver : IConflictResolver
{
    /// <inheritdoc />
    public Task<Result<ResolutionSummary, ErrorResponse>> ResolveAsync(AccountId accountId, IReadOnlyList<ConflictId> ids, ConflictStrategy strategy, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{nameof(ResolveAsync)} {accountId} {ids} {strategy} {cancellationToken}");
}
