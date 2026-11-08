using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>The right to run one sync pass for one account. Disposing it lets the next pass start; disposing it twice has no further effect.</summary>
public sealed class SyncLease : IDisposable
{
    private readonly Action<SyncLease> release;
    private int released;

    internal SyncLease(AccountId accountId, SyncPassId passId, Action<SyncLease> release)
    {
        AccountId = accountId;
        PassId = passId;
        this.release = release;
    }

    /// <summary>The account the pass runs for.</summary>
    public AccountId AccountId { get; }

    /// <summary>The pass this lease belongs to.</summary>
    public SyncPassId PassId { get; }

    /// <summary>Releases the lease.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref released, 1) == 0)
            release(this);
    }
}
