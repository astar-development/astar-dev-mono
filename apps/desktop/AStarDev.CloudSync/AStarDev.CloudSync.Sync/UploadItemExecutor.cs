using System.Collections.Concurrent;
using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Runs the steps of an upload plan: folders one after another, files up to the configured number at a time, then deletions, recording every success.</summary>
/// <param name="recorder">Writes the baseline and remote index rows of an uploaded file or a created folder in one transaction.</param>
/// <param name="baseline">Writes linked folders and deletion changes to the baseline.</param>
/// <param name="options">The concurrency limit.</param>
/// <param name="sink">Takes the bytes uploaded so far.</param>
internal sealed class UploadItemExecutor(IUploadRecorder recorder, ISyncBaselineWriter baseline, UploadPassOptions options, ISyncProgressSink sink)
{
    private const char Separator = '/';

    internal async Task<Result<IReadOnlyList<UploadItemOutcome>, UploadPassFailure>> ExecuteAsync(AccountId accountId, UploadPlan plan, UploadPassInput input, UploadProviders providers, CancellationToken cancellationToken)
    {
        var tally = new PhaseTally(accountId, SyncPhaseFactory.CreateUploading(), plan.Items.Sum(SizeOf), sink);
        var run = new PassRun(accountId, input, providers, SeedFolders(input), tally);
        foreach (var item in plan.Items.Where(IsFolderStep))
            run.Outcomes.Add(await RunFolderStepAsync(run, item, cancellationToken).ConfigureAwait(false));

        await Parallel.ForEachAsync(plan.Items.Where(IsUploadStep), new ParallelOptions { MaxDegreeOfParallelism = options.MaxConcurrentUploads, CancellationToken = cancellationToken }, (item, token) => new ValueTask(UploadStepAsync(run, item, token))).ConfigureAwait(false);

        foreach (var item in plan.Items.Where(IsRemovalStep))
            run.Outcomes.Add(await RunRemovalStepAsync(run, item, cancellationToken).ConfigureAwait(false));

        await Parallel.ForEachAsync(plan.Items.Where(IsReportedStep), new ParallelOptions { MaxDegreeOfParallelism = options.MaxConcurrentUploads, CancellationToken = cancellationToken }, async (item, token) => run.Outcomes.Add(await ReportedAsync(run, item, token).ConfigureAwait(false))).ConfigureAwait(false);

        return run.Stop.Match(
            failure => Result.Failure<IReadOnlyList<UploadItemOutcome>, UploadPassFailure>(failure),
            () => Result.Success<IReadOnlyList<UploadItemOutcome>, UploadPassFailure>([.. run.Outcomes]));
    }

    private static long SizeOf(UploadPlanItem item) => item switch
    {
        UploadPlanItem.UploadNewFile created => created.Size,
        UploadPlanItem.UploadReplace replace => replace.Size,
        UploadPlanItem.SendToConflicts conflict => FirstSyncLinker.BytesToHash(conflict.Finding, conflict.RemoteItem),
        _ => 0,
    };

    private static bool IsFolderStep(UploadPlanItem item) => item is UploadPlanItem.EnsureRemoteFolder or UploadPlanItem.LinkExisting;

    private static bool IsUploadStep(UploadPlanItem item) => item is UploadPlanItem.UploadNewFile or UploadPlanItem.UploadReplace;

    private static bool IsRemovalStep(UploadPlanItem item) => item is UploadPlanItem.RemoveRemote or UploadPlanItem.ForgetBaseline;

    private static bool IsReportedStep(UploadPlanItem item) => item is UploadPlanItem.SendToConflicts or UploadPlanItem.Rejected;

    private static ConcurrentDictionary<RelativeSyncPath, RemoteItemId> SeedFolders(UploadPassInput input)
    {
        var folders = new ConcurrentDictionary<RelativeSyncPath, RemoteItemId>();
        foreach (var entry in input.Baseline.Values.Where(entry => entry.Kind is SyncBaselineKind.Folder))
            folders[entry.Path] = entry.ItemId;

        foreach (var pair in input.Remote.Where(pair => pair.Value.Kind is RemoteItemKind.Folder))
            folders[pair.Key] = pair.Value.Id;

        return folders;
    }

    private static string NameOf(RelativeSyncPath path)
    {
        var value = path.Value ?? string.Empty;

        return value[(value.LastIndexOf(Separator) + 1)..];
    }

