using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="reader">Reads every account's root.</param>
/// <param name="provider">Proposes the default root.</param>
/// <param name="writer">Saves the root.</param>
public sealed class DefaultSyncRootAssigner(ISyncRootReader reader, IDefaultSyncRootProvider provider, ISyncRootWriter writer) : IDefaultSyncRootAssigner
{
    /// <inheritdoc />
    public Task<Result<SyncRoot, SyncRootRejection>> AssignAsync(AccountId accountId, AccountRegistration registration, CancellationToken cancellationToken)
    {
        _ = (reader, provider, writer);

        return Task.FromResult(Result.Failure<SyncRoot, SyncRootRejection>(SyncRootRejectionFactory.CreateUnavailable()));
    }
}
