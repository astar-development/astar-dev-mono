using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Downloads one file for the download pass (#1400): free-space check, temp file in the target folder, size check, a re-check of the target just before the move, the move, the hash and the baseline row.</summary>
/// <param name="downloaders">Finds the account's provider downloader.</param>
/// <param name="files">Creates the folder and moves the finished file.</param>
/// <param name="hasher">Hashes the finished file.</param>
/// <param name="disk">Reads the free space.</param>
/// <param name="options">The free-space margin.</param>
/// <param name="classifier">Decides the conflict kind when the target changed during the download.</param>
internal sealed class DownloadFileStep(IProviderCapability<IRemoteFileDownloader> downloaders, ILocalFileOperations files, IContentHasher hasher, IDiskSpaceProbe disk, DownloadPassOptions options, IConflictClassifier classifier)
{
    internal async Task<(DownloadItemOutcome Outcome, Option<SyncBaselineEntry> Entry)> RunAsync(AccountId accountId, IDirectoryInfo root, DownloadPlanItem.DownloadFile plan, Option<SyncBaselineEntry> baseline, IProgress<TransferProgress> progress, CancellationToken cancellationToken)
    {
        var fileSystem = root.FileSystem;
        var target = fileSystem.FileInfo.New(fileSystem.Path.Join(root.FullName, plan.Path.Value));
        var folder = target.Directory ?? root;
        var size = plan.Item.Kind is RemoteItemKind.File file ? file.Size : 0;

        if (disk.GetAvailableBytes(folder).Match(free => free < size + options.FreeSpaceMarginBytes, () => false))
            return Failed(DownloadFailureFactory.CreateLocal(LocalOperationFailureFactory.CreateInsufficientDiskSpace()));

        return await files.CreateFolderAsync(folder, cancellationToken)
            .MatchAsync(_ => DownloadToTempAsync(NewAttempt(accountId, plan, target, folder, size, baseline), progress, cancellationToken), failure => Task.FromResult(Failed(DownloadFailureFactory.CreateLocal(failure))))
            .ConfigureAwait(false);
    }

    private static DownloadAttempt NewAttempt(AccountId accountId, DownloadPlanItem.DownloadFile plan, IFileInfo target, IDirectoryInfo folder, long size, Option<SyncBaselineEntry> baseline) =>
        new(accountId, plan, target, target.FileSystem.FileInfo.New(target.FileSystem.Path.Join(folder.FullName, $"{target.Name}.{Guid.NewGuid():N}.download")), size, baseline);

    private static (DownloadItemOutcome Outcome, Option<SyncBaselineEntry> Entry) Failed(DownloadFailure failure) => (DownloadItemOutcomeFactory.CreateFailed(failure), Option.None<SyncBaselineEntry>());

    private static (DownloadItemOutcome Outcome, Option<SyncBaselineEntry> Entry) ProviderFailed(ProviderError error) => error is ProviderError.NotFound
        ? (DownloadItemOutcomeFactory.CreateSkipped(DownloadSkipReasonFactory.CreateRemoteGone()), Option.None<SyncBaselineEntry>())
        : Failed(DownloadFailureFactory.CreateProvider(error));

    private static SideSnapshot LocalSnapshotOf(IFileInfo target)
    {
        target.Refresh();

        return target.Exists
            ? SideSnapshotFactory.CreatePresent(target.Length, new DateTimeOffset(target.LastWriteTimeUtc, TimeSpan.Zero), Option.None<ContentHash>(), Option.None<ContentTag>())
            : SideSnapshotFactory.CreateAbsent();
    }

    private static bool IsTargetChanged(IFileInfo target, Option<SyncBaselineEntry> baseline)
    {
        target.Refresh();
        if (!target.Exists)
            return false;

        var writtenAt = new DateTimeOffset(target.LastWriteTimeUtc, TimeSpan.Zero);

        return baseline.Match(entry => !(entry.Kind is SyncBaselineKind.File synced && synced.Size == target.Length && synced.LastWriteUtc == writtenAt), () => true);
    }

    private (DownloadItemOutcome Outcome, Option<SyncBaselineEntry> Entry) TargetChangedConflict(DownloadAttempt attempt)
    {
        var local = LocalSnapshotOf(attempt.Target);
        var remote = ConflictSides.Remote(attempt.Plan.Item);
        var evidence = ConflictEvidenceFactory.Create(local, remote, Option.None<ConflictBaseline>(), ConflictSignalFactory.CreatePreconditionFailed());
        var kind = classifier.Classify(evidence).Match(found => found, ConflictKindFactory.CreateRemoteChangedDuringTransfer);

        return (DownloadItemOutcomeFactory.CreateSentToConflicts(ConflictFindingFactory.Create(attempt.Plan.Path, kind, local, remote)), Option.None<SyncBaselineEntry>());
    }

    private static void Discard(IFileInfo temp) => _ = Try.Run(() =>
    {
        temp.Refresh();
        if (temp.Exists)
            temp.Delete();

        return Unit.Instance;
    });

    private async Task<(DownloadItemOutcome Outcome, Option<SyncBaselineEntry> Entry)> DownloadToTempAsync(DownloadAttempt attempt, IProgress<TransferProgress> progress, CancellationToken cancellationToken)
    {
        try
        {
            return await downloaders.ForAccountAsync(attempt.AccountId, cancellationToken)
                .MatchAsync(downloader => FetchAsync(downloader, attempt, progress, cancellationToken), error => Task.FromResult(ProviderFailed(error)))
                .ConfigureAwait(false);
        }
        finally
        {
            Discard(attempt.Temp);
        }
    }

    private Task<(DownloadItemOutcome Outcome, Option<SyncBaselineEntry> Entry)> FetchAsync(IRemoteFileDownloader downloader, DownloadAttempt attempt, IProgress<TransferProgress> progress, CancellationToken cancellationToken) =>
        downloader.DownloadAsync(attempt.AccountId, attempt.Plan.Item.Id, attempt.Temp, progress, cancellationToken)
            .MatchAsync(downloaded => InstallAsync(attempt, downloaded, cancellationToken), error => Task.FromResult(ProviderFailed(error)));

    private async Task<(DownloadItemOutcome Outcome, Option<SyncBaselineEntry> Entry)> InstallAsync(DownloadAttempt attempt, DownloadedFile downloaded, CancellationToken cancellationToken)
    {
        attempt.Temp.Refresh();
        if (!attempt.Temp.Exists || attempt.Temp.Length != attempt.Size)
            return Failed(DownloadFailureFactory.CreateProvider(ProviderErrorFactory.CreateTransient()));

        if (IsTargetChanged(attempt.Target, attempt.Baseline))
            return TargetChangedConflict(attempt);

        return await files.ReplaceAsync(attempt.Temp, attempt.Target, attempt.Plan.Item.LastModified, cancellationToken)
            .MatchAsync(_ => HashAsync(attempt, downloaded, cancellationToken), failure => Task.FromResult(Failed(DownloadFailureFactory.CreateLocal(failure))))
            .ConfigureAwait(false);
    }

    private Task<(DownloadItemOutcome Outcome, Option<SyncBaselineEntry> Entry)> HashAsync(DownloadAttempt attempt, DownloadedFile downloaded, CancellationToken cancellationToken) =>
        hasher.HashAsync(attempt.Target, cancellationToken).MatchAsync(
            hash => Task.FromResult(Done(attempt, downloaded, hash)),
            _ => Task.FromResult(Failed(DownloadFailureFactory.CreateLocal(LocalOperationFailureFactory.CreateLocked()))));

    private static (DownloadItemOutcome Outcome, Option<SyncBaselineEntry> Entry) Done(DownloadAttempt attempt, DownloadedFile downloaded, ContentHash hash)
    {
        attempt.Target.Refresh();
        var kind = SyncBaselineKindFactory.CreateFile(attempt.Target.Length, new DateTimeOffset(attempt.Target.LastWriteTimeUtc, TimeSpan.Zero), hash, downloaded.CTag, attempt.Plan.Item.ETag);

        return (DownloadItemOutcomeFactory.CreateDownloaded(attempt.Target.Length), Option.Some(SyncBaselineEntryFactory.Create(attempt.AccountId, attempt.Plan.Path, attempt.Plan.Item.Id, kind)));
    }
}
