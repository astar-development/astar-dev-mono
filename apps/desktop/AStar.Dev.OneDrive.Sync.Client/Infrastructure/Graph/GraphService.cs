using System.Reactive;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using Microsoft.Kiota.Abstractions;
using Microsoft.Graph.Models;

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
    public async Task<Result<DriveId, string>> GetDriveIdAsync(string accountId, Func<CancellationToken, Task<string>> tokenFactory, CancellationToken ct = default)
    {
        try
        {
            var contextResult = await driveContextCache.ResolveAsync(accountId, tokenFactory, ct).ConfigureAwait(false);

            return contextResult.Match<Result<DriveId, string>>(
                ctx => new Result<DriveId, string>.Ok(ctx.Ctx.DriveId),
                error => new Result<DriveId, string>.Error(error));
        }
        catch(Exception ex) when(ex is not OperationCanceledException and not SyncReAuthRequiredException)
        {
            return new Result<DriveId, string>.Error(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<DriveFolder>, string>> GetRootFoldersAsync(string accountId, Func<CancellationToken, Task<string>> tokenFactory, CancellationToken ct = default)
    {
        try
        {
            var contextResult = await driveContextCache.ResolveAsync(accountId, tokenFactory, ct).ConfigureAwait(false);

            return await contextResult.MatchAsync<Result<List<DriveFolder>, string>>(
                async ctx =>
                {
                    var firstPage = await ctx.Client.Drives[ctx.Ctx.DriveId.Value].Items[ctx.Ctx.RootId].Children
                        .GetAsync(req => req.QueryParameters.Select = childrenSelect, ct).ConfigureAwait(false);

                    var folders = await PaginateDriveFoldersAsync(
                        firstPage,
                        nextLink => ctx.Client.Drives[ctx.Ctx.DriveId.Value].Items[ctx.Ctx.RootId].Children
                            .WithUrl(nextLink)
                            .GetAsync(cancellationToken: ct)).ConfigureAwait(false);

                    return new Result<List<DriveFolder>, string>.Ok([.. folders.OrderBy(f => f.Name)]);
                },
                error => new Result<List<DriveFolder>, string>.Error(error)).ConfigureAwait(false);
        }
        catch(Exception ex) when(ex is not OperationCanceledException and not SyncReAuthRequiredException)
        {
            return new Result<List<DriveFolder>, string>.Error(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<DriveFolder>, string>> GetChildFoldersAsync(Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, string parentFolderId, CancellationToken ct = default)
    {
        try
        {
            var client = graphClientFactory.CreateClient(tokenFactory);

            var firstPage = await client.Drives[driveId.Value].Items[parentFolderId].Children
                .GetAsync(req =>
                {
                    req.QueryParameters.Select = ["id", "name", "folder", "parentReference"];
                    req.QueryParameters.Top = 100;
                }, ct).ConfigureAwait(false);

            var folders = await PaginateDriveFoldersAsync(
                firstPage,
                nextLink => client.Drives[driveId.Value].Items[parentFolderId].Children
                    .WithUrl(nextLink)
                    .GetAsync(cancellationToken: ct)).ConfigureAwait(false);

            return new Result<List<DriveFolder>, string>.Ok([.. folders.OrderBy(f => f.Name)]);
        }
        catch(Exception ex) when(ex is not OperationCanceledException and not SyncReAuthRequiredException)
        {
            return new Result<List<DriveFolder>, string>.Error(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<(long Total, long Used), string>> GetQuotaAsync(string accountId, Func<CancellationToken, Task<string>> tokenFactory, CancellationToken ct = default)
    {
        try
        {
            var contextResult = await driveContextCache.ResolveAsync(accountId, tokenFactory, ct).ConfigureAwait(false);

            return await contextResult.MatchAsync<Result<(long Total, long Used), string>>(
                async ctx =>
                {
                    var drive = await ctx.Client.Drives[ctx.Ctx.DriveId.Value]
                        .GetAsync(req => req.QueryParameters.Select = ["quota"], ct).ConfigureAwait(false);

                    var quota = drive?.Quota is { Total: not null, Used: not null }
                        ? (drive.Quota.Total!.Value, drive.Quota.Used!.Value)
                        : (0L, 0L);

                    return new Result<(long Total, long Used), string>.Ok(quota);
                },
                error => new Result<(long Total, long Used), string>.Error(error)).ConfigureAwait(false);
        }
        catch(Exception ex) when(ex is not OperationCanceledException and not SyncReAuthRequiredException)
        {
            return new Result<(long Total, long Used), string>.Error(ex.Message);
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<DeltaItem> EnumerateFolderAsync(Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, string folderId, string remotePath, Action<int>? onItemDiscovered = null, CancellationToken ct = default)
        => graphFolderEnumerator.EnumerateFolderAsync(tokenFactory, driveId, folderId, remotePath, onItemDiscovered, ct);

    /// <inheritdoc />
    public async Task<string?> GetFolderIdByPathAsync(Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, string remotePath, CancellationToken ct = default)
    {
        var client = graphClientFactory.CreateClient(tokenFactory);

        try
        {
            var item = await client.Drives[driveId.Value].Items[$"{RootPathMarker}:{remotePath}"]
                .GetAsync(req => req.QueryParameters.Select = ["id"], ct).ConfigureAwait(false);

            return item?.Id;
        }
        catch(ApiException ex) when(ex.ResponseStatusCode == 404)
        {
            return null;
        }
        catch(Exception ex) when(ex is not OperationCanceledException and not SyncReAuthRequiredException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<Result<string, string>> GetDownloadUrlAsync(string accountId, Func<CancellationToken, Task<string>> tokenFactory, string itemId, CancellationToken ct = default)
    {
        try
        {
            var contextResult = await driveContextCache.ResolveAsync(accountId, tokenFactory, ct).ConfigureAwait(false);

            return await contextResult.MatchAsync<Result<string, string>>(
                async ctx =>
                {
                    var item = await ctx.Client.Drives[ctx.Ctx.DriveId.Value].Items[itemId]
                        .GetAsync(req => req.QueryParameters.Select = [DownloadUrlKey], ct)
                        .ConfigureAwait(false);

                    if(item?.AdditionalData is null)
                        return new Result<string, string>.Error($"No download URL available for item {itemId}.");

                    if(!item.AdditionalData.TryGetValue(DownloadUrlKey, out object? url) || url is null)
                        return new Result<string, string>.Error($"No download URL available for item {itemId}.");

                    return new Result<string, string>.Ok(url.ToString()!);
                },
                error => new Result<string, string>.Error(error)).ConfigureAwait(false);
        }
        catch(Exception ex) when(ex is not OperationCanceledException and not SyncReAuthRequiredException)
        {
            return new Result<string, string>.Error(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<string, string>> UploadFileAsync(string accountId, Func<CancellationToken, Task<string>> tokenFactory, string localPath, string remotePath, string parentFolderId, CancellationToken ct = default)
    {
        try
        {
            var contextResult = await driveContextCache.ResolveAsync(accountId, tokenFactory, ct).ConfigureAwait(false);

            return await contextResult.MatchAsync(
                async ctx => await uploadService.UploadAsync(ctx.Client, ctx.Ctx.DriveId, parentFolderId, localPath, remotePath, ct: ct).ConfigureAwait(false),
                error => new Result<string, string>.Error(error)).ConfigureAwait(false);
        }
        catch(Exception ex) when(ex is not OperationCanceledException and not SyncReAuthRequiredException)
        {
            return new Result<string, string>.Error(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<Unit, string>> DeleteItemAsync(string accountId, Func<CancellationToken, Task<string>> tokenFactory, string itemId, CancellationToken ct = default)
    {
        var contextResult = await driveContextCache.ResolveAsync(accountId, tokenFactory, ct).ConfigureAwait(false);

        return await contextResult.MatchAsync<Result<Unit, string>>(
            async ctx =>
            {
                try
                {
                    await ctx.Client.Drives[ctx.Ctx.DriveId.Value].Items[itemId].DeleteAsync(cancellationToken: ct).ConfigureAwait(false);

                    return new Result<Unit, string>.Ok(Unit.Default);
                }
                catch(Exception ex) when(ex is not OperationCanceledException and not SyncReAuthRequiredException)
                {
                    return new Result<Unit, string>.Error(ex.Message);
                }
            },
            error => new Result<Unit, string>.Error(error)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void EvictCachedDriveContext(string accountId) => driveContextCache.Evict(accountId);

    private static async Task<List<DriveFolder>> PaginateDriveFoldersAsync(DriveItemCollectionResponse? firstPage, Func<string, Task<DriveItemCollectionResponse?>> fetchNext)
    {
        List<DriveFolder> folders = [];
        var page = firstPage;
        while(page?.Value is not null)
        {
            folders.AddRange(
                page.Value
                    .Where(i => i.Folder is not null)
                    .Select(i => new DriveFolder(Id: i.Id!, Name: i.Name!, ParentId: ToOptionString(i.ParentReference?.Id))));

            if(!OdataNextLinkGuard.IsSafe(page.OdataNextLink))
                break;

            page = await fetchNext(page.OdataNextLink!).ConfigureAwait(false);
        }

        return folders;
    }

    private static Option<string> ToOptionString(string? value) => value is not null ? Option.Some(value) : Option.None<string>();
}
