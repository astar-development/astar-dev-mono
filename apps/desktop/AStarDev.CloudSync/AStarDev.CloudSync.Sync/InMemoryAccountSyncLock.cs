using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>An in-process <see cref="IAccountSyncLock"/>. Single instance (#1461) makes in-process enough.</summary>
internal sealed class InMemoryAccountSyncLock : IAccountSyncLock
{
    private readonly Lock gate = new();
    private readonly Dictionary<AccountId, SyncLease> running = [];
    private TaskCompletionSource idle = CompletedSignal();
    private int drains;

    /// <inheritdoc />
    public SyncLockAttempt TryAcquire(AccountId accountId)
    {
        lock (gate)
        {
            if (drains > 0)
                return SyncLockAttemptFactory.CreateDraining();

            if (running.TryGetValue(accountId, out var current))
                return SyncLockAttemptFactory.CreateBusy(current.PassId);

            var lease = new SyncLease(accountId, SyncPassIdFactory.Create(Guid.NewGuid()), Release);
            running[accountId] = lease;
            if (running.Count == 1)
                idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            return SyncLockAttemptFactory.CreateAcquired(lease);
        }
    }

    /// <inheritdoc />
    public async Task<SyncDrain> DrainAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Task allReleased;
        lock (gate)
        {
            drains++;
            allReleased = running.Count == 0 ? Task.CompletedTask : idle.Task;
        }

        var waiting = allReleased.WaitAsync(cancellationToken);
        await waiting.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        if (waiting.IsCanceled)
        {
            Reopen();
            cancellationToken.ThrowIfCancellationRequested();
        }

        return new SyncDrain(Reopen);
    }

    private static TaskCompletionSource CompletedSignal()
    {
        var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        signal.SetResult();

        return signal;
    }

    private void Release(SyncLease lease)
    {
        lock (gate)
        {
            if (!running.TryGetValue(lease.AccountId, out var current) || !ReferenceEquals(current, lease))
                return;

            running.Remove(lease.AccountId);
            if (running.Count == 0)
                idle.TrySetResult();
        }
    }

    private void Reopen()
    {
        lock (gate)
            drains--;
    }
}
