using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.Delta;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Graph;

public sealed class GraphService : IGraphService
{
    private readonly UploadService _uploadService = new();
    private readonly Dictionary<string, DriveContext> _cache = [];

    /// <inheritdoc />
    public async Task<string> GetDriveIdAsync(string accessToken, CancellationToken ct = default)
        => (await ResolveClientWithDriveContextAsync(accessToken, ct)).Ctx.DriveId;

    /// <inheritdoc />
    public async Task<List<DriveFolder>> GetRootFoldersAsync(string accessToken, CancellationToken ct = default)
    {
        (var client, var driveContext) = await ResolveClientWithDriveContextAsync(accessToken, ct);

        var driveItemCollectionResponse = await client.Drives[driveContext.DriveId].Items[driveContext.RootId].Children
            .GetAsync(req => req.QueryParameters.Select =    ["id", "name", "folder", "file", "size",     "lastModifiedDateTime", "parentReference",     "@microsoft.graph.downloadUrl"], ct);

        List<DriveFolder> folders = [];

        var page = driveItemCollectionResponse;
        while(page?.Value is not null)
        {
            folders.AddRange(
                page.Value
                    .Where(i => i.Folder is not null)
                    .Select(i => new DriveFolder(
                        Id: i.Id!,
                        Name: i.Name!,
                        ParentId: i.ParentReference?.Id)));

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
        var client = BuildClient(accessToken);

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
                    .Select(i => new DriveFolder(
                        Id: i.Id!,
                        Name: i.Name!,
                        ParentId: i.ParentReference?.Id)));

            if(page.OdataNextLink is null)
                break;

            page = await client.Drives[driveId].Items[parentFolderId].Children
                .WithUrl(page.OdataNextLink)
                .GetAsync(cancellationToken: ct);
        }

