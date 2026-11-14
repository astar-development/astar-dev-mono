using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="syncLock">The per-account lock every trigger goes through.</param>
/// <param name="steps">The four steps of a run.</param>
/// <param name="registry">Told when a run starts and ends.</param>
/// <param name="states">Set to sign-in required when a step reports it.</param>
/// <param name="timeProvider">Times the steps.</param>
/// <param name="progress">Told which phase a run is in.</param>
/// <param name="logger">Logs the trigger, step timings and counts, with the hashed account id only.</param>
internal sealed class SyncPassRunner(IAccountSyncLock syncLock, SyncPassSteps steps, SyncPassRegistry registry, IAccountSignInStates states, TimeProvider timeProvider, ISyncProgressSink progress, ILogger<SyncPassRunner> logger) : ISyncPassRunner
{
    private const string Location = nameof(SyncPassRunner);

    /// <inheritdoc />
    public async Task<Result<SyncPassSummary, SyncPassFailure>> RunAsync(AccountId accountId, SyncTrigger trigger, CancellationToken cancellationToken)
    {
        using var scope = SyncLogScope.Begin(logger, accountId);

        return syncLock.TryAcquire(accountId) is SyncLockAttempt.Acquired acquired
            ? await RunHoldingAsync(acquired.Lease, accountId, trigger, cancellationToken).ConfigureAwait(false)
            : Skipped(trigger);
    }

    private static Result<T, SyncPassFailure> Stopped<T>(SyncPassFailure failure) => Result.Failure<T, SyncPassFailure>(failure);

    private Result<SyncPassSummary, SyncPassFailure> Skipped(SyncTrigger trigger)
    {
        var message = $"The {trigger.GetType().Name} sync run was skipped: a run is already in progress, or the lock is draining.";
        LogMessage.Information(logger, Location, message);

        return Stopped<SyncPassSummary>(SyncPassFailureFactory.CreateAlreadyRunning());
    }

    private async Task<Result<SyncPassSummary, SyncPassFailure>> RunHoldingAsync(SyncLease lease, AccountId accountId, SyncTrigger trigger, CancellationToken cancellationToken)
    {
        var started = timeProvider.GetTimestamp();
        RunningPass pass;
        Result<SyncPassSummary, SyncPassFailure> outcome;
        using (lease)
        {
            pass = registry.Begin(accountId, trigger, cancellationToken);
            var running = StepsAsync(accountId, pass.Token);
            await ((Task)running).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            outcome = running.IsCompletedSuccessfully ? await running.ConfigureAwait(false) : Abandoned(running);
            await RefreshQuotaAsync(accountId, outcome, pass.Token).ConfigureAwait(false);
        }

        registry.End(pass, outcome);
        LogEnd(trigger, outcome, timeProvider.GetElapsedTime(started));

        return outcome;
    }

    private static bool NeedsNoQuotaRefresh(Result<SyncPassSummary, SyncPassFailure> outcome) => outcome.Match(_ => false, failure => failure is SyncPassFailure.Cancelled or SyncPassFailure.AuthenticationRequired);

    private async Task RefreshQuotaAsync(AccountId accountId, Result<SyncPassSummary, SyncPassFailure> outcome, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested || NeedsNoQuotaRefresh(outcome))
            return;

