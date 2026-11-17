using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>The account leases a reclassify batch holds while it writes. An account whose lease is busy or drained is not held. Disposing releases every lease.</summary>
internal sealed class HeldAccountLeases : IDisposable
{
    private readonly IReadOnlyList<SyncLease> leases;

    private HeldAccountLeases(IReadOnlyList<SyncLease> leases) => this.leases = leases;

    /// <summary>Tries each account's lease once, without waiting.</summary>
    /// <param name="syncLock">The account lock.</param>
    /// <param name="accountIds">The accounts of the batch.</param>
    /// <returns>The leases that were free.</returns>
    internal static HeldAccountLeases Acquire(IAccountSyncLock syncLock, IEnumerable<AccountId> accountIds) =>
        new([.. accountIds.Distinct().Select(syncLock.TryAcquire).OfType<SyncLockAttempt.Acquired>().Select(acquired => acquired.Lease)]);

    /// <summary>Whether the account's lease is held.</summary>
    /// <param name="accountId">The account.</param>
    internal bool Holds(AccountId accountId) => leases.Any(lease => lease.AccountId == accountId);

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var lease in leases)
            lease.Dispose();
    }
}
