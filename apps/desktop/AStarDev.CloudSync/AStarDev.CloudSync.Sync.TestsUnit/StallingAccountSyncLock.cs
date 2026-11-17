using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync.TestsUnit;

internal sealed class StallingAccountSyncLock : IAccountSyncLock
{
    private static readonly SyncLockAttempt Busy = SyncLockAttemptFactory.CreateBusy(SyncPassIdFactory.Create(Guid.NewGuid()));
    private int attempts;

    public SyncLockAttempt TryAcquire(AccountId accountId) => Busy;

    public SyncLockAttempt TryAcquireBackground(AccountId accountId) => Busy;

    public Task<SyncLockAttempt> AcquireAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Interlocked.Increment(ref attempts) == 1 ? Task.FromResult(Busy) : WaitUntilCancelledAsync(cancellationToken);

    public Task<SyncDrain> DrainAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

    private static async Task<SyncLockAttempt> WaitUntilCancelledAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        return Busy;
    }
}
