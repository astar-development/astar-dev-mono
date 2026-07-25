using System.Reactive;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

/// <summary>Implementation of <see cref="IGraphService"/> that delegates drive-context resolution to <see cref="DriveContextCache"/> and recursive folder enumeration to <see cref="GraphFolderEnumerator"/>.</summary>
internal sealed class GraphService(IUploadService uploadService, IGraphClientFactory graphClientFactory, DriveContextCache driveContextCache, GraphFolderEnumerator graphFolderEnumerator) : IGraphService
{
    private const string RootPathMarker = "root:";
    private const string DownloadUrlKey = "@microsoft.graph.downloadUrl";

    private static readonly string[] childrenSelect =
    [
        "id", "name", "folder", "file", "size", "lastModifiedDateTime", "parentReference",
        "eTag", "cTag", DownloadUrlKey
    ];

    /// <inheritdoc />
    public async Task<Result<DriveId, string>> GetDriveIdAsync(string accountId, Func<CancellationToken, Task<string>> tokenFactory, CancellationToken cancellationToken = default)
    {
        try
        {
            var contextResult = await driveContextCache.ResolveAsync(accountId, tokenFactory, cancellationToken).ConfigureAwait(false);

            return contextResult.Match(
                ctx => new Ok<DriveId, string>(ctx.Ctx.DriveId),
                error => (Result<DriveId, string>)new Fail<DriveId, string>(error));
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not SyncReAuthRequiredException)
        {
            return new Fail<DriveId, string>(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<DriveFolder>, string>> GetRootFoldersAsync(string accountId, Func<CancellationToken, Task<string>> tokenFactory, CancellationToken cancellationToken = default)
    {
        try
        {
            var contextResult = await driveContextCache.ResolveAsync(accountId, tokenFactory, cancellationToken).ConfigureAwait(false);

            return await contextResult.MatchAsync(
                async ctx =>
                {
                    var firstPage = await ctx.Client.Drives[ctx.Ctx.DriveId.Value].Items[ctx.Ctx.RootId].Children
                        .GetAsync(req => req.QueryParameters.Select = childrenSelect, cancellationToken).ConfigureAwait(false);

                    var folders = await PaginateDriveFoldersAsync(
                        firstPage,
                        nextLink => ctx.Client.Drives[ctx.Ctx.DriveId.Value].Items[ctx.Ctx.RootId].Children
                            .WithUrl(nextLink)
                            .GetAsync(cancellationToken: cancellationToken)).ConfigureAwait(false);

                    return (Result<List<DriveFolder>, string>)new Ok<List<DriveFolder>, string>([.. folders.OrderBy(f => f.Name)]);
                },
                error => new Fail<List<DriveFolder>, string>(error)).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not SyncReAuthRequiredException)
        {
            return new Fail<List<DriveFolder>, string>(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<DriveFolder>, string>> GetChildFoldersAsync(Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, string parentFolderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = graphClientFactory.CreateClient(tokenFactory);

            var firstPage = await client.Drives[driveId.Value].Items[parentFolderId].Children
                .GetAsync(req =>
                {
                    req.QueryParameters.Select = ["id", "name", "folder", "parentReference"];
                    req.QueryParameters.Top = 100;
                }, cancellationToken).ConfigureAwait(false);

            var folders = await PaginateDriveFoldersAsync(
                firstPage,
                nextLink => client.Drives[driveId.Value].Items[parentFolderId].Children
                    .WithUrl(nextLink)
                    .GetAsync(cancellationToken: cancellationToken)).ConfigureAwait(false);

            return new Ok<List<DriveFolder>, string>([.. folders.OrderBy(f => f.Name)]);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not SyncReAuthRequiredException)
        {
            return new Fail<List<DriveFolder>, string>(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<(long Total, long Used), string>> GetQuotaAsync(string accountId, Func<CancellationToken, Task<string>> tokenFactory, CancellationToken cancellationToken = default)
    {
        try
        {
            var contextResult = await driveContextCache.ResolveAsync(accountId, tokenFactory, cancellationToken).ConfigureAwait(false);

            return await contextResult.MatchAsync(
                async ctx =>
                {
                    var drive = await ctx.Client.Drives[ctx.Ctx.DriveId.Value]
                        .GetAsync(req => req.QueryParameters.Select = ["quota"], cancellationToken).ConfigureAwait(false);

                    var quota = drive?.Quota is { Total: not null, Used: not null }
                        ? (drive.Quota.Total!.Value, drive.Quota.Used!.Value)
                        : (0L, 0L);

                    return (Result<(long Total, long Used), string>)new Ok<(long Total, long Used), string>(quota);
                },
                error => new Fail<(long Total, long Used), string>(error)).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not SyncReAuthRequiredException)
        {
            return new Fail<(long Total, long Used), string>(ex.Message);
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<DeltaItem> EnumerateFolderAsync(Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, string folderId, string remotePath, Action<int>? onItemDiscovered = null, CancellationToken cancellationToken = default)
        => graphFolderEnumerator.EnumerateFolderAsync(tokenFactory, driveId, folderId, remotePath, onItemDiscovered, cancellationToken);

    /// <inheritdoc />
    public async Task<string?> GetFolderIdByPathAsync(Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, string remotePath, CancellationToken cancellationToken = default)
    {
        var client = graphClientFactory.CreateClient(tokenFactory);

        try
        {
            var item = await client.Drives[driveId.Value].Items[$"{RootPathMarker}:{remotePath}"]
                .GetAsync(req => req.QueryParameters.Select = ["id"], cancellationToken).ConfigureAwait(false);

            return item?.Id;
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 404)
        {
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not SyncReAuthRequiredException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<Result<string, string>> GetDownloadUrlAsync(string accountId, Func<CancellationToken, Task<string>> tokenFactory, string itemId, CancellationToken cancellationToken = default)
    {
        try
        {
            var contextResult = await driveContextCache.ResolveAsync(accountId, tokenFactory, cancellationToken).ConfigureAwait(false);

            return await contextResult.MatchAsync(
                async ctx =>
                {
                    var item = await ctx.Client.Drives[ctx.Ctx.DriveId.Value].Items[itemId]
                        .GetAsync(req => req.QueryParameters.Select = [DownloadUrlKey], cancellationToken)
                        .ConfigureAwait(false);

                    if (item?.AdditionalData is null)
                        return (Result<string, string>)new Fail<string, string>($"No download URL available for item {itemId}.");

                    if (!item.AdditionalData.TryGetValue(DownloadUrlKey, out object? url) || url is null)
                        return new Fail<string, string>($"No download URL available for item {itemId}.");

                    return new Ok<string, string>(url.ToString()!);
                },
                error => new Fail<string, string>(error)).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not SyncReAuthRequiredException)
        {
            return new Fail<string, string>(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<string, string>> UploadFileAsync(string accountId, Func<CancellationToken, Task<string>> tokenFactory, string localPath, string remotePath, string parentFolderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var contextResult = await driveContextCache.ResolveAsync(accountId, tokenFactory, cancellationToken).ConfigureAwait(false);

            return await contextResult.MatchAsync(
                async ctx => await uploadService.UploadAsync(ctx.Client, ctx.Ctx.DriveId, parentFolderId, localPath, remotePath, cancellationToken: cancellationToken).ConfigureAwait(false),
                error => new Fail<string, string>(error)).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not SyncReAuthRequiredException)
        {
            return new Fail<string, string>(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<Unit, string>> DeleteItemAsync(string accountId, Func<CancellationToken, Task<string>> tokenFactory, string itemId, CancellationToken cancellationToken = default)
    {
        var contextResult = await driveContextCache.ResolveAsync(accountId, tokenFactory, cancellationToken).ConfigureAwait(false);

        return await contextResult.MatchAsync(
            async ctx =>
            {
                try
                {
                    await ctx.Client.Drives[ctx.Ctx.DriveId.Value].Items[itemId].DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                    return (Result<Unit, string>)new Ok<Unit, string>(Unit.Default);
                }
                catch (Exception ex) when (ex is not OperationCanceledException and not SyncReAuthRequiredException)
                {
                    return new Fail<Unit, string>(ex.Message);
                }
            },
            error => new Fail<Unit, string>(error)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void EvictCachedDriveContext(string accountId) => driveContextCache.Evict(accountId);

    private static async Task<List<DriveFolder>> PaginateDriveFoldersAsync(DriveItemCollectionResponse? firstPage, Func<string, Task<DriveItemCollectionResponse?>> fetchNext)
    {
        List<DriveFolder> folders = [];
        var page = firstPage;
        while (page?.Value is not null)
        {
            folders.AddRange(
                page.Value
                    .Where(i => i.Folder is not null)
                    .Select(i => new DriveFolder(Id: i.Id!, Name: i.Name!, ParentId: ToOptionString(i.ParentReference?.Id))));

            if (!OdataNextLinkGuard.IsSafe(page.OdataNextLink))
                break;

            page = await fetchNext(page.OdataNextLink!).ConfigureAwait(false);
        }

        return folders;
    }

    private static Option<string> ToOptionString(string? value) => value is not null ? Option.Some(value) : Option.None<string>();
}
