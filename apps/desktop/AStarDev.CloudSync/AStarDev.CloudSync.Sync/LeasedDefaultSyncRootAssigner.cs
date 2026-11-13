using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Assigns the default root while holding the account's sync lease, so the save cannot race an account removal.</summary>
/// <param name="leases">Takes the account's lease.</param>
/// <param name="assigner">Assigns the root once the lease is held.</param>
internal sealed class LeasedDefaultSyncRootAssigner(AccountLeaseWaiter leases, DefaultSyncRootAssigner assigner) : IDefaultSyncRootAssigner
{
    /// <inheritdoc />
    public Task<Result<SyncRoot, SyncRootRejection>> AssignAsync(AccountId accountId, AccountRegistration registration, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{leases}{assigner}{accountId}{registration}{cancellationToken}");
}
