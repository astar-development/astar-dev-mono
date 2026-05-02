using System.Collections.Concurrent;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

public sealed class GraphService(IUploadService uploadService, IGraphClientFactory graphClientFactory) : IGraphService
{
    private const string RootPathMarker = "root:";
    private const string DownloadUrlKey = "@microsoft.graph.downloadUrl";

    private static readonly string[] ChildrenSelect =
    [
        "id", "name", "folder", "file", "size", "lastModifiedDateTime", "parentReference",
        "eTag", "cTag", DownloadUrlKey
    ];

    private readonly ConcurrentDictionary<string, DriveContext> _cache = [];

    /// <inheritdoc />
    public async Task<string> GetDriveIdAsync(string accountId, string accessToken, CancellationToken ct = default)
        => (await ResolveClientWithDriveContextAsync(accountId, accessToken, ct)).Ctx.DriveId;

    /// <inheritdoc />
    public async Task<List<DriveFolder>> GetRootFoldersAsync(string accountId, string accessToken, CancellationToken ct = default)
    {
        (var client, var driveContext) = await ResolveClientWithDriveContextAsync(accountId, accessToken, ct);

        var response = await client.Drives[driveContext.DriveId].Items[driveContext.RootId].Children
            .GetAsync(req => req.QueryParameters.Select = ChildrenSelect, ct);

        List<DriveFolder> folders = [];

        var page = response;
        while(page?.Value is not null)
        {
            folders.AddRange(
                page.Value
                    .Where(i => i.Folder is not null)
                    .Select(i => new DriveFolder(Id: i.Id!, Name: i.Name!, ParentId: i.ParentReference?.Id)));

            if(page.OdataNextLink is null)
                break;

            page = await client.Drives[driveContext.DriveId].Items[driveContext.RootId].Children
                .WithUrl(page.OdataNextLink)
                .GetAsync(cancellationToken: ct);
        }

        return [.. folders.OrderBy(f => f.Name)];
    }

    /// <inheritdoc />
    public async Task<List<DriveFolder>> GetChildFoldersAsync(string accessToken, string driveId, string parentFolderId, CancellationToken ct = default)
    {
        var client = graphClientFactory.CreateClient(accessToken);

        var result = await client.Drives[driveId].Items[parentFolderId].Children
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
                    .Select(i => new DriveFolder(Id: i.Id!, Name: i.Name!, ParentId: i.ParentReference?.Id)));

            if(page.OdataNextLink is null)
                break;

