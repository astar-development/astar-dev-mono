using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification;

/// <summary>Reclassifies the whole baseline in batches, one store transaction per batch. The set of files already seen is held in memory to drop duplicates, which costs one small entry per file; a baseline of millions of files would need a store-side distinct instead.</summary>
/// <param name="fileSource">Streams every file the baseline knows about.</param>
/// <param name="classifier">Classifies one path.</param>
/// <param name="store">Writes the automatic links.</param>
/// <param name="options">Batch size, parallelism and the pause between batches.</param>
/// <param name="logger">Reports the run without paths or terms.</param>
public sealed class ClassificationReclassifier(IClassifiableFileSource fileSource, IFileClassifier classifier, IFileClassificationStore store, ReclassifyOptions options, ILogger<ClassificationReclassifier> logger) : IClassificationReclassifier
{
    private int running;

    /// <inheritdoc />
    public async Task<ReclassifyOutcome> StartAsync(IProgress<ReclassifyProgress> progress, CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref running, 1, 0) != 0)
            return ReclassifyOutcomeFactory.CreateAlreadyRunning();

        var state = new ReclassifyRunState();
        var run = Task.Run(() => RunAsync(state, progress, cancellationToken), cancellationToken);
        await ((Task)run).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        _ = Interlocked.Exchange(ref running, 0);

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

        LogMessage.Information(logger, "Reclassification completed.");

        return ReclassifyOutcomeFactory.CreateCompleted(state.ToCounts());
    }

    private async Task<bool> CommitAsync(List<ClassifiableFile> batch, ReclassifyRunState state, IProgress<ReclassifyProgress> progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var links = new FileTermLinks[batch.Count];
        var parallel = new ParallelOptions { MaxDegreeOfParallelism = options.DegreeOfParallelism, CancellationToken = cancellationToken };
        await Parallel.ForEachAsync(Enumerable.Range(0, batch.Count), parallel, async (index, token) => links[index] = await ClassifyAsync(batch[index], token).ConfigureAwait(false)).ConfigureAwait(false);
        var written = await store.ReplaceAutoLinksAsync(links, cancellationToken).MatchAsync(_ => true, WriteFailed).ConfigureAwait(false);
        if (!written)
            return false;

        state.CommitBatch(batch.Count);
        progress.Report(ReclassifyProgressFactory.Create(state.FilesWritten, state.Batches));
        await Task.Yield();
        if (options.DelayBetweenBatches > TimeSpan.Zero)
            await Task.Delay(options.DelayBetweenBatches, cancellationToken).ConfigureAwait(false);

        return true;
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
