using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="homeFolder">The folder the default root lives under.</param>
/// <param name="policy">Rejects a candidate that crosses an existing root.</param>
public sealed class DefaultSyncRootProvider(IHomeFolder homeFolder, ISyncRootPolicy policy) : IDefaultSyncRootProvider
{
    /// <inheritdoc />
    public Result<SyncRoot, SyncRootRejection> CreateFor(AccountRegistration registration, IReadOnlyList<AccountSyncRoot> existing)
    {
        _ = (homeFolder, policy);

        return Result.Failure<SyncRoot, SyncRootRejection>(SyncRootRejectionFactory.CreateUnavailable());
    }
}
