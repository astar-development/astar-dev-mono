using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>An in-process <see cref="IAccountSyncLock"/>. Single instance (#1461) makes in-process enough.</summary>
internal sealed class InMemoryAccountSyncLock : IAccountSyncLock
{
    /// <inheritdoc />
    public SyncLockAttempt TryAcquire(AccountId accountId) => throw new NotImplementedException($"{accountId}");

    /// <inheritdoc />
    public Task<SyncDrain> DrainAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{cancellationToken}");
}
