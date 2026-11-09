using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="remoteBrowsers">Finds the account's cloud folder browser.</param>
/// <param name="localBrowser">Lists the folders on disk.</param>
/// <param name="rules">Reads the stored selection rules.</param>
/// <param name="evaluator">Judges each folder against the rules.</param>
public sealed class FolderTreeSource(IProviderCapability<IRemoteFolderBrowser> remoteBrowsers, ILocalFolderBrowser localBrowser, ISelectionRuleReader rules, ISyncSelectionEvaluator evaluator) : IFolderTreeSource
{
    private const char Separator = '/';

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>> GetChildrenAsync(AccountId accountId, FolderTreePosition parent, CancellationToken cancellationToken)
    {
        var (parentPath, remoteId, isRoot) = parent switch
        {
            FolderTreePosition.Folder folder => (folder.Path, folder.RemoteId, false),
            _ => (LocalRelativePathFactory.Root, Option.None<RemoteItemId>(), true),
        };
        var remoteListing = ListRemoteAsync(accountId, remoteId, isRoot, cancellationToken);
        var localListing = ListLocalAsync(accountId, parentPath, isRoot, cancellationToken);
        var ruleListing = rules.GetAsync(accountId, cancellationToken);
        await Task.WhenAll(remoteListing, localListing, ruleListing).ConfigureAwait(false);

        return Combine(parentPath, remoteListing.Result, localListing.Result, ruleListing.Result);
    }

    private static Result<IReadOnlyList<FolderTreeNode>, FolderTreeFailure> Failed(FolderTreeFailure failure) => Result.Failure<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>(failure);

    private static LocalRelativePath ChildPath(LocalRelativePath parent, string name) =>
        LocalRelativePathFactory.Create(string.IsNullOrEmpty(parent.Value) ? name : parent.Value + Separator + name).Match(path => path, _ => parent);

    private static FolderChildren Combined(IEnumerable<LocalFolder> localMatches, Option<RemoteFolder> remoteMatch) =>
        localMatches.Any(folder => folder.Children is LocalFolderChildren.HasChildFolders) || remoteMatch.Match(folder => folder.Children is RemoteFolderChildren.MayHaveChildFolders, () => false)
            ? FolderChildrenFactory.CreateMayHaveChildFolders()
            : FolderChildrenFactory.CreateNoChildFolders();

    private static FolderOrigin OriginOf(bool onDisk, bool inCloud) => (onDisk, inCloud) switch
    {
        (true, true) => FolderOriginFactory.CreateBoth(),
        (true, false) => FolderOriginFactory.CreateLocalOnly(),
        _ => FolderOriginFactory.CreateRemoteOnly(),
    };

    private async Task<Result<IReadOnlyList<RemoteFolder>, ProviderError>> ListRemoteAsync(AccountId accountId, Option<RemoteItemId> remoteId, bool isRoot, CancellationToken cancellationToken)
    {
        if (!isRoot && remoteId.Match(_ => false, () => true))
            return Result.Success<IReadOnlyList<RemoteFolder>, ProviderError>([]);

        var browser = await remoteBrowsers.ForAccountAsync(accountId, cancellationToken).ConfigureAwait(false);

        return await browser.Match(found => found.GetChildFoldersAsync(accountId, remoteId, cancellationToken), error => Task.FromResult(Result.Failure<IReadOnlyList<RemoteFolder>, ProviderError>(error))).ConfigureAwait(false);
    }

    private async Task<Result<IReadOnlyList<LocalFolder>, LocalFolderError>> ListLocalAsync(AccountId accountId, LocalRelativePath parentPath, bool isRoot, CancellationToken cancellationToken)
    {
        var listing = await localBrowser.GetChildFoldersAsync(accountId, parentPath, cancellationToken).ConfigureAwait(false);

        return !isRoot && listing.Match(_ => false, error => error is LocalFolderError.NotFound)
            ? Result.Success<IReadOnlyList<LocalFolder>, LocalFolderError>([])
            : listing;
    }

    private Result<IReadOnlyList<FolderTreeNode>, FolderTreeFailure> Combine(
        LocalRelativePath parentPath,
        Result<IReadOnlyList<RemoteFolder>, ProviderError> remote,
        Result<IReadOnlyList<LocalFolder>, LocalFolderError> local,
        Result<IReadOnlyList<SelectionRule>, ErrorResponse> ruleList) =>
        remote.Match(
            remoteFolders => local.Match(
                localFolders => ruleList.Match(
                    stored => Result.Success<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>(Merge(parentPath, localFolders, remoteFolders, stored)),
                    error => Failed(FolderTreeFailureFactory.CreateRulesFailed(error))),
                localError => Failed(FolderTreeFailureFactory.CreateLocalFailed(localError))),
            remoteError => local.Match(
                _ => Failed(FolderTreeFailureFactory.CreateRemoteFailed(remoteError)),
                localError => Failed(FolderTreeFailureFactory.CreateBothFailed(remoteError, localError))));

    private IReadOnlyList<FolderTreeNode> Merge(LocalRelativePath parentPath, IReadOnlyList<LocalFolder> localFolders, IReadOnlyList<RemoteFolder> remoteFolders, IReadOnlyList<SelectionRule> stored)
    {
        var localByName = localFolders.ToLookup(folder => folder.Name, StringComparer.OrdinalIgnoreCase);
        var remoteByName = remoteFolders.ToLookup(folder => folder.Name, StringComparer.OrdinalIgnoreCase);
        var names = localFolders.Select(folder => folder.Name).Concat(remoteFolders.Select(folder => folder.Name)).Distinct(StringComparer.OrdinalIgnoreCase);

        return [.. names.Select(name => ToNode(parentPath, [.. localByName[name].OrderBy(folder => folder.Name, StringComparer.Ordinal)], remoteByName[name].FirstOrNone(), name, stored)).OrderBy(node => node.Name, StringComparer.OrdinalIgnoreCase)];
    }

    private FolderTreeNode ToNode(LocalRelativePath parentPath, List<LocalFolder> localMatches, Option<RemoteFolder> remoteMatch, string name, IReadOnlyList<SelectionRule> stored)
    {
        var displayName = localMatches.Count > 0 ? localMatches[0].Name : name;
        var path = localMatches.Count > 0 ? localMatches[0].Path : ChildPath(parentPath, displayName);

        return FolderTreeNodeFactory.Create(
            displayName,
            path,
            remoteMatch.Map(folder => folder.Id),
            OriginOf(localMatches.Count > 0, remoteMatch.Match(_ => true, () => false)),
            Combined(localMatches, remoteMatch),
            evaluator.Evaluate(path, stored),
            localMatches.Count > 1 ? FolderNameClashFactory.CreateCaseClash() : FolderNameClashFactory.CreateUnique());
    }
}
