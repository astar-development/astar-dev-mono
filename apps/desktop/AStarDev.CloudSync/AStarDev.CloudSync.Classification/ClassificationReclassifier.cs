using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification;

/// <summary>Reclassifies the whole baseline in batches, one store transaction per batch. Each batch takes the lease of each of its accounts without waiting and writes only the files of accounts it holds and that still exist, so a running reclassify cannot recreate links for an account being removed; the other files are skipped and counted, and the next run picks up those of a busy account. The set of files already seen is held in memory to drop duplicates, which costs one small entry per file; a baseline of millions of files would need a store-side distinct instead.</summary>
/// <param name="fileSource">Streams every file the baseline knows about.</param>
/// <param name="classifier">Classifies one path.</param>
/// <param name="store">Writes the automatic links.</param>
/// <param name="syncLock">Holds each account's background lease while that account's links are written, so an account removal cannot run in between. A background lease gives way: a pass or user action waits for the short write, and a batch that finds a lease taken pauses for <see cref="ReclassifyOptions.DelayAfterContention"/>.</param>
/// <param name="accountStore">Confirms, under the lease, that the account still exists.</param>
/// <param name="options">Batch size, parallelism and the pause between batches.</param>
/// <param name="logger">Reports the run without paths or terms.</param>
public sealed class ClassificationReclassifier(IClassifiableFileSource fileSource, IFileClassifier classifier, IFileClassificationStore store, IAccountSyncLock syncLock, IAccountStore accountStore, ReclassifyOptions options, ILogger<ClassificationReclassifier> logger) : IClassificationReclassifier, IDisposable
{
    private readonly SemaphoreSlim runGate = new(1, 1);

    /// <summary>Releases the run guard.</summary>
    public void Dispose() => runGate.Dispose();

    /// <inheritdoc />
    public async Task WaitUntilIdleAsync(CancellationToken cancellationToken)
    {
        var wait = runGate.WaitAsync(cancellationToken);
        await wait.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (wait.IsCompletedSuccessfully)
            _ = runGate.Release();
    }

    /// <inheritdoc />
    public async Task<ReclassifyOutcome> StartAsync(IProgress<ReclassifyProgress> progress, CancellationToken cancellationToken)
    {
        if (!await runGate.WaitAsync(TimeSpan.Zero, CancellationToken.None).ConfigureAwait(false))
            return ReclassifyOutcomeFactory.CreateAlreadyRunning();

        var state = new ReclassifyRunState();
        var run = Task.Run(() => RunAsync(state, progress, cancellationToken), cancellationToken);
        await ((Task)run).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        _ = runGate.Release();

        return Resolve(run, state, cancellationToken);
    }

    private static ReclassifyOutcome Resolve(Task<ReclassifyOutcome> run, ReclassifyRunState state, CancellationToken cancellationToken)
    {
        if (run.IsCompletedSuccessfully)
            return run.Result;

        return run.IsCanceled || cancellationToken.IsCancellationRequested ? ReclassifyOutcomeFactory.CreateCancelled(state.ToCounts()) : ReclassifyOutcomeFactory.CreateFailed(state.ToCounts());
    }

    private Task<ReclassifyOutcome> RunAsync(ReclassifyRunState state, IProgress<ReclassifyProgress> progress, CancellationToken cancellationToken)
    {
        LogMessage.Information(logger, "Reclassification started.");

        return Try.RunAsync(() => ProcessAsync(state, progress, cancellationToken), cancellationToken).MatchAsync(outcome => outcome, exception => StreamFailed(state, exception));
    }

    private ReclassifyOutcome StreamFailed(ReclassifyRunState state, Exception exception)
    {
        LogMessage.Error(logger, "Reclassification failed.", exception.ErrorType());

        return ReclassifyOutcomeFactory.CreateFailed(state.ToCounts());
    }

