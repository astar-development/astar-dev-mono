using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Runs the transfers of a resolution through the sync engine's own executors, so a resolution uploads with the same preconditions, records the same baseline rows and downloads with the same re-checks as a pass (#1406).</summary>
/// <param name="providers">Finds the provider capabilities.</param>
/// <param name="inputs">Reads the root and the remote index rows of the affected paths.</param>
/// <param name="uploads">Runs upload, folder and removal steps and records them.</param>
/// <param name="downloads">Downloads one file with the free-space check and the re-check of the target.</param>
/// <param name="baseline">Writes the baseline row of a downloaded file.</param>
/// <param name="hasher">Hashes a local file that a download replaces.</param>
internal sealed class ResolutionTransfers(UploadPassProviders providers, UploadPassInputs inputs, UploadItemExecutor uploads, DownloadFileStep downloads, ISyncBaselineWriter baseline, IContentHasher hasher)
{
    internal static IFileInfo LocalFile(ResolutionContext context, RelativeSyncPath path)
    {
        var file = context.Input.Root.FileSystem.FileInfo.New(FullPath(context, path));
        file.Refresh();

        return file;
    }

    internal static IDirectoryInfo LocalDirectory(ResolutionContext context, RelativeSyncPath path)
    {
        var directory = context.Input.Root.FileSystem.DirectoryInfo.New(FullPath(context, path));
        directory.Refresh();

        return directory;
    }

    internal static string FullPath(ResolutionContext context, RelativeSyncPath path) => context.Input.Root.FileSystem.Path.Join(context.Input.Root.FullName, path.Value);

    internal static Option<RemoteItemSnapshot> RemoteItem(ResolutionContext context, RelativeSyncPath path) => context.Input.Remote.TryGetValue(path, out var found) ? Option.Some(found) : Option.None<RemoteItemSnapshot>();

    internal Task<Result<ResolutionContext, ResolutionFailure>> LoadAsync(AccountId accountId, IReadOnlyList<RelativeSyncPath> paths, CancellationToken cancellationToken) =>
        providers.ResolveAsync(accountId, cancellationToken)
            .BindAsync(resolved => inputs.LoadForPathsAsync(accountId, paths, cancellationToken).BindAsync(input => Task.FromResult(Result.Success<ResolutionContext, UploadPassFailure>(new ResolutionContext(accountId, input, resolved)))))
            .MatchAsync(Result.Success<ResolutionContext, ResolutionFailure>, failure => Result.Failure<ResolutionContext, ResolutionFailure>(FailureOf(failure)));

    internal Task<Result<Unit, ResolutionFailure>> ReplaceAsync(ResolutionContext context, RelativeSyncPath path, Option<ContentTag> seen, CancellationToken cancellationToken)
    {
        var file = LocalFile(context, path);
        if (!file.Exists)
            return Task.FromResult(Failed(ResolutionFailureFactory.CreateLocalFileMissing()));

        return CheckedFile(context, path, seen).Match(
            found => UploadPreconditionFactory.CreateMustMatch(found.File.CTag).Match(
                precondition => RunAsync(context, [UploadPlanItemFactory.CreateUploadReplace(path, found.Item.Id, file.Length, WrittenAt(file), precondition)], cancellationToken),
                _ => Task.FromResult(Failed(ResolutionFailureFactory.CreateUnexpected()))),
            failure => Task.FromResult(Failed(failure)));
    }

    internal Task<Result<Unit, ResolutionFailure>> UploadNewAsync(ResolutionContext context, RelativeSyncPath path, CancellationToken cancellationToken)
    {
        var file = LocalFile(context, path);

        return file.Exists
            ? RunAsync(context, [UploadPlanItemFactory.CreateUploadNewFile(path, file.Length, WrittenAt(file))], cancellationToken)
            : Task.FromResult(Failed(ResolutionFailureFactory.CreateLocalFileMissing()));
    }

