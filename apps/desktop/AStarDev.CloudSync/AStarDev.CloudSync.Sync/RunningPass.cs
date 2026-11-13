using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>One sync run in progress: its cancellation and the signal that it has ended.</summary>
internal sealed class RunningPass : IDisposable
{
    private readonly Lock gate = new();
    private readonly CancellationTokenSource cancellation;
    private readonly TaskCompletionSource ended = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool disposed;

    internal RunningPass(AccountId accountId, SyncTrigger trigger, DateTimeOffset startedAt, CancellationToken callerToken)
    {
        AccountId = accountId;
        Trigger = trigger;
        StartedAt = startedAt;
        cancellation = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
        Token = cancellation.Token;
    }

    /// <summary>The account.</summary>
    public AccountId AccountId { get; }

    /// <summary>What started the run.</summary>
    public SyncTrigger Trigger { get; }

    /// <summary>When the run started.</summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>Cancelled by the caller's token or by <see cref="Cancel"/>.</summary>
    public CancellationToken Token { get; }

    /// <summary>Completes when the run has ended and the account is free.</summary>
    public Task Ended => ended.Task;

    /// <summary>Cancels the run; no effect once it has ended.</summary>
    public void Cancel()
    {
        lock (gate)
        {
            if (!disposed)
                cancellation.Cancel();
        }
    }

    /// <summary>Marks the run as ended and frees its cancellation.</summary>
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
                return;

            disposed = true;
            cancellation.Dispose();
        }

        ended.TrySetResult();
    }
}
