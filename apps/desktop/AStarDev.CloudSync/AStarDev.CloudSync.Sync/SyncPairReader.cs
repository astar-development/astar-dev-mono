using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="rules">Reads the account's selection rules.</param>
public sealed class SyncPairReader(ISelectionRuleReader rules) : ISyncPairReader
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<SyncPair>, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        _ = rules;

        return Task.FromResult(Result.Success<IReadOnlyList<SyncPair>, ErrorResponse>([]));
    }
}