        var refreshing = Task.Run(() => steps.Quota.RefreshAsync(accountId, cancellationToken), cancellationToken);
        await ((Task)refreshing).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        if (refreshing.Exception?.GetBaseException() is { } exception)
            LogMessage.Warning(logger, Location, $"The storage quota refresh threw {exception.GetType().Name}.");
    }

    private Result<SyncPassSummary, SyncPassFailure> Abandoned(Task running)
    {
        if (running.IsCanceled)
            return Stopped<SyncPassSummary>(SyncPassFailureFactory.CreateCancelled());

        var message = $"A sync step threw {running.Exception?.GetBaseException().GetType().Name}.";
        LogMessage.Warning(logger, Location, message);

        return Stopped<SyncPassSummary>(SyncPassFailureFactory.CreateUnexpected());
    }

    private void LogEnd(SyncTrigger trigger, Result<SyncPassSummary, SyncPassFailure> outcome, TimeSpan elapsed)
    {
        var milliseconds = (long)elapsed.TotalMilliseconds;
        var message = outcome.Match(
            done => $"The {trigger.GetType().Name} sync run finished in {milliseconds} ms: {done.LocalChanges} local changes, {done.Download.Downloaded} downloaded, {done.Download.DeletedLocally} deleted locally, {done.Upload.Uploaded} uploaded, {done.Upload.Removed} removed remotely, {done.Conflicts} conflicts, {done.HeldDeletions} held deletions.",
            failure => $"The {trigger.GetType().Name} sync run stopped after {milliseconds} ms ({failure.GetType().Name}).");
        LogMessage.Information(logger, Location, message);
    }

    private Task<Result<SyncPassSummary, SyncPassFailure>> StepsAsync(AccountId accountId, CancellationToken cancellationToken) =>
        TimedAsync(accountId, "remote index", SyncPhaseFactory.CreateUpdatingRemoteIndex(), () => UpdateIndexAsync(accountId, cancellationToken))
            .BindAsync(remote => TimedAsync(accountId, "local scan", SyncPhaseFactory.CreateScanningLocal(), () => ScanAsync(accountId, cancellationToken))
                .BindAsync(changes => ClearAsync(accountId, changes, cancellationToken)
                    .BindAsync(_ => TimedAsync(accountId, "download", SyncPhaseFactory.CreateDownloading(), () => DownloadAsync(accountId, changes, cancellationToken)))
                    .BindAsync(downloaded => RecordDownloadAsync(accountId, downloaded, cancellationToken))
                    .BindAsync(downloaded => TimedAsync(accountId, "upload", SyncPhaseFactory.CreateUploading(), () => UploadAsync(accountId, changes, cancellationToken))
                        .BindAsync(uploaded => RecordUploadAsync(accountId, uploaded, cancellationToken))
                        .MatchAsync(uploaded => Result.Success<SyncPassSummary, SyncPassFailure>(SyncPassSummaryFactory.Create(remote, changes, downloaded, uploaded)), Stopped<SyncPassSummary>))));

    private Task<Result<Unit, SyncPassFailure>> ClearAsync(AccountId accountId, LocalChangeSet changes, CancellationToken cancellationToken) =>
        steps.Attention.ClearAsync(accountId, changes, cancellationToken).MatchAsync(Result.Success<Unit, SyncPassFailure>, _ => AttentionFailed<Unit>());

    private Task<Result<DownloadPassSummary, SyncPassFailure>> RecordDownloadAsync(AccountId accountId, DownloadPassSummary downloaded, CancellationToken cancellationToken) =>
        steps.Attention.RecordAsync(accountId, downloaded.Conflicts, downloaded.Held, cancellationToken).MatchAsync(_ => Result.Success<DownloadPassSummary, SyncPassFailure>(downloaded), _ => AttentionFailed<DownloadPassSummary>());

    private Task<Result<UploadPassSummary, SyncPassFailure>> RecordUploadAsync(AccountId accountId, UploadPassSummary uploaded, CancellationToken cancellationToken) =>
        steps.Attention.RecordAsync(accountId, uploaded.Conflicts, [], cancellationToken).MatchAsync(_ => Result.Success<UploadPassSummary, SyncPassFailure>(uploaded), _ => AttentionFailed<UploadPassSummary>());

    private Result<T, SyncPassFailure> AttentionFailed<T>()
    {
        LogMessage.Warning(logger, Location, "The sync run could not read or write the attention queue.");

        return Stopped<T>(SyncPassFailureFactory.CreateUnexpected());
    }

    private async Task<Result<T, SyncPassFailure>> TimedAsync<T>(AccountId accountId, string step, SyncPhase phase, Func<Task<Result<T, SyncPassFailure>>> run)
    {
        progress.Report(accountId, SyncProgressFactory.Create(phase, 0, 0));
        var started = timeProvider.GetTimestamp();
        var outcome = await run().ConfigureAwait(false);
        var message = $"The {step} step took {(long)timeProvider.GetElapsedTime(started).TotalMilliseconds} ms.";
        LogMessage.Information(logger, Location, message);

        return outcome;
    }

    private Task<Result<RemoteUpdateSummary, SyncPassFailure>> UpdateIndexAsync(AccountId accountId, CancellationToken cancellationToken) =>
        steps.RemoteIndex.UpdateAsync(accountId, cancellationToken)
            .MatchAsync(Result.Success<RemoteUpdateSummary, SyncPassFailure>, error => Stopped<RemoteUpdateSummary>(error is ProviderError.AuthenticationRequired ? SignInRequired(accountId) : SyncPassFailureFactory.CreateUnexpected()));

    private Task<Result<LocalChangeSet, SyncPassFailure>> ScanAsync(AccountId accountId, CancellationToken cancellationToken) =>
        steps.LocalChanges.DetectAsync(accountId, cancellationToken)
            .MatchAsync(Result.Success<LocalChangeSet, SyncPassFailure>, failure => Stopped<LocalChangeSet>(failure is LocalScanFailure.RootUnavailable ? SyncPassFailureFactory.CreateRootUnavailable() : SyncPassFailureFactory.CreateUnexpected()));

    private Task<Result<DownloadPassSummary, SyncPassFailure>> DownloadAsync(AccountId accountId, LocalChangeSet changes, CancellationToken cancellationToken) =>
        steps.Download.RunAsync(accountId, changes, cancellationToken)
            .MatchAsync(Result.Success<DownloadPassSummary, SyncPassFailure>, failure => Stopped<DownloadPassSummary>(failure switch
            {
                DownloadPassFailure.RootUnavailable => SyncPassFailureFactory.CreateRootUnavailable(),
                DownloadPassFailure.InsufficientDiskSpace => SyncPassFailureFactory.CreateInsufficientDiskSpace(),
                DownloadPassFailure.AuthenticationRequired => SignInRequired(accountId),
                _ => SyncPassFailureFactory.CreateUnexpected(),
            }));

    private Task<Result<UploadPassSummary, SyncPassFailure>> UploadAsync(AccountId accountId, LocalChangeSet changes, CancellationToken cancellationToken) =>
        steps.Upload.RunAsync(accountId, changes, cancellationToken)
            .MatchAsync(Result.Success<UploadPassSummary, SyncPassFailure>, failure => Stopped<UploadPassSummary>(failure switch
            {
                UploadPassFailure.RootUnavailable => SyncPassFailureFactory.CreateRootUnavailable(),
                UploadPassFailure.QuotaExceeded => SyncPassFailureFactory.CreateQuotaExceeded(),
                UploadPassFailure.AuthenticationRequired => SignInRequired(accountId),
                _ => SyncPassFailureFactory.CreateUnexpected(),
            }));

    private SyncPassFailure SignInRequired(AccountId accountId)
    {
        states.Record(accountId, AccountSignInStateFactory.CreateSignInRequired());

        return SyncPassFailureFactory.CreateAuthenticationRequired();
    }
}
