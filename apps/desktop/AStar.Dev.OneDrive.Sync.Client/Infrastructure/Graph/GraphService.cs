using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

public sealed class GraphService(IUploadService uploadService) : IGraphService
{
    private const string RootPathMarker = "root:";
    private const string DownloadUrlKey  = "@microsoft.graph.downloadUrl";

    private static readonly string[] ChildrenSelect =
    [
        "id", "name", "folder", "file", "size", "lastModifiedDateTime", "parentReference",
        DownloadUrlKey
    ];

    private static readonly string[] FolderSelect = ["id", "name", "folder", "parentReference"];

    private readonly Dictionary<string, DriveContext> _cache = [];

    /// <inheritdoc />
    public async Task<string> GetDriveIdAsync(string accessToken, CancellationToken ct = default)
        => (await ResolveClientWithDriveContextAsync(accessToken, ct)).Ctx.DriveId;

    /// <inheritdoc />
    public async Task<List<DriveFolder>> GetRootFoldersAsync(string accessToken, CancellationToken ct = default)
    {
        (var client, var driveContext) = await ResolveClientWithDriveContextAsync(accessToken, ct);

        var driveItemCollectionResponse = await client.Drives[driveContext.DriveId].Items[driveContext.RootId].Children
            .GetAsync(req => req.QueryParameters.Select = ChildrenSelect, ct);

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
                req.QueryParameters.Select = FolderSelect;
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
    public async Task<List<DriveFolder>> GetAllFoldersAsync(string accessToken, CancellationToken ct = default)
    {
        (var client, var ctx) = await ResolveClientWithDriveContextAsync(accessToken, ct);

        List<DriveFolder> all = [];
        await CollectAllFoldersRecursiveAsync(client, ctx.DriveId, ctx.RootId, all, ct);

        return all;
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
    public async Task<DeltaResult> GetDeltaAsync(string accessToken, string folderId, string? deltaLink, IReadOnlySet<string> excludedFolderIds, CancellationToken ct = default)
    {
        (var client, var ctx) = await ResolveClientWithDriveContextAsync(accessToken, ct);

        List<DeltaItem> items = [];
        string? nextDeltaLink = null;
        bool hasMorePages      = false;

        var page = deltaLink is null
            ? await client.Drives[ctx.DriveId].Items[folderId].Delta
                .GetAsDeltaGetResponseAsync(cancellationToken: ct)
            : await client.Drives[ctx.DriveId].Items[folderId].Delta
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

        return new DeltaResult(DeltaItemExclusionFilter.Filter(items, excludedFolderIds), nextDeltaLink, hasMorePages);
    }

    /// <inheritdoc />
    public async Task<string?> GetDownloadUrlAsync(string accessToken, string itemId, CancellationToken ct = default)
    {
        (var client, var ctx) = await ResolveClientWithDriveContextAsync(accessToken, ct);

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
    public async Task<string> UploadFileAsync(string accessToken, string localPath, string remotePath, string parentFolderId, CancellationToken ct = default)
    {
        (var client, var ctx) = await ResolveClientWithDriveContextAsync(accessToken, ct);

        return await uploadService.UploadAsync(client, ctx.DriveId, parentFolderId, localPath, remotePath, ct: ct);
    }

    private static async Task CollectAllFoldersRecursiveAsync(GraphServiceClient client, string driveId, string parentId, List<DriveFolder> accumulator, CancellationToken ct)
    {
        var page = await client.Drives[driveId].Items[parentId].Children
            .GetAsync(req =>
            {
                req.QueryParameters.Select = FolderSelect;
                req.QueryParameters.Top = 100;
            }, ct);

        while(page?.Value is not null)
        {
            foreach(var item in page.Value.Where(i => i.Folder is not null))
            {
                accumulator.Add(new DriveFolder(item.Id!, item.Name!, item.ParentReference?.Id));
                await CollectAllFoldersRecursiveAsync(client, driveId, item.Id!, accumulator, ct);
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
        string afterRoot  = parentPath.Contains(RootPathMarker)
                            ? parentPath[(parentPath.IndexOf(RootPathMarker, StringComparison.Ordinal) + RootPathMarker.Length)..].TrimStart('/')
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
            DownloadUrl: ExtractDownloadUrl(item),
            RelativePath: relativePath);
    }

    private static string? ExtractDownloadUrl(DriveItem item)
        => item.AdditionalData?.TryGetValue(DownloadUrlKey, out var url) is true
            ? url?.ToString()
            : null;

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
}
