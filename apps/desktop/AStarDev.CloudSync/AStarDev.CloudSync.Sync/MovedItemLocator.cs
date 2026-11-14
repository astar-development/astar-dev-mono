using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Finds what a <see cref="ConflictKind.MoveTargetTaken"/> resolution needs besides the conflicted path (#1626): the baseline row a cloud move left at the item's old path, and the names the cloud already holds beside the item.</summary>
/// <param name="baseline">Pages through the baseline.</param>
/// <param name="remoteIndex">Lists the cloud siblings of an item.</param>
internal sealed class MovedItemLocator(ISyncBaselineReader baseline, IRemoteIndexReader remoteIndex)
{
    private const int PageSize = 500;

    internal Task<Option<SyncBaselineEntry>> FindBaselineRowAsync(AccountId accountId, RemoteItemId itemId, RelativeSyncPath except, CancellationToken cancellationToken) =>
        SearchAsync(accountId, itemId, except, Option.None<RelativeSyncPath>(), cancellationToken);

    internal Task<Result<IReadOnlyCollection<string>, ResolutionFailure>> SiblingNamesAsync(AccountId accountId, Option<RemoteItemId> parent, CancellationToken cancellationToken) =>
        remoteIndex.GetChildrenAsync(accountId, parent, cancellationToken).MatchAsync(
            children => Result.Success<IReadOnlyCollection<string>, ResolutionFailure>([.. children.Select(child => child.Name)]),
            _ => Result.Failure<IReadOnlyCollection<string>, ResolutionFailure>(ResolutionFailureFactory.CreateUnexpected()));

    private Task<Option<SyncBaselineEntry>> SearchAsync(AccountId accountId, RemoteItemId itemId, RelativeSyncPath except, Option<RelativeSyncPath> after, CancellationToken cancellationToken) =>
        baseline.GetUnderAsync(accountId, new RelativeSyncPath(string.Empty), after, PageSize, cancellationToken)
            .MatchAsync(page => ScanAsync(page, accountId, itemId, except, cancellationToken), _ => Task.FromResult(Option.None<SyncBaselineEntry>()));

    private async Task<Option<SyncBaselineEntry>> ScanAsync(IReadOnlyList<SyncBaselineEntry> page, AccountId accountId, RemoteItemId itemId, RelativeSyncPath except, CancellationToken cancellationToken)
    {
        if (page.Count == 0)
            return Option.None<SyncBaselineEntry>();

        var found = page.FirstOrNone(entry => entry.ItemId == itemId && entry.Path != except && entry.Kind is SyncBaselineKind.File);

        return found is Option<SyncBaselineEntry>.Some ? found : await SearchAsync(accountId, itemId, except, Option.Some(page[^1].Path), cancellationToken).ConfigureAwait(false);
    }
}
