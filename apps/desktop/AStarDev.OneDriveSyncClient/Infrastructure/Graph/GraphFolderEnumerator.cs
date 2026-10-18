using System.Diagnostics;
using System.Runtime.CompilerServices;
using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Graph;

/// <summary>Recursively enumerates all descendants (files and sub-folders) of a given OneDrive folder using the Microsoft Graph API.</summary>
internal sealed class GraphFolderEnumerator(IGraphClientFactory graphClientFactory, ILogger<GraphFolderEnumerator> logger)
{
    private const int ProgressPageInterval = 50;
    private static readonly TimeSpan slowPageThreshold = TimeSpan.FromSeconds(5);

    private const string DownloadUrlKey = "@microsoft.graph.downloadUrl";

    private static readonly string[] childrenSelect =
    [
        "id", "name", "folder", "file", "size", "lastModifiedDateTime", "parentReference",
        "eTag", "cTag", DownloadUrlKey
    ];

    /// <summary>Streams all descendants of the specified folder, yielding each item as it arrives from the Graph API. Cycles in the folder graph are detected and broken.</summary>
    internal async IAsyncEnumerable<DeltaItem> EnumerateFolderAsync(Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, string folderId, string remotePath, Action<int>? onItemDiscovered, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var client = graphClientFactory.CreateClient(tokenFactory);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var progress = new WalkProgress(remotePath);
        int count = 0;

        await foreach (var item in EnumerateSubFolderAsync(client, driveId, folderId, remotePath, visited, progress, cancellationToken))
        {
            count++;
            onItemDiscovered?.Invoke(count);
            yield return item;
        }

        OneDriveSyncClientMessages.GraphFolderEnumeratorCompleted(logger, remotePath, count, progress.Pages, progress.ElapsedMs);
    }

    private async IAsyncEnumerable<DeltaItem> EnumerateSubFolderAsync(GraphServiceClient client, DriveId driveId, string parentId, string relativePath, HashSet<string> visited, WalkProgress progress, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!visited.Add(parentId))
            yield break;

        var firstPage = await TimePageAsync(progress, relativePath, () => client.Drives[driveId.Value].Items[parentId].Children.GetAsync(req => req.QueryParameters.Select = childrenSelect, cancellationToken)).ConfigureAwait(false);

        await foreach (var item in EnumeratePageItemsAsync(firstPage, nextLink => TimePageAsync(progress, relativePath, () => client.Drives[driveId.Value].Items[parentId].Children.WithUrl(nextLink).GetAsync(cancellationToken: cancellationToken)), cancellationToken))
        {
            string itemPath = BuildRelativePath(relativePath, item);
            yield return MapToDeltaItem(item, itemPath);

            if (item.Folder is not null && item.Id is not null)
                await foreach (var child in EnumerateSubFolderAsync(client, driveId, item.Id, itemPath, visited, progress, cancellationToken))
                    yield return child;
        }
    }

    private static async IAsyncEnumerable<DriveItem> EnumeratePageItemsAsync(DriveItemCollectionResponse? firstPage, Func<string, Task<DriveItemCollectionResponse?>> fetchNext, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var page = firstPage;
        while (page?.Value is not null)
        {
            foreach (var item in page.Value)
                yield return item;

            if (!OdataNextLinkGuard.IsSafe(page.OdataNextLink))
                yield break;

            cancellationToken.ThrowIfCancellationRequested();
            page = await fetchNext(page.OdataNextLink!).ConfigureAwait(false);
        }
    }

    private async Task<DriveItemCollectionResponse?> TimePageAsync(WalkProgress progress, string folderPath, Func<Task<DriveItemCollectionResponse?>> fetchPage)
    {
        long started = Stopwatch.GetTimestamp();
        var page = await fetchPage().ConfigureAwait(false);
        var elapsed = Stopwatch.GetElapsedTime(started);
        progress.Pages++;

        if (elapsed >= slowPageThreshold)
            OneDriveSyncClientMessages.GraphFolderEnumeratorSlowPage(logger, folderPath, (long)elapsed.TotalMilliseconds);

        if (progress.Pages % ProgressPageInterval == 0)
            OneDriveSyncClientMessages.GraphFolderEnumeratorProgress(logger, progress.RootPath, progress.Pages, progress.ElapsedMs, folderPath);

        return page;
    }

    private static string BuildRelativePath(string relativePath, DriveItem item) => string.IsNullOrEmpty(relativePath)
        ? item.Name ?? string.Empty
        : $"{relativePath}/{item.Name}";

    private static DeltaItem MapToDeltaItem(DriveItem item, string itemPath)
    {
        var id = new OneDriveItemId(item.Id);
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
        if (item.AdditionalData?.TryGetValue(DownloadUrlKey, out object? url) is not true || url is null)
            return Option.None<string>();

        return Option.Some(url.ToString()!);
    }

    private static Option<string> ToOptionString(string? value) => value is not null ? Option.Some(value) : Option.None<string>();

    private sealed class WalkProgress(string rootPath)
    {
        private readonly long startTimestamp = Stopwatch.GetTimestamp();

        public string RootPath { get; } = rootPath;

        public long ElapsedMs => (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

        public int Pages { get; set; }
    }
}
