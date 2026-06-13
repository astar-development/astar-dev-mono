using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

/// <summary>Recursively enumerates all descendants (files and sub-folders) of a given OneDrive folder using the Microsoft Graph API.</summary>
internal sealed class GraphFolderEnumerator(IGraphClientFactory graphClientFactory)
{
    private const string DownloadUrlKey = "@microsoft.graph.downloadUrl";

    private static readonly string[] childrenSelect =
    [
        "id", "name", "folder", "file", "size", "lastModifiedDateTime", "parentReference",
        "eTag", "cTag", DownloadUrlKey
    ];

    /// <summary>Enumerates all descendants of the specified folder, returning a flat list of <see cref="DeltaItem"/> instances. Cycles in the folder graph are detected and broken.</summary>
    internal async Task<Result<List<DeltaItem>, string>> EnumerateFolderAsync(Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, string folderId, string remotePath, Action<int>? onItemDiscovered, CancellationToken ct)
    {
        try
        {
            var client = graphClientFactory.CreateClient(tokenFactory);
            List<DeltaItem> items = [];
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await EnumerateSubFolderAsync(client, driveId, folderId, remotePath, items, visited, onItemDiscovered, ct).ConfigureAwait(false);

            return new Result<List<DeltaItem>, string>.Ok(items);
        }
        catch(Exception ex) when(ex is not OperationCanceledException and not SyncReAuthRequiredException)
        {
            return new Result<List<DeltaItem>, string>.Error(ex.Message);
        }
    }

    private static async Task EnumerateSubFolderAsync(GraphServiceClient client, DriveId driveId, string parentId, string relativePath, List<DeltaItem> items, HashSet<string> visited, Action<int>? onItemDiscovered, CancellationToken ct)
    {
        if(!visited.Add(parentId))
            return;

        var page = await client.Drives[driveId.Value].Items[parentId].Children
            .GetAsync(req => req.QueryParameters.Select = childrenSelect, ct).ConfigureAwait(false);

        while(page?.Value is not null)
        {
            foreach(var item in page.Value)
            {
                string itemPath = BuildRelativePath(relativePath, item);

                items.Add(MapToDeltaItem(item, itemPath));
                onItemDiscovered?.Invoke(items.Count);

                if(item.Folder is not null && item.Id is not null)
                    await EnumerateSubFolderAsync(client, driveId, item.Id, itemPath, items, visited, onItemDiscovered, ct).ConfigureAwait(false);
            }

            if(!OdataNextLinkGuard.IsSafe(page.OdataNextLink))
                break;

            page = await client.Drives[driveId.Value].Items[parentId].Children
                .WithUrl(page.OdataNextLink!)
                .GetAsync(cancellationToken: ct).ConfigureAwait(false);
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

        if(item.Folder is not null)
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
}
