using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Carries out the three <see cref="ConflictKind.MoveTargetTaken"/> plans (#1615). The cloud item sits at the target path in the remote index and its baseline row is still at the old path, so after a plan the next sync pass follows the move on its own; nothing here rewrites that row.</summary>
/// <param name="transfers">Checks the cloud item and uploads the renamed copy.</param>
/// <param name="renamers">Finds the cloud rename capability.</param>
/// <param name="files">Moves the local item aside.</param>
/// <param name="namer">Names the Keep Both copy.</param>
/// <param name="time">Stamps the Keep Both name.</param>
/// <param name="baseline">Drops the baseline row of the item that used to sit at the target.</param>
internal sealed class MoveTargetSteps(ResolutionTransfers transfers, IProviderCapability<IRemoteItemRenamer> renamers, ILocalFileOperations files, IKeepBothNamer namer, TimeProvider time, ISyncBaselineWriter baseline)
{
    internal Task<Result<Unit, ResolutionFailure>> RenameRemoteAsync(ResolutionContext context, RelativeSyncPath path, Option<ContentTag> seen, CancellationToken cancellationToken) =>
        ResolutionTransfers.CheckedFile(context, path, seen).Match(
            found => NamesIn(context, path).Match(names => RenameItemAsync(context, path, found.Item, names, cancellationToken), failure => Task.FromResult(Failed(failure))),
            failure => Task.FromResult(Failed(failure)));

    internal Task<Result<Unit, ResolutionFailure>> RenameLocalAsync(ResolutionContext context, RelativeSyncPath path, Option<ContentTag> seen, CancellationToken cancellationToken) =>
        ResolutionTransfers.CheckedFile(context, path, seen).Match(
            found => MoveAsideAsync(context, path, cancellationToken).BindAsync(_ => ForgetOtherAsync(context, path, found.Item.Id, cancellationToken)),
            failure => Task.FromResult(Failed(failure)));

    internal Task<Result<Unit, ResolutionFailure>> RenameLocalAndUploadAsync(ResolutionContext context, RelativeSyncPath path, Option<ContentTag> seen, CancellationToken cancellationToken) =>
        ResolutionTransfers.CheckedFile(context, path, seen).Match(
            found => MoveAsideAsync(context, path, cancellationToken)
                .BindAsync(copy => copy.Match(copyPath => UploadOrRollBackAsync(context, path, copyPath, cancellationToken), () => Task.FromResult(Done())))
                .BindAsync(_ => ForgetOtherAsync(context, path, found.Item.Id, cancellationToken)),
            failure => Task.FromResult(Failed(failure)));

    private static Result<Unit, ResolutionFailure> Done() => Result.Success<Unit, ResolutionFailure>(Unit.Instance);

    private static Result<Unit, ResolutionFailure> Failed(ResolutionFailure failure) => Result.Failure<Unit, ResolutionFailure>(failure);

    private static Result<IReadOnlyCollection<string>, ResolutionFailure> NamesIn(ResolutionContext context, RelativeSyncPath path)
    {
        var folder = ResolutionTransfers.LocalFile(context, path).Directory ?? context.Input.Root;

        return Try.Run(() => (IReadOnlyCollection<string>)[.. folder.EnumerateFileSystemInfos().Select(entry => entry.Name)])
            .Match(Result.Success<IReadOnlyCollection<string>, ResolutionFailure>, _ => Result.Failure<IReadOnlyCollection<string>, ResolutionFailure>(ResolutionFailureFactory.CreateLocal(LocalOperationFailureFactory.CreateUnexpected())));
    }

    private async Task<Result<Unit, ResolutionFailure>> RenameItemAsync(ResolutionContext context, RelativeSyncPath path, RemoteItemSnapshot item, IReadOnlyCollection<string> names, CancellationToken cancellationToken)
    {
        var newName = namer.Name(item.Name, time.GetUtcNow(), names);
        var forgotten = await ForgetOtherAsync(context, path, item.Id, cancellationToken).ConfigureAwait(false);

        return await forgotten.BindAsync(_ => renamers.ForAccountAsync(context.AccountId, cancellationToken)
            .BindAsync(renamer => renamer.RenameAsync(context.AccountId, item.Id, newName, cancellationToken))
            .MatchAsync(_ => Done(), error => Failed(ResolutionFailureFactory.CreateTransfer(error)))).ConfigureAwait(false);
    }

    private Task<Result<Option<RelativeSyncPath>, ResolutionFailure>> MoveAsideAsync(ResolutionContext context, RelativeSyncPath path, CancellationToken cancellationToken)
    {
        if (ResolutionTransfers.LocalDirectory(context, path).Exists)
            return Task.FromResult(Result.Failure<Option<RelativeSyncPath>, ResolutionFailure>(ResolutionFailureFactory.CreateUnsupported()));

        var local = ResolutionTransfers.LocalFile(context, path);
        if (!local.Exists)
            return Task.FromResult(Result.Success<Option<RelativeSyncPath>, ResolutionFailure>(Option.None<RelativeSyncPath>()));

        return NamesIn(context, path).Match(
            names => MoveNamedAsync(context, path, local, names, cancellationToken),
            failure => Task.FromResult(Result.Failure<Option<RelativeSyncPath>, ResolutionFailure>(failure)));
    }

    private Task<Result<Option<RelativeSyncPath>, ResolutionFailure>> MoveNamedAsync(ResolutionContext context, RelativeSyncPath path, IFileInfo local, IReadOnlyCollection<string> names, CancellationToken cancellationToken)
    {
        var copyPath = ResolutionSteps.SiblingPath(path, namer.Name(local.Name, time.GetUtcNow(), names));

        return files.MoveAsync(local, ResolutionTransfers.FullPath(context, copyPath), cancellationToken)
            .MatchAsync(_ => Result.Success<Option<RelativeSyncPath>, ResolutionFailure>(Option.Some(copyPath)), failure => Result.Failure<Option<RelativeSyncPath>, ResolutionFailure>(ResolutionFailureFactory.CreateLocal(failure)));
    }

    private async Task<Result<Unit, ResolutionFailure>> UploadOrRollBackAsync(ResolutionContext context, RelativeSyncPath path, RelativeSyncPath copyPath, CancellationToken cancellationToken)
    {
        var uploaded = await transfers.UploadNewAsync(context, copyPath, cancellationToken).ConfigureAwait(false);

        return await uploaded.MatchAsync(_ => Task.FromResult(Done()), failure => RollBackAsync(context, path, copyPath, failure)).ConfigureAwait(false);
    }

    private Task<Result<Unit, ResolutionFailure>> RollBackAsync(ResolutionContext context, RelativeSyncPath path, RelativeSyncPath copyPath, ResolutionFailure failure) =>
        files.MoveAsync(ResolutionTransfers.LocalFile(context, copyPath), ResolutionTransfers.FullPath(context, path), CancellationToken.None).MatchAsync(_ => Failed(failure), _ => Failed(failure));

    private Task<Result<Unit, ResolutionFailure>> ForgetOtherAsync(ResolutionContext context, RelativeSyncPath path, RemoteItemId movedItem, CancellationToken cancellationToken) =>
        context.Input.Baseline.TryGetValue(path, out var row) && row.ItemId != movedItem
            ? baseline.RemoveAsync(context.AccountId, [path], cancellationToken).MatchAsync(_ => Done(), _ => Failed(ResolutionFailureFactory.CreateNotRecorded()))
            : Task.FromResult(Done());
}
