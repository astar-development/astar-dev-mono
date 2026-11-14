using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Carries out one <see cref="ResolutionPlan"/> for one path (#1406): transfers through <see cref="ResolutionTransfers"/>, local moves through <see cref="ILocalFileOperations"/>.</summary>
/// <param name="transfers">Runs the uploads, downloads and removals.</param>
/// <param name="files">Moves and trashes local files.</param>
/// <param name="namer">Names the Keep Both copy.</param>
/// <param name="time">Stamps the Keep Both name.</param>
/// <param name="baseline">Removes the baseline row of a trashed file.</param>
internal sealed class ResolutionSteps(ResolutionTransfers transfers, ILocalFileOperations files, IKeepBothNamer namer, TimeProvider time, ISyncBaselineWriter baseline)
{
    private const char Separator = '/';

    internal Task<Result<Unit, ResolutionFailure>> RunAsync(AccountId accountId, SyncAttentionItem.Conflict conflict, ResolutionPlan plan, CancellationToken cancellationToken) => plan switch
    {
        ResolutionPlan.LeaveOpen => Task.FromResult(Done()),
        ResolutionPlan.KeepLocalMoveRemoteAside or ResolutionPlan.ApplyRemoteMoveRenameLocal or ResolutionPlan.KeepBothAtMoveTarget => Task.FromResult(Failed(ResolutionFailureFactory.CreateUnsupported())),
        _ => transfers.LoadAsync(accountId, [conflict.Path], cancellationToken).BindAsync(context => RunLoadedAsync(context, conflict, plan, cancellationToken)),
    };

    private static Result<Unit, ResolutionFailure> Done() => Result.Success<Unit, ResolutionFailure>(Unit.Instance);

    private static Result<Unit, ResolutionFailure> Failed(ResolutionFailure failure) => Result.Failure<Unit, ResolutionFailure>(failure);

    private static RelativeSyncPath SiblingPath(RelativeSyncPath path, string name) => new(path.Value[..(path.Value.LastIndexOf(Separator) + 1)] + name);

    private static Option<ContentTag> SeenTag(SyncAttentionItem.Conflict conflict) => conflict.Remote is SideSnapshot.Present present ? present.Tag : Option.None<ContentTag>();

    private Task<Result<Unit, ResolutionFailure>> RunLoadedAsync(ResolutionContext context, SyncAttentionItem.Conflict conflict, ResolutionPlan plan, CancellationToken cancellationToken) => RunPathAsync(context, conflict.Path, SeenTag(conflict), plan, cancellationToken);

    private Task<Result<Unit, ResolutionFailure>> RunPathAsync(ResolutionContext context, RelativeSyncPath path, Option<ContentTag> seen, ResolutionPlan plan, CancellationToken cancellationToken) => plan switch
    {
        ResolutionPlan.UploadReplacingRemote => transfers.ReplaceAsync(context, path, seen, cancellationToken),
        ResolutionPlan.UploadAsNewFile => transfers.UploadNewAsync(context, path, cancellationToken),
        ResolutionPlan.DownloadOverLocal => transfers.DownloadAsync(context, path, seen, cancellationToken),
        ResolutionPlan.DeleteRemote => RemoveRemoteAsync(context, path, seen, cancellationToken),
        ResolutionPlan.TrashLocal => TrashAsync(context, path, cancellationToken),
        ResolutionPlan.KeepBothUploadAndDownload => KeepBothAsync(context, path, seen, cancellationToken),
        _ => Task.FromResult(Failed(ResolutionFailureFactory.CreateUnsupported())),
    };

    private async Task<Result<Unit, ResolutionFailure>> RemoveRemoteAsync(ResolutionContext context, RelativeSyncPath path, Option<ContentTag> seen, CancellationToken cancellationToken)
    {
        var removed = await transfers.RemoveRemoteAsync(context, path, seen, cancellationToken).ConfigureAwait(false);

        return await removed.BindAsync(_ => ForgetAsync(context, path, cancellationToken)).ConfigureAwait(false);
    }

    private Task<Result<Unit, ResolutionFailure>> ForgetAsync(ResolutionContext context, RelativeSyncPath path, CancellationToken cancellationToken) =>
        baseline.RemoveAsync(context.AccountId, [path], cancellationToken).MatchAsync(_ => Done(), _ => Failed(ResolutionFailureFactory.CreateNotRecorded()));

    private Task<Result<Unit, ResolutionFailure>> TrashAsync(ResolutionContext context, RelativeSyncPath path, CancellationToken cancellationToken) =>
        files.TrashAsync(context.Input.Root, ResolutionTransfers.LocalFile(context, path), cancellationToken)
            .MatchAsync(_ => ForgetAsync(context, path, cancellationToken), failure => Task.FromResult(Failed(ResolutionFailureFactory.CreateLocal(failure))));

    private Task<Result<Unit, ResolutionFailure>> KeepBothAsync(ResolutionContext context, RelativeSyncPath path, Option<ContentTag> seen, CancellationToken cancellationToken)
    {
        var local = ResolutionTransfers.LocalFile(context, path);
        if (!local.Exists)
            return transfers.DownloadAsync(context, path, seen, cancellationToken);

        var folder = local.Directory ?? context.Input.Root;

        return Try.Run(() => (IReadOnlyCollection<string>)[.. folder.EnumerateFileSystemInfos().Select(entry => entry.Name)])
            .Match(names => RenameAsync(context, path, local, names, seen, cancellationToken), _ => Task.FromResult(Failed(ResolutionFailureFactory.CreateLocal(LocalOperationFailureFactory.CreateUnexpected()))));
    }

    private Task<Result<Unit, ResolutionFailure>> RenameAsync(ResolutionContext context, RelativeSyncPath path, IFileInfo local, IReadOnlyCollection<string> names, Option<ContentTag> seen, CancellationToken cancellationToken)
    {
        var copyPath = SiblingPath(path, namer.Name(local.Name, time.GetUtcNow(), names));

        return files.MoveAsync(local, ResolutionTransfers.FullPath(context, copyPath), cancellationToken)
            .MatchAsync(_ => UploadCopyAsync(context, path, copyPath, seen, cancellationToken), failure => Task.FromResult(Failed(ResolutionFailureFactory.CreateLocal(failure))));
    }

    private async Task<Result<Unit, ResolutionFailure>> UploadCopyAsync(ResolutionContext context, RelativeSyncPath path, RelativeSyncPath copyPath, Option<ContentTag> seen, CancellationToken cancellationToken)
    {
        var uploaded = await transfers.UploadNewAsync(context, copyPath, cancellationToken).ConfigureAwait(false);

        return await uploaded.MatchAsync(_ => transfers.DownloadAsync(context, path, seen, cancellationToken), failure => RollBackAsync(context, path, copyPath, failure)).ConfigureAwait(false);
    }

    private Task<Result<Unit, ResolutionFailure>> RollBackAsync(ResolutionContext context, RelativeSyncPath path, RelativeSyncPath copyPath, ResolutionFailure failure) =>
        files.MoveAsync(ResolutionTransfers.LocalFile(context, copyPath), ResolutionTransfers.FullPath(context, path), CancellationToken.None).MatchAsync(_ => Failed(failure), _ => Failed(failure));
}