    internal Task<Result<Unit, ResolutionFailure>> RemoveRemoteAsync(ResolutionContext context, RelativeSyncPath path, Option<ContentTag> seen, CancellationToken cancellationToken) =>
        CheckedFile(context, path, seen).Match(
            found => RemovePreconditionFactory.CreateMustMatchContent(found.File.CTag).Match(
                precondition => RunAsync(context, [UploadPlanItemFactory.CreateRemoveRemote(path, found.Item.Id, precondition)], cancellationToken),
                _ => Task.FromResult(Failed(ResolutionFailureFactory.CreateUnexpected()))),
            failure => Task.FromResult(Failed(failure)));

    internal async Task<Result<Unit, ResolutionFailure>> RunAsync(ResolutionContext context, IReadOnlyList<UploadPlanItem> items, CancellationToken cancellationToken) =>
        await uploads.ExecuteAsync(context.AccountId, UploadPlanFactory.Create(items), context.Input, context.Providers, cancellationToken)
            .MatchAsync(Settled, failure => Failed(FailureOf(failure)))
            .ConfigureAwait(false);

    internal Task<Result<Unit, ResolutionFailure>> DownloadAsync(ResolutionContext context, RelativeSyncPath path, Option<ContentTag> seen, CancellationToken cancellationToken) =>
        CheckedFile(context, path, seen).Match(
            found => DownloadFileAsync(context, path, found.Item, found.File, cancellationToken),
            failure => Task.FromResult(Failed(failure)));

    private static Result<(RemoteItemSnapshot Item, RemoteItemKind.File File), ResolutionFailure> CheckedFile(ResolutionContext context, RelativeSyncPath path, Option<ContentTag> seen) =>
        RemoteItem(context, path).Match(
            item => item.Kind is RemoteItemKind.File file ? Seen(item, file, seen) : Result.Failure<(RemoteItemSnapshot, RemoteItemKind.File), ResolutionFailure>(ResolutionFailureFactory.CreateRemoteFileMissing()),
            () => Result.Failure<(RemoteItemSnapshot, RemoteItemKind.File), ResolutionFailure>(ResolutionFailureFactory.CreateRemoteFileMissing()));

    private static Result<(RemoteItemSnapshot Item, RemoteItemKind.File File), ResolutionFailure> Seen(RemoteItemSnapshot item, RemoteItemKind.File file, Option<ContentTag> seen) =>
        seen.Match(tag => tag != file.CTag, () => false)
            ? Result.Failure<(RemoteItemSnapshot, RemoteItemKind.File), ResolutionFailure>(ResolutionFailureFactory.CreateRemoteChanged())
            : Result.Success<(RemoteItemSnapshot, RemoteItemKind.File), ResolutionFailure>((item, file));

    private static Result<Unit, ResolutionFailure> Done() => Result.Success<Unit, ResolutionFailure>(Unit.Instance);

    private static Result<Unit, ResolutionFailure> Failed(ResolutionFailure failure) => Result.Failure<Unit, ResolutionFailure>(failure);

    private static DateTimeOffset WrittenAt(IFileInfo file) => new(file.LastWriteTimeUtc, TimeSpan.Zero);

    private static ResolutionFailure FailureOf(UploadPassFailure failure) => failure switch
    {
        UploadPassFailure.QuotaExceeded => ResolutionFailureFactory.CreateTransfer(ProviderErrorFactory.CreateQuotaExceeded()),
        UploadPassFailure.AuthenticationRequired => ResolutionFailureFactory.CreateTransfer(ProviderErrorFactory.CreateAuthenticationRequired()),
        _ => ResolutionFailureFactory.CreateUnexpected(),
    };

    private static ResolutionFailure FailureOf(UploadItemOutcome outcome) => outcome switch
    {
        UploadItemOutcome.SentToConflicts => ResolutionFailureFactory.CreateRemoteChanged(),
        UploadItemOutcome.Deferred => ResolutionFailureFactory.CreateLocalChanged(),
        UploadItemOutcome.Failed { Error: ProviderError.PreconditionFailed } => ResolutionFailureFactory.CreateRemoteChanged(),
        UploadItemOutcome.Failed failed => ResolutionFailureFactory.CreateTransfer(failed.Error),
        _ => ResolutionFailureFactory.CreateUnexpected(),
    };