            page = await client.Drives[driveId].Items[parentFolderId].Children
                .WithUrl(page.OdataNextLink)
                .GetAsync(cancellationToken: ct);
        }

        return [.. folders.OrderBy(f => f.Name)];
    }

    /// <inheritdoc />
    public async Task<(long Total, long Used)> GetQuotaAsync(string accountId, string accessToken, CancellationToken ct = default)
    {
        (var client, var ctx) = await ResolveClientWithDriveContextAsync(accountId, accessToken, ct);

        var drive = await client.Drives[ctx.DriveId]
            .GetAsync(req => req.QueryParameters.Select = ["quota"], ct);

        return drive?.Quota is { Total: not null, Used: not null }
            ? (drive.Quota.Total!.Value, drive.Quota.Used!.Value)
            : (0L, 0L);
    }

    /// <inheritdoc />
    public async Task<List<DeltaItem>> EnumerateFolderAsync(string accessToken, string driveId, string folderId, string remotePath, CancellationToken ct = default)
    {
        var client = graphClientFactory.CreateClient(accessToken);
        List<DeltaItem> items = [];
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await EnumerateSubFolderAsync(client, driveId, folderId, remotePath, items, visited, ct);

        return items;
    }

    /// <inheritdoc />
    public async Task<string?> GetFolderIdByPathAsync(string accessToken, string driveId, string remotePath, CancellationToken ct = default)
    {
        var client = graphClientFactory.CreateClient(accessToken);

        try
        {
            var item = await client.Drives[driveId].Items[$"{RootPathMarker}:{remotePath}"]
                .GetAsync(req => req.QueryParameters.Select = ["id"], ct);

            return item?.Id;
        }
        catch(ApiException ex) when(ex.ResponseStatusCode == 404)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetDownloadUrlAsync(string accountId, string accessToken, string itemId, CancellationToken ct = default)
    {
        (var client, var ctx) = await ResolveClientWithDriveContextAsync(accountId, accessToken, ct);

        var item = await client.Drives[ctx.DriveId].Items[itemId]
            .GetAsync(req => req.QueryParameters.Select = [DownloadUrlKey], ct)
            .ConfigureAwait(false);

        if(item?.AdditionalData is null)
            return null;

        return item.AdditionalData.TryGetValue(DownloadUrlKey, out var url)
            ? url?.ToString()
            : null;
    }

    /// <inheritdoc />
    public async Task<string> UploadFileAsync(string accountId, string accessToken, string localPath, string remotePath, string parentFolderId, CancellationToken ct = default)
    {
        (var client, var ctx) = await ResolveClientWithDriveContextAsync(accountId, accessToken, ct);

        return await uploadService.UploadAsync(client, ctx.DriveId, parentFolderId, localPath, remotePath, ct: ct);
    }

    /// <inheritdoc />
    public async Task DeleteItemAsync(string accountId, string accessToken, string itemId, CancellationToken ct = default)
    {
        (var client, var ctx) = await ResolveClientWithDriveContextAsync(accountId, accessToken, ct);
        await client.Drives[ctx.DriveId].Items[itemId].DeleteAsync(cancellationToken: ct);
    }

    private static async Task EnumerateSubFolderAsync(GraphServiceClient client, string driveId, string parentId, string relativePath, List<DeltaItem> items, HashSet<string> visited, CancellationToken ct)
    {
        if(!visited.Add(parentId))
            return;

        var page = await client.Drives[driveId].Items[parentId].Children
            .GetAsync(req => req.QueryParameters.Select = ChildrenSelect, ct);

        while(page?.Value is not null)
        {
            foreach(var item in page.Value)
            {
                string itemPath = BuildRelativePath(relativePath, item);

                items.Add(MapToDeltaItem(item, itemPath));

                if (item.Folder is not null && item.Id is not null)
                    await EnumerateSubFolderAsync(client, driveId, item.Id, itemPath, items, visited, ct);
            }

            if (page.OdataNextLink is null)
                break;

            page = await client.Drives[driveId].Items[parentId].Children
                .WithUrl(page.OdataNextLink)
                .GetAsync(cancellationToken: ct);
        }
    }

    private static string BuildRelativePath(string relativePath, DriveItem item) => string.IsNullOrEmpty(relativePath)
                        ? item.Name ?? string.Empty
                        : $"{relativePath}/{item.Name}";
    private static DeltaItem MapToDeltaItem(DriveItem item, string itemPath) =>
         new(item.Id!, item.ParentReference?.DriveId ?? string.Empty, item.Name ?? string.Empty, item.ParentReference?.Id,
            IsFolder: item.Folder is not null, IsDeleted: false, item.Size ?? 0L, item.LastModifiedDateTime, DownloadUrl: ExtractDownloadUrl(item),
            RelativePath: itemPath, item.ETag, item.CTag);

    private static string? ExtractDownloadUrl(DriveItem item)
        => item.AdditionalData?.TryGetValue(DownloadUrlKey, out var url) is true
            ? url?.ToString()
            : null;

    private async Task<(GraphServiceClient Client, DriveContext Ctx)> ResolveClientWithDriveContextAsync(string accountId, string accessToken, CancellationToken ct)
    {
        var client = graphClientFactory.CreateClient(accessToken);

        if(_cache.TryGetValue(accountId, out var cached))
            return (client, cached);

        var drive = await client.Me.Drive.GetAsync(cancellationToken: ct);

        string driveId = drive?.Id ?? throw new InvalidOperationException("Could not retrieve drive ID.");

        var root = await client.Drives[driveId].Root.GetAsync(cancellationToken: ct);

        string rootId = root?.Id ?? throw new InvalidOperationException("Could not retrieve root item ID.");

        var driveContext = new DriveContext(driveId, rootId);
        _cache[accountId] = driveContext;

        return (client, driveContext);
    }

    private sealed record DriveContext(string DriveId, string RootId);
}