    private static Option<RelativeSyncPath> ParentOf(RelativeSyncPath path)
    {
        var value = path.Value ?? string.Empty;
        var cut = value.LastIndexOf(Separator);

        return cut < 0 ? Option.None<RelativeSyncPath>() : Option.Some(new RelativeSyncPath(value[..cut]));
    }

    private static Result<Option<RemoteItemId>, UploadItemOutcome> ParentIdOf(PassRun run, RelativeSyncPath path) =>
        ParentOf(path).Match(
            parent => run.Folders.TryGetValue(parent, out var id) ? Result.Success<Option<RemoteItemId>, UploadItemOutcome>(Option.Some(id)) : Result.Failure<Option<RemoteItemId>, UploadItemOutcome>(UploadItemOutcomeFactory.CreateFailed(path, ProviderErrorFactory.CreateNotFound())),
            () => Result.Success<Option<RemoteItemId>, UploadItemOutcome>(Option.None<RemoteItemId>()));

    private static ConflictFinding FindingFor(PassRun run, RelativeSyncPath path, ConflictKind kind)
    {
        var change = run.Input.Changes.Changes.FirstOrNone(candidate => candidate.Path == path);
        var remote = run.Input.Remote.TryGetValue(path, out var found) ? Option.Some(found) : Option.None<RemoteItemSnapshot>();

        return ConflictSides.Finding(path, kind, change, remote);
    }

    private static IFileInfo SourceOf(PassRun run, RelativeSyncPath path) => run.Input.Root.FileSystem.FileInfo.New(run.Input.Root.FileSystem.Path.Join(run.Input.Root.FullName, path.Value));

    private static UploadItemOutcome UploadFailed(PassRun run, UploadPlanItem item, ProviderError error)
    {
        run.Observe(error);
        var isReplace = item is UploadPlanItem.UploadReplace;

        return error switch
        {
            ProviderError.PreconditionFailed or ProviderError.NameConflict => UploadItemOutcomeFactory.CreateSentToConflicts(FindingFor(run, item.Path, isReplace ? ConflictKindFactory.CreateBothModified() : ConflictKindFactory.CreateBothAdded())),
            ProviderError.SourceChanged => UploadItemOutcomeFactory.CreateDeferred(item.Path),
            _ => UploadItemOutcomeFactory.CreateFailed(item.Path, error),
        };
    }

    private async Task<UploadItemOutcome> ReportedAsync(PassRun run, UploadPlanItem item, CancellationToken cancellationToken)
    {
        if (item is UploadPlanItem.Rejected rejected)
            return UploadItemOutcomeFactory.CreateRejected(rejected.Path, rejected.Reason);

        if (item is not UploadPlanItem.SendToConflicts conflict)
            return UploadItemOutcomeFactory.CreateFailed(item.Path, ProviderErrorFactory.CreateUnexpected());

        return await FirstSyncLinker.TryLinkAsync(run.AccountId, run.Input.Root, conflict.Finding, conflict.RemoteItem, run.Tally.ForFile(FirstSyncLinker.BytesToHash(conflict.Finding, conflict.RemoteItem)), cancellationToken)
            .MatchAsync(link => LinkedOrConflictAsync(run, conflict, link, cancellationToken), failure => Task.FromResult(LinkFailed(conflict.Path, failure))).ConfigureAwait(false);
    }

    private static UploadItemOutcome LinkFailed(RelativeSyncPath path, LocalOperationFailure failure) =>
        failure is LocalOperationFailure.TargetChanged ? UploadItemOutcomeFactory.CreateDeferred(path) : UploadItemOutcomeFactory.CreateFailed(path, ProviderErrorFactory.CreateUnexpected());

    private Task<UploadItemOutcome> LinkedOrConflictAsync(PassRun run, UploadPlanItem.SendToConflicts conflict, Option<SyncBaselineEntry> link, CancellationToken cancellationToken) =>
        link.Match(
            entry => baseline.UpsertAsync(run.AccountId, [entry], cancellationToken).MatchAsync(_ => UploadItemOutcomeFactory.CreateLinked(conflict.Path), _ => UploadItemOutcomeFactory.CreateFailed(conflict.Path, ProviderErrorFactory.CreateUnexpected())),
            () => Task.FromResult(UploadItemOutcomeFactory.CreateSentToConflicts(conflict.Finding)));

