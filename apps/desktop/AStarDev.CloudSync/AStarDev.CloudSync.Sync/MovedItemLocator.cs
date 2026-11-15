using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Finds what a <see cref="ConflictKind.MoveTargetTaken"/> resolution needs besides the conflicted path (#1626): the baseline row a cloud move left at the item's old path, and the names the cloud already holds beside the item.</summary>
/// <param name="baseline">Reads the baseline by item id.</param>
/// <param name="remoteIndex">Lists the cloud siblings of an item.</param>
internal sealed class MovedItemLocator(ISyncBaselineReader baseline, IRemoteIndexReader remoteIndex)
{
    internal Task<Option<SyncBaselineEntry>> FindBaselineRowAsync(AccountId accountId, RemoteItemId itemId, RelativeSyncPath except, CancellationToken cancellationToken) =>
        baseline.GetByItemAsync(accountId, itemId, cancellationToken)
            .MatchAsync(rows => rows.FirstOrNone(entry => entry.Path != except && entry.Kind is SyncBaselineKind.File), _ => Option.None<SyncBaselineEntry>());

    internal Task<Result<IReadOnlyCollection<string>, ResolutionFailure>> SiblingNamesAsync(AccountId accountId, Option<RemoteItemId> parent, CancellationToken cancellationToken) =>
        remoteIndex.GetChildrenAsync(accountId, parent, cancellationToken).MatchAsync(
            children => Result.Success<IReadOnlyCollection<string>, ResolutionFailure>([.. children.Select(child => child.Name)]),
            _ => Result.Failure<IReadOnlyCollection<string>, ResolutionFailure>(ResolutionFailureFactory.CreateUnexpected()));
}
