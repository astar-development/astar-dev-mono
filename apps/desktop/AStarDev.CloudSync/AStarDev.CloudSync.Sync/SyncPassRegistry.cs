using System.Collections.Concurrent;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Knows the sync runs in progress: the <see cref="ISyncActivitySource"/> and the <see cref="ISyncCancelAction"/>. The runner tells it when a run starts and ends.</summary>
/// <param name="timeProvider">Stamps the start of a run.</param>
/// <param name="progress">Tells how far the running passes have got.</param>
internal sealed class SyncPassRegistry(TimeProvider timeProvider, SyncProgressFeed progress) : ISyncActivitySource, ISyncCancelAction, ISyncRunStatusSource
{
    private readonly ConcurrentDictionary<AccountId, RunningPass> running = new();
    private readonly ConcurrentDictionary<AccountId, SyncStatus> lastStatus = new();

    /// <inheritdoc />
    public event EventHandler<SyncPassStartedEventArgs>? PassStarted;

    /// <inheritdoc />
    public event EventHandler<SyncPassFinishedEventArgs>? PassFinished;

    /// <inheritdoc />
    public AccountSyncActivity ActivityOf(AccountId accountId) =>
        running.TryGetValue(accountId, out var pass) ? AccountSyncActivityFactory.CreateRunning(pass.Trigger, pass.StartedAt) : AccountSyncActivityFactory.CreateIdle();

    /// <inheritdoc />
    public SyncStatus StatusOf(AccountId accountId) =>
        running.ContainsKey(accountId) ? SyncStatusFactory.CreateSyncing(progress.ProgressOf(accountId).Match(current => current.PercentComplete(), () => 0)) : lastStatus.GetValueOrDefault(accountId, SyncStatusFactory.CreateUpToDate());

    /// <inheritdoc />
    public Task CancelAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        if (!running.TryGetValue(accountId, out var pass))
            return Task.CompletedTask;

        pass.Cancel();

        return pass.Ended.WaitAsync(cancellationToken);
    }

    /// <summary>Records that a run started and raises <see cref="PassStarted"/>.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="trigger">What started the run.</param>
    /// <param name="cancellationToken">The caller's token.</param>
    internal RunningPass Begin(AccountId accountId, SyncTrigger trigger, CancellationToken cancellationToken)
    {
        var pass = new RunningPass(accountId, trigger, timeProvider.GetUtcNow(), cancellationToken);
        progress.Reset(accountId);
        running[accountId] = pass;
        PassStarted?.Invoke(this, new SyncPassStartedEventArgs(accountId, trigger, pass.StartedAt));

        return pass;
    }

    /// <summary>Records that a run ended and raises <see cref="PassFinished"/>. Call it after the account's lease is released.</summary>
    /// <param name="pass">The run.</param>
    /// <param name="outcome">What the run did.</param>
    internal void End(RunningPass pass, Result<SyncPassSummary, SyncPassFailure> outcome)
    {
        if (outcome.ToStatus() is Option<SyncStatus>.Some recorded)
            lastStatus[pass.AccountId] = recorded.Value;

        _ = running.TryRemove(new KeyValuePair<AccountId, RunningPass>(pass.AccountId, pass));
        progress.Reset(pass.AccountId);
        pass.Dispose();
        PassFinished?.Invoke(this, new SyncPassFinishedEventArgs(pass.AccountId, pass.Trigger, outcome));
    }
}
