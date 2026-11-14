using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Carries out the confirm and restore of one held deletion set (#1406).</summary>
/// <param name="transfers">Runs the uploads, downloads and removals.</param>
/// <param name="files">Trashes local items and creates local folders.</param>
/// <param name="baselineReader">Reads the baseline row of a held path.</param>
/// <param name="baselineWriter">Removes the baseline rows of trashed items.</param>
internal sealed class HeldDeletionSteps(ResolutionTransfers transfers, ILocalFileOperations files, ISyncBaselineReader baselineReader, ISyncBaselineWriter baselineWriter)
{
    internal Task<Result<Unit, ResolutionFailure>> ConfirmAsync(ResolutionContext context, SyncAttentionItem.HeldDeletions held, CancellationToken cancellationToken) =>
        held.Direction is DeletionDirection.DeleteLocal ? TrashAllAsync(context, held, cancellationToken) : RemoveAllRemoteAsync(context, held, cancellationToken);

    internal Task<Result<Unit, ResolutionFailure>> RestoreAsync(ResolutionContext context, SyncAttentionItem.HeldDeletions held, CancellationToken cancellationToken) =>
        held.Direction is DeletionDirection.DeleteLocal ? UploadAllAsync(context, held, cancellationToken) : DownloadAllAsync(context, held, cancellationToken);

    private static Result<Unit, ResolutionFailure> Done() => Result.Success<Unit, ResolutionFailure>(Unit.Instance);

    private static Result<Unit, ResolutionFailure> Failed(ResolutionFailure failure) => Result.Failure<Unit, ResolutionFailure>(failure);

    private static IEnumerable<RelativeSyncPath> ShallowFirst(SyncAttentionItem.HeldDeletions held) => held.Paths.OrderBy(path => path.Value.Length);

    private static async Task<Result<Unit, ResolutionFailure>> EachAsync(IEnumerable<RelativeSyncPath> paths, Func<RelativeSyncPath, Task<Result<Unit, ResolutionFailure>>> step)
    {
        foreach (var path in paths)
        {
            var done = await step(path).ConfigureAwait(false);
            if (done.Match(_ => false, _ => true))
                return done;
        }

        return Done();
    }

    private async Task<Result<Unit, ResolutionFailure>> TrashAllAsync(ResolutionContext context, SyncAttentionItem.HeldDeletions held, CancellationToken cancellationToken)
    {
        var trashed = await EachAsync(held.Paths, path => TrashOneAsync(context, path, cancellationToken)).ConfigureAwait(false);

        return await trashed.BindAsync(_ => baselineWriter.RemoveAsync(context.AccountId, [.. held.Paths], cancellationToken).MatchAsync(_ => Done(), _ => Failed(ResolutionFailureFactory.CreateNotRecorded()))).ConfigureAwait(false);
    }

    private async Task<Result<Unit, ResolutionFailure>> TrashOneAsync(ResolutionContext context, RelativeSyncPath path, CancellationToken cancellationToken)
    {
        var entry = await baselineReader.GetAsync(context.AccountId, path, cancellationToken).ConfigureAwait(false);

        return await entry.MatchAsync(
            row => files.TrashAsync(context.Input.Root, row.Match(found => found.Kind is SyncBaselineKind.Folder, () => false) ? ResolutionTransfers.LocalDirectory(context, path) : ResolutionTransfers.LocalFile(context, path), cancellationToken)
                .MatchAsync(_ => Done(), failure => Failed(ResolutionFailureFactory.CreateLocal(failure))),
            _ => Task.FromResult(Failed(ResolutionFailureFactory.CreateUnexpected()))).ConfigureAwait(false);
    }

    private async Task<Result<Unit, ResolutionFailure>> RemoveAllRemoteAsync(ResolutionContext context, SyncAttentionItem.HeldDeletions held, CancellationToken cancellationToken)
    {
        List<UploadPlanItem> items = [];
        foreach (var path in held.Paths.OrderByDescending(candidate => candidate.Value.Length))
        {
            var entry = await baselineReader.GetAsync(context.AccountId, path, cancellationToken).ConfigureAwait(false);
            var item = entry.Match(row => row.Match(found => RemovalOf(context, found), () => Option.None<UploadPlanItem>()), _ => Option.None<UploadPlanItem>());
            _ = item.Match(planned => { items.Add(planned); return Unit.Instance; }, () => Unit.Instance);
        }

        return await transfers.RunAsync(context, items, cancellationToken).ConfigureAwait(false);
    }

    private static Option<UploadPlanItem> RemovalOf(ResolutionContext context, SyncBaselineEntry entry) => entry.Kind switch
    {
        SyncBaselineKind.File file => RemovePreconditionFactory.CreateMustMatchContent(file.CTag).Match(precondition => Option.Some(UploadPlanItemFactory.CreateRemoveRemote(entry.Path, entry.ItemId, precondition)), _ => Option.None<UploadPlanItem>()),
        _ => ResolutionTransfers.RemoteItem(context, entry.Path).Match(
            remote => RemovePreconditionFactory.CreateMustMatch(remote.ETag).Match(precondition => Option.Some(UploadPlanItemFactory.CreateRemoveRemote(entry.Path, entry.ItemId, precondition)), _ => Option.None<UploadPlanItem>()),
            () => Option.Some(UploadPlanItemFactory.CreateForgetBaseline(entry.Path))),
    };

    private Task<Result<Unit, ResolutionFailure>> UploadAllAsync(ResolutionContext context, SyncAttentionItem.HeldDeletions held, CancellationToken cancellationToken)
    {
        List<UploadPlanItem> items = [];
        foreach (var path in ShallowFirst(held))
        {
            var file = ResolutionTransfers.LocalFile(context, path);
            if (file.Exists)
                items.Add(UploadPlanItemFactory.CreateUploadNewFile(path, file.Length, new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero)));
            else if (ResolutionTransfers.LocalDirectory(context, path).Exists)
                items.Add(UploadPlanItemFactory.CreateEnsureRemoteFolder(path));
        }

        return transfers.RunAsync(context, items, cancellationToken);
    }

    private Task<Result<Unit, ResolutionFailure>> DownloadAllAsync(ResolutionContext context, SyncAttentionItem.HeldDeletions held, CancellationToken cancellationToken) =>
        EachAsync(ShallowFirst(held), path => DownloadOneAsync(context, path, cancellationToken));

    private Task<Result<Unit, ResolutionFailure>> DownloadOneAsync(ResolutionContext context, RelativeSyncPath path, CancellationToken cancellationToken)
    {
        if (ResolutionTransfers.LocalFile(context, path).Exists || ResolutionTransfers.LocalDirectory(context, path).Exists)
            return Task.FromResult(Done());

        return ResolutionTransfers.RemoteItem(context, path).Match(
            remote => remote.Kind is RemoteItemKind.Folder
                ? files.CreateFolderAsync(ResolutionTransfers.LocalDirectory(context, path), cancellationToken).MatchAsync(_ => Done(), failure => Failed(ResolutionFailureFactory.CreateLocal(failure)))
                : transfers.DownloadAsync(context, path, cancellationToken),
            () => Task.FromResult(Failed(ResolutionFailureFactory.CreateRemoteFileMissing())));
    }
}
