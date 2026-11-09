using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
public sealed class SyncRootPolicy : ISyncRootPolicy
{
    /// <inheritdoc />
    public Result<SyncRoot, SyncRootRejection> Check(AccountId accountId, IDirectoryInfo candidate, IReadOnlyList<AccountSyncRoot> existing) =>
        SyncRootFactory.Create(candidate);
}