    private async Task<ReclassifyOutcome> ProcessAsync(ReclassifyRunState state, IProgress<ReclassifyProgress> progress, CancellationToken cancellationToken)
    {
        var seen = new HashSet<(AccountId, RelativeSyncPath)>();
        var batch = new List<ClassifiableFile>(options.BatchSize);
        await foreach (var file in fileSource.StreamAsync(cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (!seen.Add((file.AccountId, file.Path)))
            {
                state.SkipDuplicate();
                continue;
            }

            batch.Add(file);
            if (batch.Count < options.BatchSize)
                continue;

            if (!await CommitAsync(batch, state, progress, cancellationToken).ConfigureAwait(false))
                return ReclassifyOutcomeFactory.CreateFailed(state.ToCounts());

            batch.Clear();
        }

        if (batch.Count > 0 && !await CommitAsync(batch, state, progress, cancellationToken).ConfigureAwait(false))
            return ReclassifyOutcomeFactory.CreateFailed(state.ToCounts());

        if (state.SkippedForAccount > 0)
            LogMessage.Information(logger, "Reclassification skipped the files of busy or removed accounts; the run result counts them.");

        LogMessage.Information(logger, "Reclassification completed.");

        return ReclassifyOutcomeFactory.CreateCompleted(state.ToCounts());
    }

    private async Task<bool> CommitAsync(List<ClassifiableFile> batch, ReclassifyRunState state, IProgress<ReclassifyProgress> progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var links = new FileTermLinks[batch.Count];
        var parallel = new ParallelOptions { MaxDegreeOfParallelism = options.DegreeOfParallelism, CancellationToken = cancellationToken };
        await Parallel.ForEachAsync(Enumerable.Range(0, batch.Count), parallel, async (index, token) => links[index] = await ClassifyAsync(batch[index], token).ConfigureAwait(false)).ConfigureAwait(false);
        var leases = HeldAccountLeases.Acquire(syncLock, links.Select(link => link.AccountId));
        bool written;
        using (leases)
            written = await WriteUnderLeasesAsync(links, leases, state, cancellationToken).ConfigureAwait(false);

        if (!written)
            return false;

        progress.Report(ReclassifyProgressFactory.Create(state.FilesWritten, state.Batches, state.SkippedForAccount));

        await Task.Yield();
        var pause = leases.Contended ? options.DelayAfterContention : options.DelayBetweenBatches;
        if (pause > TimeSpan.Zero)
            await Task.Delay(pause, options.TimeProvider, cancellationToken).ConfigureAwait(false);

        return true;
    }

    private async Task<bool> WriteUnderLeasesAsync(FileTermLinks[] links, HeldAccountLeases leases, ReclassifyRunState state, CancellationToken cancellationToken) =>
        await accountStore.GetAllAsync(cancellationToken)
            .MatchAsync(accounts => WriteKnownAsync(links, leases, accounts, state, cancellationToken), error => Task.FromResult(AccountsUnreadable(error)))
            .ConfigureAwait(false);

    private async Task<bool> WriteKnownAsync(FileTermLinks[] links, HeldAccountLeases leases, IReadOnlyList<StoredAccount> accounts, ReclassifyRunState state, CancellationToken cancellationToken)
    {
        var writable = links.Where(link => leases.Holds(link.AccountId) && accounts.Any(account => account.Id == link.AccountId)).ToList();
        state.SkipForAccount(links.Length - writable.Count);
        if (writable.Count == 0)
            return true;

        var written = await store.ReplaceAutoLinksAsync(writable, cancellationToken).MatchAsync(_ => true, WriteFailed).ConfigureAwait(false);
        if (written)
            state.CommitBatch(writable.Count);

        return written;
    }

    private bool AccountsUnreadable(ErrorResponse error)
    {
        LogMessage.Error(logger, "The accounts could not be read for reclassification.", error.GetType().Name);

        return false;
    }

    private bool WriteFailed(ClassificationFailure failure)
    {
        LogMessage.Error(logger, "A reclassification batch could not be written.", failure.GetType().Name);

        return false;
    }

    private async Task<FileTermLinks> ClassifyAsync(ClassifiableFile file, CancellationToken cancellationToken)
    {
        var result = await classifier.ClassifyAsync(file.Path, cancellationToken).ConfigureAwait(false);

        return FileTermLinksFactory.Create(file.AccountId, file.Path, [.. result.Candidates.Select(candidate => candidate.Term.Id)]);
    }
}