        return [.. folders.OrderBy(f => f.Name)];
    }

    /// <inheritdoc />
    public async Task<(long Total, long Used)> GetQuotaAsync(string accessToken, CancellationToken ct = default)
    {
        (var client, var ctx) = await ResolveClientWithDriveContextAsync(accessToken, ct);

        var drive = await client.Drives[ctx.DriveId]
            .GetAsync(req => req.QueryParameters.Select = ["quota"], ct);

        return drive?.Quota is { Total: not null, Used: not null }
            ? (drive.Quota.Total!.Value, drive.Quota.Used!.Value)
            : (0L, 0L);
    }

    /// <inheritdoc />
    public async Task<DeltaResult> GetDeltaAsync(string accessToken, string folderId, string? deltaLink, CancellationToken ct = default)
    {
        (var client, var ctx) = await ResolveClientWithDriveContextAsync(accessToken, ct);

        var folderItem = await client.Drives[ctx.DriveId]
        .Items[folderId]
        .GetAsync(req =>
            req.QueryParameters.Select = ["id", "name"], ct);

        string folderName = folderItem?.Name ?? string.Empty;

        if(deltaLink is null)
        {
            return await FullEnumerationAsync(
            client, ctx.DriveId, folderId, folderName, ct);
        }

        List<DeltaItem> items = [];
        string? nextDeltaLink = null;
        bool hasMorePages      = false;

        var page = await client.Drives[ctx.DriveId].Items[folderId].Delta
                                            .WithUrl(deltaLink)
                                            .GetAsDeltaGetResponseAsync(cancellationToken: ct);

        while(page?.Value is not null)
        {
            foreach(var item in page.Value)
                items.Add(MapToDeltaItem(item));

            if(page.OdataNextLink is not null)
            {
                hasMorePages = true;
                page = await client.Drives[ctx.DriveId].Items[folderId].Delta
                    .WithUrl(page.OdataNextLink)
                    .GetAsDeltaGetResponseAsync(cancellationToken: ct);
            }
            else
            {
                nextDeltaLink = page.OdataDeltaLink;
                break;
            }
        }

        return new DeltaResult(items, nextDeltaLink, hasMorePages);
    }

    /// <inheritdoc />
    public async Task<string> UploadFileAsync(string accessToken, string localPath, string remotePath, string parentFolderId, CancellationToken ct = default)
    {
        (var client, var ctx) = await ResolveClientWithDriveContextAsync(accessToken, ct);

        return await _uploadService.UploadAsync(client, ctx.DriveId, parentFolderId, localPath, remotePath, ct: ct);
    }

    private static async Task<DeltaResult> FullEnumerationAsync(GraphServiceClient client, string driveId, string folderId, string folderName, CancellationToken ct)
    {
        List<DeltaItem> items = [];
        await EnumerateSubFolderAsync(client, driveId, folderId, folderName, items, ct);
        var deltaPage = await GetDeltaLinkForNextSync(client, driveId, folderId, ct);

        string? deltaLink = deltaPage?.OdataDeltaLink;

        return new DeltaResult(items, deltaLink, false);
    }

    private static async Task<DeltaGetResponse?> GetDeltaLinkForNextSync(GraphServiceClient client, string driveId, string folderId, CancellationToken ct)
            => await client.Drives[driveId].Items[folderId].Delta.GetAsDeltaGetResponseAsync(cancellationToken: ct);

    private static async Task EnumerateSubFolderAsync(GraphServiceClient client, string driveId, string parentId, string relativePath, List<DeltaItem> items, CancellationToken ct)
    {
        var page = await client.Drives[driveId].Items[parentId].Children.GetAsync(cancellationToken: ct);

        while(page?.Value is not null)
        {
            foreach(var item in page.Value)
            {
                string itemPath = string.IsNullOrEmpty(relativePath)
                ? item.Name ?? string.Empty
                : $"{relativePath}/{item.Name}";

                items.Add(new DeltaItem(
                    Id: item.Id!,
                    DriveId: item.ParentReference?.DriveId ?? string.Empty,
                    Name: item.Name ?? string.Empty,
                    ParentId: item.ParentReference?.Id,
                    IsFolder: item.Folder is not null,
                    IsDeleted: false,
                    Size: item.Size ?? 0L,
                    LastModified: item.LastModifiedDateTime,
                    DownloadUrl: item.AdditionalData
                        .TryGetValue("@microsoft.graph.downloadUrl", out object? url)
                            ? url?.ToString()
                            : null,
                    RelativePath: itemPath));

                if(item.Folder is not null && item.Id is not null)
                {
                    await EnumerateSubFolderAsync(client, driveId, item.Id, itemPath, items, ct);
                }
            }

            if(page.OdataNextLink is null)
                break;

            page = await client.Drives[driveId].Items[parentId].Children
                .WithUrl(page.OdataNextLink)
                .GetAsync(cancellationToken: ct);
        }
    }

    private static DeltaItem MapToDeltaItem(DriveItem item)
    {
        string parentPath = item.ParentReference?.Path ?? string.Empty;
        string rootMarker = "root:";
        string afterRoot  = parentPath.Contains(rootMarker)
                            ? parentPath[(parentPath.IndexOf(rootMarker, StringComparison.CurrentCulture) + rootMarker.Length)..].TrimStart('/')
                            : string.Empty;

        string relativePath = string.IsNullOrEmpty(afterRoot)
                            ? item.Name ?? string.Empty
                            : $"{afterRoot}/{item.Name}";

        return new DeltaItem(
            Id: item.Id!,
            DriveId: item.ParentReference?.DriveId ?? string.Empty,
            Name: item.Name ?? string.Empty,
            ParentId: item.ParentReference?.Id,
            IsFolder: item.Folder is not null,
            IsDeleted: item.Deleted is not null,
            Size: item.Size ?? 0L,
            LastModified: item.LastModifiedDateTime,
            DownloadUrl: item.AdditionalData
                .TryGetValue("@microsoft.graph.downloadUrl", out object? url)
                    ? url?.ToString()
                    : null,
            RelativePath: relativePath);
    }

    private async Task<(GraphServiceClient Client, DriveContext Ctx)> ResolveClientWithDriveContextAsync(string accessToken, CancellationToken ct)
    {
        var graphServiceClient = BuildClient(accessToken);

        if(_cache.TryGetValue(accessToken, out var cached))
            return (graphServiceClient, cached);

        var drive = await graphServiceClient.Me.Drive.GetAsync(cancellationToken: ct);

        string driveId = drive?.Id ?? throw new InvalidOperationException("Could not retrieve drive ID.");

        var root = await graphServiceClient.Drives[driveId].Root.GetAsync(cancellationToken: ct);

        string rootId = root?.Id ?? throw new InvalidOperationException("Could not retrieve root item ID.");

        var driveContext = new DriveContext(driveId, rootId);
        _cache[accessToken] = driveContext;

        return (graphServiceClient, driveContext);
    }

    private static GraphServiceClient BuildClient(string accessToken)
        => new(new BaseBearerTokenAuthenticationProvider(new StaticAccessTokenProvider(accessToken)));

    private sealed record DriveContext(string DriveId, string RootId);

    private sealed class StaticAccessTokenProvider(string token) : IAccessTokenProvider
    {
        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken ct = default) => Task.FromResult(token);

        public AllowedHostsValidator AllowedHostsValidator { get; } = new(["graph.microsoft.com"]);
    }

    public void Dispose() => _uploadService.Dispose();
}
