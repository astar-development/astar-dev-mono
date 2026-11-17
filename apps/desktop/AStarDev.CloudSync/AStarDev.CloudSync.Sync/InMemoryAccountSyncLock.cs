using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>An in-process <see cref="IAccountSyncLock"/>. Single instance (#1461) makes in-process enough. A background lease (a reclassify write) gives way: <see cref="AcquireAsync"/> waits for it, and no new one starts while such a caller waits.</summary>
/// <param name="timeProvider">Times the wait for a background lease.</param>
internal sealed class InMemoryAccountSyncLock(TimeProvider timeProvider) : IAccountSyncLock
{
    private readonly Lock gate = new();
    private readonly Dictionary<AccountId, SyncLease> running = [];
    private readonly Dictionary<AccountId, TaskCompletionSource> backgroundReleased = [];
    private readonly Dictionary<AccountId, (int Count, SyncPassId WaitingBehind)> waiters = [];
    private TaskCompletionSource idle = CompletedSignal();
    private int drains;

    /// <inheritdoc />
    public SyncLockAttempt TryAcquire(AccountId accountId)
    {
        lock (gate)
            return Attempt(accountId);
    }

    /// <inheritdoc />
    public async Task<SyncLockAttempt> AcquireAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        SyncLockAttempt refusal;
        Task released;
        lock (gate)
        {
            refusal = Attempt(accountId);
            if (refusal is not SyncLockAttempt.Busy busy || !backgroundReleased.TryGetValue(accountId, out var signal))
                return refusal;

            AddWaiter(accountId, busy.RunningPass);
            released = signal.Task;
        }

        var waiting = released.WaitAsync(AccountLeasePolicy.WaitForRunningWork, timeProvider, cancellationToken);
        await waiting.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        lock (gate)
        {
            RemoveWaiter(accountId);

            return waiting.IsCompletedSuccessfully ? Attempt(accountId) : refusal;
        }
    }

    /// <inheritdoc />
    public SyncLockAttempt TryAcquireBackground(AccountId accountId)
    {
        lock (gate)
        {
            if (drains == 0 && !running.ContainsKey(accountId) && waiters.TryGetValue(accountId, out var waiting))
                return SyncLockAttemptFactory.CreateBusy(waiting.WaitingBehind);

            var attempt = Attempt(accountId);
            if (attempt is SyncLockAttempt.Acquired)
                backgroundReleased[accountId] = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            return attempt;
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

    private SyncLockAttempt Attempt(AccountId accountId)
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

    private void AddWaiter(AccountId accountId, SyncPassId waitingBehind) =>
        waiters[accountId] = waiters.TryGetValue(accountId, out var current) ? (current.Count + 1, current.WaitingBehind) : (1, waitingBehind);

    private void RemoveWaiter(AccountId accountId)
    {
        var current = waiters[accountId];
        if (current.Count == 1)
            _ = waiters.Remove(accountId);
        else
            waiters[accountId] = (current.Count - 1, current.WaitingBehind);
    }

    private void Release(SyncLease lease)
    {
        lock (gate)
        {
            if (!running.TryGetValue(lease.AccountId, out var current) || !ReferenceEquals(current, lease))
                return;

            running.Remove(lease.AccountId);
            if (backgroundReleased.Remove(lease.AccountId, out var released))
                released.TrySetResult();

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