    private static bool IsDone(UploadItemOutcome outcome) => outcome is UploadItemOutcome.Uploaded or UploadItemOutcome.FolderCreated or UploadItemOutcome.Linked or UploadItemOutcome.RemovedRemotely;

    private static Result<Unit, ResolutionFailure> Settled(IReadOnlyList<UploadItemOutcome> outcomes) =>
        outcomes.FirstOrNone(outcome => !IsDone(outcome)).Match(outcome => Failed(FailureOf(outcome)), Done);

    private static ResolutionFailure FailureOf(DownloadFailure failure) => failure switch
    {
        DownloadFailure.Provider provider => ResolutionFailureFactory.CreateTransfer(provider.Error),
        DownloadFailure.Local local => ResolutionFailureFactory.CreateLocal(local.Failure),
        _ => ResolutionFailureFactory.CreateUnexpected(),
    };

    private async Task<Result<Unit, ResolutionFailure>> DownloadFileAsync(ResolutionContext context, RelativeSyncPath path, RemoteItemSnapshot remote, RemoteItemKind.File current, CancellationToken cancellationToken)
    {
        var local = LocalFile(context, path);
        var replaced = local.Exists
            ? await hasher.HashAsync(local, cancellationToken).MatchAsync(
                hash => Result.Success<Option<SyncBaselineEntry>, ResolutionFailure>(Option.Some(SyncBaselineEntryFactory.Create(context.AccountId, path, remote.Id, SyncBaselineKindFactory.CreateFile(local.Length, WrittenAt(local), hash, current.CTag, remote.ETag)))),
                _ => Result.Failure<Option<SyncBaselineEntry>, ResolutionFailure>(ResolutionFailureFactory.CreateLocal(LocalOperationFailureFactory.CreateLocked()))).ConfigureAwait(false)
            : Result.Success<Option<SyncBaselineEntry>, ResolutionFailure>(Option.None<SyncBaselineEntry>());

        return await replaced.BindAsync(entry => StepAsync(context, path, remote, entry, cancellationToken)).ConfigureAwait(false);
    }

    private async Task<Result<Unit, ResolutionFailure>> StepAsync(ResolutionContext context, RelativeSyncPath path, RemoteItemSnapshot remote, Option<SyncBaselineEntry> replaced, CancellationToken cancellationToken)
    {
        if (DownloadPlanItemFactory.CreateDownloadFile(path, remote) is not DownloadPlanItem.DownloadFile plan)
            return Failed(ResolutionFailureFactory.CreateUnexpected());

        var (outcome, entry) = await downloads.RunAsync(context.AccountId, context.Input.Root, plan, replaced, new Progress<TransferProgress>(), cancellationToken).ConfigureAwait(false);

        return outcome switch
        {
            DownloadItemOutcome.Downloaded => await RecordAsync(context, entry, cancellationToken).ConfigureAwait(false),
            DownloadItemOutcome.SentToConflicts => Failed(ResolutionFailureFactory.CreateLocalChanged()),
            DownloadItemOutcome.Skipped => Failed(ResolutionFailureFactory.CreateRemoteFileMissing()),
            DownloadItemOutcome.Failed failed => Failed(FailureOf(failed.Failure)),
            _ => Failed(ResolutionFailureFactory.CreateUnexpected()),
        };
    }

    private async Task<Result<Unit, ResolutionFailure>> RecordAsync(ResolutionContext context, Option<SyncBaselineEntry> entry, CancellationToken cancellationToken) =>
        await entry.Match(
            written => baseline.UpsertAsync(context.AccountId, [written], cancellationToken).MatchAsync(_ => Done(), _ => Failed(ResolutionFailureFactory.CreateNotRecorded())),
            () => Task.FromResult(Done())).ConfigureAwait(false);
}