    private async Task<UploadItemOutcome> RunFolderStepAsync(PassRun run, UploadPlanItem item, CancellationToken cancellationToken)
    {
        if (item is UploadPlanItem.LinkExisting link)
            return await RecordFolderAsync(run, item.Path, link.ItemId, Option.None<RemoteItemSnapshot>(), UploadItemOutcomeFactory.CreateLinked(item.Path), cancellationToken).ConfigureAwait(false);

        return await ParentIdOf(run, item.Path).Match(
            parent => run.Providers.Folders.EnsureFolderAsync(run.AccountId, parent, NameOf(item.Path), cancellationToken)
                .MatchAsync(
                    folder => RecordFolderAsync(run, item.Path, folder.Id, IndexRowOf(folder, parent, item.Path), UploadItemOutcomeFactory.CreateFolderCreated(item.Path), cancellationToken),
                    error => Task.FromResult(FolderFailed(run, item.Path, error))),
            failed => Task.FromResult(failed)).ConfigureAwait(false);
    }

    private static UploadItemOutcome FolderFailed(PassRun run, RelativeSyncPath path, ProviderError error)
    {
        run.Observe(error);

        return UploadItemOutcomeFactory.CreateFailed(path, error);
    }

    private static Option<RemoteItemSnapshot> IndexRowOf(RemoteFolder folder, Option<RemoteItemId> parent, RelativeSyncPath path) =>
        folder.Version.Map(version => RemoteItemSnapshotFactory.Create(folder.Id, parent, NameOf(path), RemoteItemKindFactory.CreateFolder(), version.LastModified, version.ETag));

    private Task<UploadItemOutcome> RecordFolderAsync(PassRun run, RelativeSyncPath path, RemoteItemId folderId, Option<RemoteItemSnapshot> indexRow, UploadItemOutcome success, CancellationToken cancellationToken)
    {
        run.Folders[path] = folderId;
        var entry = SyncBaselineEntryFactory.Create(run.AccountId, path, folderId, SyncBaselineKindFactory.CreateFolder());

        return indexRow.Match(snapshot => recorder.RecordAsync(run.AccountId, entry, snapshot, cancellationToken), () => baseline.UpsertAsync(run.AccountId, [entry], cancellationToken))
            .MatchAsync(_ => success, _ => UploadItemOutcomeFactory.CreateFailed(path, ProviderErrorFactory.CreateUnexpected()));
    }

    private async Task UploadStepAsync(PassRun run, UploadPlanItem item, CancellationToken cancellationToken)
    {
        if (run.IsStopped)
            return;

        var progress = run.Tally.ForFile(SizeOf(item));
        var outcome = await UploadOneAsync(run, item, progress, cancellationToken).ConfigureAwait(false);
        if (outcome is UploadItemOutcome.Uploaded)
            progress.Finish();

        run.Outcomes.Add(outcome);
    }

    private Task<UploadItemOutcome> UploadOneAsync(PassRun run, UploadPlanItem item, IProgress<TransferProgress> progress, CancellationToken cancellationToken) => item switch
    {
        UploadPlanItem.UploadReplace replace => SendAsync(run, item, Result.Success<UploadTarget, UploadItemOutcome>(UploadTargetFactory.CreateReplacingItem(replace.ItemId)), replace.Precondition, replace.LastWriteUtc, progress, cancellationToken),
        UploadPlanItem.UploadNewFile created => SendAsync(run, item, ParentIdOf(run, item.Path).Map(UploadTargetFactory.CreateNewFileIn), created.Precondition, created.LastWriteUtc, progress, cancellationToken),
        _ => Task.FromResult(UploadItemOutcomeFactory.CreateFailed(item.Path, ProviderErrorFactory.CreateUnexpected())),
    };

    private Task<UploadItemOutcome> SendAsync(PassRun run, UploadPlanItem item, Result<UploadTarget, UploadItemOutcome> target, UploadPrecondition precondition, DateTimeOffset lastWrite, IProgress<TransferProgress> progress, CancellationToken cancellationToken) =>
        target.Match(
            resolved => run.Providers.Uploader.UploadAsync(run.AccountId, resolved, SourceOf(run, item.Path), precondition, progress, cancellationToken)
                .MatchAsync(uploaded => RecordUploadAsync(run, item.Path, uploaded, lastWrite, cancellationToken), error => Task.FromResult(UploadFailed(run, item, error))),
            failed => Task.FromResult(failed));

