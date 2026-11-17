using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>The background account leases a reclassify batch holds while it writes. An account whose lease is busy, wanted by a waiting pass or user action, or drained is not held. Disposing releases every lease.</summary>
internal sealed class HeldAccountLeases : IDisposable
{
    private readonly IReadOnlyList<SyncLease> leases;

    private HeldAccountLeases(IReadOnlyList<SyncLease> leases, bool contended)
    {
        this.leases = leases;
        Contended = contended;
    }

    /// <summary>Whether any account's lease was refused, so the run should back off.</summary>
    internal bool Contended { get; }

    /// <summary>Tries each account's background lease once, without waiting.</summary>
    /// <param name="syncLock">The account lock.</param>
    /// <param name="accountIds">The accounts of the batch.</param>
    /// <returns>The leases that were free.</returns>
    internal static HeldAccountLeases Acquire(IAccountSyncLock syncLock, IEnumerable<AccountId> accountIds)
    {
        var attempts = accountIds.Distinct().Select(syncLock.TryAcquireBackground).ToList();
        var acquired = attempts.OfType<SyncLockAttempt.Acquired>().Select(attempt => attempt.Lease).ToList();

        return new(acquired, acquired.Count < attempts.Count);
    }

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
