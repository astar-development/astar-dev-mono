using Microsoft.Extensions.Logging;

namespace AStar.Dev.Sync.Engine;

/// <summary>
/// Manages automatic sync scheduling with per-account staggering (SE-04, SE-05).
/// </summary>
public sealed partial class SyncScheduler(ISyncEngine syncEngine, ISyncLock syncLock, SyncOptions options, ILogger<SyncScheduler> logger) : ISyncScheduler
{
    private CancellationTokenSource? _cts;
    private Task? _runLoop;

    /// <inheritdoc />
    public bool IsRunning => _cts is not null && !_cts.IsCancellationRequested;

    /// <inheritdoc />
    public Task StartAsync(IReadOnlyList<string> accountIds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(accountIds);

        if (IsRunning)
        {
            LogAlreadyRunning(logger);

            return Task.CompletedTask;
        }

        if (accountIds.Count == 0)
        {
            LogNoAccounts(logger);

            return Task.CompletedTask;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _runLoop = RunAsync(accountIds, _cts.Token);

        LogSchedulerStarted(logger, accountIds.Count, options.SyncInterval);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        if (_cts is null)
        {
            return;
        }

        await _cts.CancelAsync().ConfigureAwait(false);

        if (_runLoop is not null)
        {
            try
            {
                await _runLoop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected on cancellation.
            }
        }

        _cts.Dispose();
        _cts = null;
        _runLoop = null;

        LogSchedulerStopped(logger);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    private async Task RunAsync(IReadOnlyList<string> accountIds, CancellationToken ct)
    {
        var stagger = accountIds.Count > 1 ? options.SyncInterval / accountIds.Count : options.SyncInterval;

        LogStaggerInterval(logger, stagger, accountIds.Count);

        while (!ct.IsCancellationRequested)
        {
            for (var i = 0; i < accountIds.Count && !ct.IsCancellationRequested; i++)
            {
                var accountId = accountIds[i];

                if (syncLock.IsRunning(accountId))
                {
                    LogSkippingAccount(logger, accountId);
                }
                else
                {
                    LogScheduledSyncStarting(logger, accountId);
                    await syncEngine.SyncAsync(accountId, ct).ConfigureAwait(false);
                }

                if (i < accountIds.Count - 1)
                {
                    await Task.Delay(stagger, ct).ConfigureAwait(false);
                }
            }

            await Task.Delay(stagger, ct).ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Scheduler is already running.")]
    private static partial void LogAlreadyRunning(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No accounts provided; scheduler will not start.")]
    private static partial void LogNoAccounts(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Scheduler started for {Count} accounts with interval {Interval}.")]
    private static partial void LogSchedulerStarted(ILogger logger, int count, TimeSpan interval);

    [LoggerMessage(Level = LogLevel.Information, Message = "Scheduler stopped.")]
    private static partial void LogSchedulerStopped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stagger interval: {Stagger} across {Count} accounts.")]
    private static partial void LogStaggerInterval(ILogger logger, TimeSpan stagger, int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping account {AccountId} — sync already in progress.")]
    private static partial void LogSkippingAccount(ILogger logger, string accountId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Scheduled sync starting for account {AccountId}.")]
    private static partial void LogScheduledSyncStarting(ILogger logger, string accountId);
}
