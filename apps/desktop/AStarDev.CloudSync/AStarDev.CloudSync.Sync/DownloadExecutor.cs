using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>Carries out a <see cref="DownloadPlan"/> (#1400): folders, moves, downloads (several at once), conflicts and deletions, writing the baseline in batches.</summary>
/// <param name="fileStep">Downloads one file.</param>
/// <param name="files">Creates, moves and deletes local items.</param>
/// <param name="baselineWriter">Writes the baseline rows.</param>
/// <param name="options">The concurrency and the batch size.</param>
/// <param name="sink">Takes the bytes downloaded so far.</param>
/// <param name="logger">Reports why the pass stopped, with no paths.</param>
internal sealed class DownloadExecutor(DownloadFileStep fileStep, ILocalFileOperations files, ISyncBaselineWriter baselineWriter, DownloadPassOptions options, ISyncProgressSink sink, ILogger<DownloadExecutor> logger)
{
    private const string Location = nameof(DownloadExecutor);

    internal async Task<Result<IReadOnlyList<DownloadItemOutcome>, DownloadPassFailure>> RunAsync(AccountId accountId, IDirectoryInfo root, DownloadPlan plan, IReadOnlyList<SyncBaselineEntry> baseline, CancellationToken cancellationToken)
    {
        var byPath = baseline.GroupBy(entry => entry.Path).ToDictionary(group => group.Key, group => group.First());
        List<DownloadItemOutcome> outcomes = [];
        var batch = new BaselineBatch();

        foreach (var folder in plan.Items.OfType<DownloadPlanItem.CreateLocalFolder>())
            outcomes.Add(await CreateFolderAsync(accountId, root, folder, batch, cancellationToken).ConfigureAwait(false));

        foreach (var move in plan.Items.OfType<DownloadPlanItem.MoveLocal>())
            outcomes.Add(await MoveAsync(root, move, byPath, baseline, batch, cancellationToken).ConfigureAwait(false));

        var flushed = await FlushAsync(accountId, batch, cancellationToken).ConfigureAwait(false);
        if (flushed.Match(_ => false, _ => true))
            return Stopped();

        List<DownloadPlanItem.DownloadFile> files = [.. plan.Items.OfType<DownloadPlanItem.DownloadFile>()];
        var tally = new PhaseTally(accountId, SyncPhaseFactory.CreateDownloading(), files.Sum(SizeOf), sink);
        var downloaded = await DownloadAllAsync(files, new DownloadRun(accountId, root, byPath, batch, tally), cancellationToken).ConfigureAwait(false);

        return await downloaded.BindAsync(done => FinishAsync(accountId, root, plan, byPath, batch, [.. outcomes, .. done], cancellationToken)).ConfigureAwait(false);
    }

    private static long SizeOf(DownloadPlanItem.DownloadFile item) => item.Item.Kind is RemoteItemKind.File file ? file.Size : 0;

    private static string FullPath(IDirectoryInfo root, RelativeSyncPath path) => root.FileSystem.Path.Join(root.FullName, path.Value);

    private static bool IsDiskFull(DownloadItemOutcome outcome) => outcome is DownloadItemOutcome.Failed { Failure: DownloadFailure.Local { Failure: LocalOperationFailure.InsufficientDiskSpace } };

    private static IFileSystemInfo ItemAt(IDirectoryInfo root, RelativeSyncPath path, Option<SyncBaselineEntry> entry) =>
        entry.Match(found => found.Kind is SyncBaselineKind.Folder, () => false) ? root.FileSystem.DirectoryInfo.New(FullPath(root, path)) : root.FileSystem.FileInfo.New(FullPath(root, path));

    private static DownloadItemOutcome Failed(LocalOperationFailure failure) => DownloadItemOutcomeFactory.CreateFailed(DownloadFailureFactory.CreateLocal(failure));

    private static Option<SyncBaselineEntry> Lookup(IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> byPath, RelativeSyncPath path) => byPath.TryGetValue(path, out var entry) ? Option.Some(entry) : Option.None<SyncBaselineEntry>();

    private Task<DownloadItemOutcome> CreateFolderAsync(AccountId accountId, IDirectoryInfo root, DownloadPlanItem.CreateLocalFolder item, BaselineBatch batch, CancellationToken cancellationToken) =>
        files.CreateFolderAsync(root.FileSystem.DirectoryInfo.New(FullPath(root, item.Path)), cancellationToken)
            .MatchAsync(
                _ =>
                {
                    batch.Upsert(SyncBaselineEntryFactory.Create(accountId, item.Path, item.ItemId, SyncBaselineKindFactory.CreateFolder()));

                    return Task.FromResult(DownloadItemOutcomeFactory.CreateFolderCreated());
                },
                failure => Task.FromResult(Failed(failure)));

    private Task<DownloadItemOutcome> MoveAsync(IDirectoryInfo root, DownloadPlanItem.MoveLocal item, IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> byPath, IReadOnlyList<SyncBaselineEntry> baseline, BaselineBatch batch, CancellationToken cancellationToken) =>
        files.MoveAsync(ItemAt(root, item.From, Lookup(byPath, item.From)), FullPath(root, item.To), cancellationToken)
            .MatchAsync(
                _ =>
                {
                    Relocate(item, baseline, batch);

                    return Task.FromResult(DownloadItemOutcomeFactory.CreateMoved());
                },
                failure => Task.FromResult(Failed(failure)));

    private static void Relocate(DownloadPlanItem.MoveLocal item, IReadOnlyList<SyncBaselineEntry> baseline, BaselineBatch batch)
    {
        foreach (var entry in baseline.Where(candidate => candidate.Path == item.From || DownloadPlanState.IsBelow(candidate.Path, item.From)))
        {
            var moved = new RelativeSyncPath(item.To.Value + entry.Path.Value[item.From.Value.Length..]);
            batch.Remove(entry.Path);
            batch.Upsert(SyncBaselineEntryFactory.Create(entry.AccountId, moved, entry.ItemId, entry.Kind));
        }
    }

    private async Task<Result<IReadOnlyList<DownloadItemOutcome>, DownloadPassFailure>> DownloadAllAsync(IReadOnlyList<DownloadPlanItem.DownloadFile> downloads, DownloadRun run, CancellationToken cancellationToken)
    {
        foreach (var chunk in downloads.Chunk(options.BaselineChunkSize))
        {
            Result<Unit, ErrorResponse> flushed;
            try
            {
                await Parallel.ForEachAsync(chunk, new ParallelOptions { MaxDegreeOfParallelism = options.MaxConcurrentDownloads, CancellationToken = cancellationToken }, (item, token) => DownloadOneAsync(item, run, token)).ConfigureAwait(false);
            }
            finally
            {
                flushed = await FlushAsync(run.AccountId, run.Batch, CancellationToken.None).ConfigureAwait(false);
            }

            if (flushed.Match(_ => false, _ => true))
                return Stopped();

            if (Volatile.Read(ref run.Stopped.Value) == 1)
                return Result.Failure<IReadOnlyList<DownloadItemOutcome>, DownloadPassFailure>(DownloadPassFailureFactory.CreateInsufficientDiskSpace());
        }

        return Result.Success<IReadOnlyList<DownloadItemOutcome>, DownloadPassFailure>([.. run.Outcomes]);
    }

    private async ValueTask DownloadOneAsync(DownloadPlanItem.DownloadFile item, DownloadRun run, CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref run.Stopped.Value) == 1)
            return;

        var progress = run.Tally.ForFile(SizeOf(item));
        var step = await fileStep.RunAsync(run.AccountId, run.Root, item, Lookup(run.ByPath, item.Path), progress, cancellationToken).ConfigureAwait(false);
        if (step.Outcome is DownloadItemOutcome.Downloaded)
            progress.Finish();

        run.Outcomes.Add(step.Outcome);
        step.Entry.Match(entry => { run.Batch.Upsert(entry); return Unit.Instance; }, () => Unit.Instance);
        if (IsDiskFull(step.Outcome))
            _ = Interlocked.Exchange(ref run.Stopped.Value, 1);
    }

    private async Task<Result<IReadOnlyList<DownloadItemOutcome>, DownloadPassFailure>> FinishAsync(AccountId accountId, IDirectoryInfo root, DownloadPlan plan, IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> byPath, BaselineBatch batch, IReadOnlyList<DownloadItemOutcome> sofar, CancellationToken cancellationToken)
    {
        List<DownloadItemOutcome> outcomes = [.. sofar];
        outcomes.AddRange(plan.Items.OfType<DownloadPlanItem.SendToConflicts>().Select(item => DownloadItemOutcomeFactory.CreateSentToConflicts(item.Kind)));
        foreach (var deletion in plan.Items.OfType<DownloadPlanItem.DeleteLocal>())
            outcomes.Add(await DeleteAsync(root, deletion, byPath, batch, cancellationToken).ConfigureAwait(false));

        var flushed = await FlushAsync(accountId, batch, cancellationToken).ConfigureAwait(false);

        return flushed.Match(_ => Result.Success<IReadOnlyList<DownloadItemOutcome>, DownloadPassFailure>(outcomes), _ => Stopped());
    }

    private Task<DownloadItemOutcome> DeleteAsync(IDirectoryInfo root, DownloadPlanItem.DeleteLocal item, IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> byPath, BaselineBatch batch, CancellationToken cancellationToken) =>
        files.DeleteAsync(ItemAt(root, item.Path, Lookup(byPath, item.Path)), cancellationToken)
            .MatchAsync(
                _ =>
                {
                    batch.Remove(item.Path);

                    return Task.FromResult(DownloadItemOutcomeFactory.CreateDeletedLocally());
                },
                failure => Task.FromResult(Failed(failure)));

    private Result<IReadOnlyList<DownloadItemOutcome>, DownloadPassFailure> Stopped()
    {
        LogMessage.Warning(logger, Location, "The download pass could not write the sync baseline.");

        return Result.Failure<IReadOnlyList<DownloadItemOutcome>, DownloadPassFailure>(DownloadPassFailureFactory.CreateUnexpected());
    }

    private async Task<Result<Unit, ErrorResponse>> FlushAsync(AccountId accountId, BaselineBatch batch, CancellationToken cancellationToken)
    {
        var (upserts, removals) = batch.Take();
        var removed = removals.Count == 0 ? Result.Success<Unit, ErrorResponse>(Unit.Instance) : await baselineWriter.RemoveAsync(accountId, removals, cancellationToken).ConfigureAwait(false);

        return upserts.Count == 0 ? removed : await removed.BindAsync(_ => baselineWriter.UpsertAsync(accountId, upserts, cancellationToken)).ConfigureAwait(false);
    }
}
