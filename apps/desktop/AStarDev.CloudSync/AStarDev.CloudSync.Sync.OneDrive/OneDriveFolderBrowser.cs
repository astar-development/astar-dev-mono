using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>Lists the folders of a OneDrive folder through Microsoft Graph, one level per call, following every page.</summary>
/// <param name="graphAccess">Gives the account's drive id, access token and Graph client.</param>
/// <param name="timeProvider">Turns a Retry-After date into a delay.</param>
/// <param name="logger">Reports the outcome with the hashed account id, status, Graph error code and page count only.</param>
internal sealed class OneDriveFolderBrowser(OneDriveGraphAccess graphAccess, TimeProvider timeProvider, ILogger<OneDriveFolderBrowser> logger) : IRemoteFolderBrowser
{
    private const string RootSegment = "root";
    private const int PageSize = 200;
    private static readonly string[] Fields = ["id", "name", "folder", "package", "remoteItem", "parentReference"];

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RemoteFolder>, ProviderError>> GetChildFoldersAsync(AccountId accountId, Option<RemoteItemId> parent, CancellationToken cancellationToken)
    {
        using var scope = SyncLogScope.Begin(logger, accountId);

        return await graphAccess.DriveOfAsync(accountId, cancellationToken)
            .BindAsync(driveId => graphAccess.TokenOfAsync(accountId, outcome => OneDriveBrowseLog.TokenUnavailable(logger, outcome), cancellationToken).BindAsync(token => ListAsync(accountId, driveId, token, parent, cancellationToken)))
            .ConfigureAwait(false);
    }

    private static Result<IReadOnlyList<RemoteFolder>, ProviderError> Failed(ProviderError error) => Result.Failure<IReadOnlyList<RemoteFolder>, ProviderError>(error);

    private static IEnumerable<RemoteFolder> ToFolders(DriveItemCollectionResponse? page, string requestedParent) =>
        (page?.Value ?? []).Where(item => item.Folder is not null && item.Package is null && item.RemoteItem is null).SelectMany(item => ToFolder(item, requestedParent));

    private static RemoteFolder[] ToFolder(DriveItem item, string requestedParent) =>
        RemoteItemIdFactory.Create(item.Id).Match(
            id => RemoteItemIdFactory.Create(item.ParentReference?.Id ?? requestedParent).Match(
                parentId => new[] { RemoteFolderFactory.Create(id, item.Name, parentId, ChildrenOf(item)) },
                _ => []),
            _ => []);

    private static RemoteFolderChildren ChildrenOf(DriveItem item) => (item.Folder?.ChildCount ?? 0) > 0 ? RemoteFolderChildrenFactory.CreateMayHaveChildFolders() : RemoteFolderChildrenFactory.CreateNoChildFolders();

    private static void Configure(RequestConfiguration<Microsoft.Graph.Drives.Item.Items.Item.Children.ChildrenRequestBuilder.ChildrenRequestBuilderGetQueryParameters> configuration)
    {
        configuration.QueryParameters.Select = Fields;
        configuration.QueryParameters.Top = PageSize;
    }

    private async Task<Result<IReadOnlyList<RemoteFolder>, ProviderError>> ListAsync(AccountId accountId, string driveId, string accessToken, Option<RemoteItemId> parent, CancellationToken cancellationToken)
    {
        var progress = new PageProgress();
        var reading = ReadAllPagesAsync(accountId, driveId, accessToken, parent, progress, cancellationToken);
        await ((Task)reading).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        if (cancellationToken.IsCancellationRequested)
        {
            OneDriveBrowseLog.ListingCancelled(logger);
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (reading.IsCanceled)
            return Faulted(new TaskCanceledException(), progress);

        return reading.Exception is { InnerException: { } exception } ? Faulted(exception, progress) : reading.Result;
    }

    private async Task<Result<IReadOnlyList<RemoteFolder>, ProviderError>> ReadAllPagesAsync(AccountId accountId, string driveId, string accessToken, Option<RemoteItemId> parent, PageProgress progress, CancellationToken cancellationToken)
    {
        using var graph = graphAccess.CreateClient(accountId, accessToken);
        var parentSegment = parent.Match(id => id.Value, () => RootSegment);
        var children = graph.Drives[driveId].Items[parentSegment].Children;
        var folders = new List<RemoteFolder>();
        var seenLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var page = await children.GetAsync(Configure, cancellationToken).ConfigureAwait(false);

        while (true)
        {
            progress.Pages++;
            folders.AddRange(ToFolders(page, parentSegment));
            var next = page?.OdataNextLink;
            if (string.IsNullOrEmpty(next))
                break;

            if (!ODataNextLinkGuard.IsAllowed(next) || !seenLinks.Add(next))
            {
                OneDriveBrowseLog.NextLinkRefused(logger, progress.Pages);

                return Failed(ProviderErrorFactory.CreateUnexpected());
            }

            page = await children.WithUrl(next).GetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        OneDriveBrowseLog.ListingCompleted(logger, folders.Count, progress.Pages);

        return Result.Success<IReadOnlyList<RemoteFolder>, ProviderError>([.. folders.OrderBy(folder => folder.Name, StringComparer.OrdinalIgnoreCase)]);
    }

    private Result<IReadOnlyList<RemoteFolder>, ProviderError> Faulted(Exception exception, PageProgress progress)
    {
        var (error, statusCode, errorCode) = GraphErrorMapper.Map(exception, timeProvider);
        OneDriveBrowseLog.ListingFailed(logger, statusCode, errorCode, progress.Pages, exception.GetType().Name);

        return Failed(error);
    }

    private sealed class PageProgress
    {
        public int Pages { get; set; }
    }
}