    private Task<UploadItemOutcome> RecordUploadAsync(PassRun run, RelativeSyncPath path, UploadedFile uploaded, DateTimeOffset lastWrite, CancellationToken cancellationToken)
    {
        var parent = ParentOf(path).Match(folder => run.Folders.TryGetValue(folder, out var id) ? Option.Some(id) : Option.None<RemoteItemId>(), Option.None<RemoteItemId>);
        var entry = SyncBaselineEntryFactory.Create(run.AccountId, path, uploaded.Id, SyncBaselineKindFactory.CreateFile(uploaded.Size, lastWrite, uploaded.Hash, uploaded.CTag, uploaded.ETag));
        var snapshot = RemoteItemSnapshotFactory.Create(uploaded.Id, parent, NameOf(path), RemoteItemKindFactory.CreateFile(uploaded.Size, Option.None<ContentHash>(), uploaded.CTag), lastWrite, uploaded.ETag);

        return recorder.RecordAsync(run.AccountId, entry, snapshot, cancellationToken)
            .MatchAsync(_ => UploadItemOutcomeFactory.CreateUploaded(path, uploaded.Size), _ => UploadItemOutcomeFactory.CreateFailed(path, ProviderErrorFactory.CreateUnexpected()));
    }

    private async Task<UploadItemOutcome> RunRemovalStepAsync(PassRun run, UploadPlanItem item, CancellationToken cancellationToken)
    {
        if (item is UploadPlanItem.RemoveRemote remove)
            return await run.Providers.Remover.RemoveAsync(run.AccountId, remove.ItemId, remove.Precondition, cancellationToken)
                .MatchAsync(_ => ForgetAsync(run, item.Path, cancellationToken), error => Task.FromResult(RemoveFailed(run, item.Path, error))).ConfigureAwait(false);

        return await ForgetAsync(run, item.Path, cancellationToken).ConfigureAwait(false);
    }

    private static UploadItemOutcome RemoveFailed(PassRun run, RelativeSyncPath path, ProviderError error)
    {
        run.Observe(error);

        return error is ProviderError.PreconditionFailed
            ? UploadItemOutcomeFactory.CreateSentToConflicts(FindingFor(run, path, ConflictKindFactory.CreateLocalDeletedRemoteModified()))
            : UploadItemOutcomeFactory.CreateFailed(path, error);
    }

    private Task<UploadItemOutcome> ForgetAsync(PassRun run, RelativeSyncPath path, CancellationToken cancellationToken) =>
        baseline.RemoveAsync(run.AccountId, [path], cancellationToken)
            .MatchAsync(_ => UploadItemOutcomeFactory.CreateRemovedRemotely(path), _ => UploadItemOutcomeFactory.CreateFailed(path, ProviderErrorFactory.CreateUnexpected()));

    private sealed class PassRun(AccountId accountId, UploadPassInput input, UploadProviders providers, ConcurrentDictionary<RelativeSyncPath, RemoteItemId> folders, PhaseTally tally)
    {
        private readonly Lock gate = new();
        private Option<UploadPassFailure> stop = Option.None<UploadPassFailure>();

        internal AccountId AccountId { get; } = accountId;

        internal UploadPassInput Input { get; } = input;

        internal UploadProviders Providers { get; } = providers;

        internal ConcurrentDictionary<RelativeSyncPath, RemoteItemId> Folders { get; } = folders;

        internal PhaseTally Tally { get; } = tally;

        internal ConcurrentBag<UploadItemOutcome> Outcomes { get; } = [];

        internal Option<UploadPassFailure> Stop
        {
            get
            {
                lock (gate)
                    return stop;
            }
        }

        internal bool IsStopped => Stop.Match(_ => true, () => false);

        internal void Observe(ProviderError error)
        {
            var failure = error switch
            {
                ProviderError.QuotaExceeded => Option.Some(UploadPassFailureFactory.CreateQuotaExceeded()),
                ProviderError.AuthenticationRequired => Option.Some(UploadPassFailureFactory.CreateAuthenticationRequired()),
                _ => Option.None<UploadPassFailure>(),
            };

            lock (gate)
                stop = stop.Match(_ => stop, () => failure);
        }
    }
}
