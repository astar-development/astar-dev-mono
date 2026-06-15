using System.Runtime.CompilerServices;
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

    /// <summary>Streams all descendants of the specified folder, yielding each item as it arrives from the Graph API. Cycles in the folder graph are detected and broken.</summary>
    internal async IAsyncEnumerable<DeltaItem> EnumerateFolderAsync(Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, string folderId, string remotePath, Action<int>? onItemDiscovered, [EnumeratorCancellation] CancellationToken ct)
    {
        var client = graphClientFactory.CreateClient(tokenFactory);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int count = 0;

        await foreach (var item in EnumerateSubFolderAsync(client, driveId, folderId, remotePath, visited, ct))
        {
            count++;
            onItemDiscovered?.Invoke(count);
            yield return item;
        }
    }

    private static async IAsyncEnumerable<DeltaItem> EnumerateSubFolderAsync(GraphServiceClient client, DriveId driveId, string parentId, string relativePath, HashSet<string> visited, [EnumeratorCancellation] CancellationToken ct)
    {
        if(!visited.Add(parentId))
            yield break;

        var page = await client.Drives[driveId.Value].Items[parentId].Children
            .GetAsync(req => req.QueryParameters.Select = childrenSelect, ct).ConfigureAwait(false);

        while(page?.Value is not null)
        {
            foreach(var item in page.Value)
            {
                string itemPath = BuildRelativePath(relativePath, item);
                yield return MapToDeltaItem(item, itemPath);

                if(item.Folder is not null && item.Id is not null)
                    await foreach(var child in EnumerateSubFolderAsync(client, driveId, item.Id, itemPath, visited, ct))
                        yield return child;
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
