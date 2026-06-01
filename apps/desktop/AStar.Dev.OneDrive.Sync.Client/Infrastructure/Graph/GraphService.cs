using System.Collections.Concurrent;
using System.Reactive;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

public sealed class GraphService(IUploadService uploadService, IGraphClientFactory graphClientFactory) : IGraphService
{
    private const string RootPathMarker = "root:";
    private const string DownloadUrlKey = "@microsoft.graph.downloadUrl";

    private static readonly string[] _childrenSelect =
    [
        "id", "name", "folder", "file", "size", "lastModifiedDateTime", "parentReference",
        "eTag", "cTag", DownloadUrlKey
    ];

    private readonly ConcurrentDictionary<string, DriveContext> _cache = [];

    /// <inheritdoc />
    public async Task<Result<DriveId, string>> GetDriveIdAsync(string accountId, string accessToken, CancellationToken ct = default)
    {
        var contextResult = await ResolveClientWithDriveContextAsync(accountId, accessToken, ct).ConfigureAwait(false);

        return contextResult.Match<Result<DriveId, string>>(
            ctx => new Result<DriveId, string>.Ok(ctx.Ctx.DriveId),
            error => new Result<DriveId, string>.Error(error));
    }

    /// <inheritdoc />
    public async Task<Result<List<DriveFolder>, string>> GetRootFoldersAsync(string accountId, string accessToken, CancellationToken ct = default)
    {
        var contextResult = await ResolveClientWithDriveContextAsync(accountId, accessToken, ct).ConfigureAwait(false);

        return await contextResult.MatchAsync<Result<List<DriveFolder>, string>>(
            async ctx =>
            {
                var response = await ctx.Client.Drives[ctx.Ctx.DriveId.Value].Items[ctx.Ctx.RootId].Children
                    .GetAsync(req => req.QueryParameters.Select = _childrenSelect, ct);

                List<DriveFolder> folders = [];

                var page = response;
                while(page?.Value is not null)
                {
                    folders.AddRange(
                        page.Value
                            .Where(i => i.Folder is not null)
                            .Select(i => new DriveFolder(Id: i.Id!, Name: i.Name!, ParentId: ToOptionString(i.ParentReference?.Id))));

                    if(page.OdataNextLink is null)
                        break;

                    page = await ctx.Client.Drives[ctx.Ctx.DriveId.Value].Items[ctx.Ctx.RootId].Children
                        .WithUrl(page.OdataNextLink)
                        .GetAsync(cancellationToken: ct);
                }

                return new Result<List<DriveFolder>, string>.Ok([.. folders.OrderBy(f => f.Name)]);
            },
            error => new Result<List<DriveFolder>, string>.Error(error));
    }

    /// <inheritdoc />
    public async Task<Result<List<DriveFolder>, string>> GetChildFoldersAsync(string accessToken, DriveId driveId, string parentFolderId, CancellationToken ct = default)
    {
        var client = graphClientFactory.CreateClient(accessToken);

        var result = await client.Drives[driveId.Value].Items[parentFolderId].Children
            .GetAsync(req =>
            {
                req.QueryParameters.Select = ["id", "name", "folder", "parentReference"];
                req.QueryParameters.Top = 100;
            }, ct);

        List<DriveFolder> folders = [];

        var page = result;
        while(page?.Value is not null)
        {
            folders.AddRange(
                page.Value
                    .Where(i => i.Folder is not null)
                    .Select(i => new DriveFolder(Id: i.Id!, Name: i.Name!, ParentId: ToOptionString(i.ParentReference?.Id))));

            if(page.OdataNextLink is null)
                break;

            page = await client.Drives[driveId.Value].Items[parentFolderId].Children
                .WithUrl(page.OdataNextLink)
                .GetAsync(cancellationToken: ct);
        }

        return new Result<List<DriveFolder>, string>.Ok([.. folders.OrderBy(f => f.Name)]);
    }

    /// <inheritdoc />
    public async Task<Result<(long Total, long Used), string>> GetQuotaAsync(string accountId, string accessToken, CancellationToken ct = default)
    {
        var contextResult = await ResolveClientWithDriveContextAsync(accountId, accessToken, ct).ConfigureAwait(false);

        return await contextResult.MatchAsync<Result<(long Total, long Used), string>>(
            async ctx =>
            {
                var drive = await ctx.Client.Drives[ctx.Ctx.DriveId.Value]
                    .GetAsync(req => req.QueryParameters.Select = ["quota"], ct);

                var quota = drive?.Quota is { Total: not null, Used: not null }
                    ? (drive.Quota.Total!.Value, drive.Quota.Used!.Value)
                    : (0L, 0L);

                return new Result<(long Total, long Used), string>.Ok(quota);
            },
            error => new Result<(long Total, long Used), string>.Error(error));
    }

    /// <inheritdoc />
    public async Task<Result<List<DeltaItem>, string>> EnumerateFolderAsync(string accessToken, DriveId driveId, string folderId, string remotePath, Action<int>? onItemDiscovered = null, CancellationToken ct = default)
    {
        var client = graphClientFactory.CreateClient(accessToken);
        List<DeltaItem> items = [];
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await EnumerateSubFolderAsync(client, driveId, folderId, remotePath, items, visited, onItemDiscovered, ct);

        return new Result<List<DeltaItem>, string>.Ok(items);
    }

    /// <inheritdoc />
    public async Task<string?> GetFolderIdByPathAsync(string accessToken, DriveId driveId, string remotePath, CancellationToken ct = default)
    {
        var client = graphClientFactory.CreateClient(accessToken);

        try
        {
            var item = await client.Drives[driveId.Value].Items[$"{RootPathMarker}:{remotePath}"]
                .GetAsync(req => req.QueryParameters.Select = ["id"], ct);

            return item?.Id;
        }
        catch(ApiException ex) when(ex.ResponseStatusCode == 404)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<Result<string, string>> GetDownloadUrlAsync(string accountId, string accessToken, string itemId, CancellationToken ct = default)
    {
        var contextResult = await ResolveClientWithDriveContextAsync(accountId, accessToken, ct).ConfigureAwait(false);

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
            error => new Result<string, string>.Error(error));
    }

    /// <inheritdoc />
    public async Task<Result<string, string>> UploadFileAsync(string accountId, string accessToken, string localPath, string remotePath, string parentFolderId, CancellationToken ct = default)
    {
        var contextResult = await ResolveClientWithDriveContextAsync(accountId, accessToken, ct).ConfigureAwait(false);

        return await contextResult.MatchAsync<Result<string, string>>(
            async ctx => await uploadService.UploadAsync(ctx.Client, ctx.Ctx.DriveId, parentFolderId, localPath, remotePath, ct: ct).ConfigureAwait(false),
            error => new Result<string, string>.Error(error));
    }

    /// <inheritdoc />
    public async Task<Result<Unit, string>> DeleteItemAsync(string accountId, string accessToken, string itemId, CancellationToken ct = default)
    {
        var contextResult = await ResolveClientWithDriveContextAsync(accountId, accessToken, ct).ConfigureAwait(false);

        return await contextResult.MatchAsync<Result<Unit, string>>(
            async ctx =>
            {
                await ctx.Client.Drives[ctx.Ctx.DriveId.Value].Items[itemId].DeleteAsync(cancellationToken: ct);
                return new Result<Unit, string>.Ok(Unit.Default);
            },
            error => new Result<Unit, string>.Error(error));
    }

    private static async Task EnumerateSubFolderAsync(GraphServiceClient client, DriveId driveId, string parentId, string relativePath, List<DeltaItem> items, HashSet<string> visited, Action<int>? onItemDiscovered, CancellationToken ct)
    {
        if(!visited.Add(parentId))
            return;

        var page = await client.Drives[driveId.Value].Items[parentId].Children
            .GetAsync(req => req.QueryParameters.Select = _childrenSelect, ct);

        while(page?.Value is not null)
        {
            foreach(var item in page.Value)
            {
                string itemPath = BuildRelativePath(relativePath, item);

                items.Add(MapToDeltaItem(item, itemPath));
                onItemDiscovered?.Invoke(items.Count);

                if(item.Folder is not null && item.Id is not null)
                    await EnumerateSubFolderAsync(client, driveId, item.Id, itemPath, items, visited, onItemDiscovered, ct);
            }

            if(page.OdataNextLink is null)
                break;

            page = await client.Drives[driveId.Value].Items[parentId].Children
                .WithUrl(page.OdataNextLink)
                .GetAsync(cancellationToken: ct);
        }
    }

    private static string BuildRelativePath(string relativePath, DriveItem item) => string.IsNullOrEmpty(relativePath)
                        ? item.Name ?? string.Empty
                        : $"{relativePath}/{item.Name}";

    private static DeltaItem MapToDeltaItem(DriveItem item, string itemPath)
    {
        var id = new OneDriveItemId(item.Id!);
        var driveId = new DriveId(item.ParentReference?.DriveId ?? string.Empty);
        var parentId = item.ParentReference?.Id is string pid ? Option.Some(new OneDriveFolderId(pid)) : Option.None<OneDriveFolderId>();
        var path = ItemPathFactory.Create(item.Name ?? string.Empty, itemPath);
        var versionInfo = VersionInfoFactory.Create(ToOptionString(item.ETag), ToOptionString(item.CTag));

        if (item.Folder is not null)
            return DeltaItemFactory.CreateFolder(id, driveId, parentId, path, versionInfo);

        return DeltaItemFactory.CreateFile(id, driveId, parentId, path, item.Size ?? 0L, item.LastModifiedDateTime.ToOption(), ExtractDownloadUrl(item), versionInfo);
    }

    private static Option<string> ExtractDownloadUrl(DriveItem item)
    {
        if(item.AdditionalData?.TryGetValue(DownloadUrlKey, out object? url) is not true || url is null)
            return Option.None<string>();

        return Option.Some(url.ToString()!);
    }

    private static Option<string> ToOptionString(string? value) => value is not null ? Option.Some(value) : Option.None<string>();

    /// <inheritdoc />
    public void EvictCachedDriveContext(string accountId) => _cache.TryRemove(accountId, out _);

    private async Task<Result<(GraphServiceClient Client, DriveContext Ctx), string>> ResolveClientWithDriveContextAsync(string accountId, string accessToken, CancellationToken ct)
    {
        var client = graphClientFactory.CreateClient(accessToken);

        if(_cache.TryGetValue(accountId, out var cached))
            return new Result<(GraphServiceClient Client, DriveContext Ctx), string>.Ok((client, cached));

        var drive = await client.Me.Drive.GetAsync(cancellationToken: ct);

        if(drive?.Id is null)
            return new Result<(GraphServiceClient Client, DriveContext Ctx), string>.Error("Could not retrieve drive ID.");

        var driveId = new DriveId(drive.Id);
        var root = await client.Drives[driveId.Value].Root.GetAsync(cancellationToken: ct);

        if(root?.Id is null)
            return new Result<(GraphServiceClient Client, DriveContext Ctx), string>.Error("Could not retrieve root item ID.");

        var driveContext = new DriveContext(driveId, root.Id);
        _cache[accountId] = driveContext;

        return new Result<(GraphServiceClient Client, DriveContext Ctx), string>.Ok((client, driveContext));
    }

    private sealed record DriveContext(DriveId DriveId, string RootId);
}
