using Microsoft.Graph;
using Serilog;

namespace AnotherOneDriveSync.Core;

public class GraphService : IGraphService
{
    private readonly IGraphClientFactory _clientFactory;
    private readonly ILogger _logger;

    public GraphService(IGraphClientFactory clientFactory, ILogger logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async IAsyncEnumerable<DriveItem> ListDriveRootChildrenAsync()
    {
        var client = await _clientFactory.CreateAsync();
        var page = await client.Me.Drive.Root.Children.Request().GetAsync();
        while (page != null)
        {
            foreach (var item in page)
                yield return item;

            page = page.NextPageRequest != null
                ? await page.NextPageRequest.GetAsync()
                : null;
        }
    }

    public async IAsyncEnumerable<DriveItem> ListFolderChildrenAsync(string folderId)
    {
        var client = await _clientFactory.CreateAsync();
        var page = await client.Me.Drive.Items[folderId].Children.Request().GetAsync();
        var pageNumber = 0;
        while (page != null)
        {
            pageNumber++;
            var pageItems = page.CurrentPage;
            _logger.Debug("Folder {FolderId} page {Page}: {Count} item(s), hasNextPage={HasNext}",
                folderId, pageNumber, pageItems.Count, page.NextPageRequest != null);
            foreach (var item in pageItems)
            {
                _logger.Debug("  → {Name} ({Id}) folder={IsFolder} file={IsFile}",
                    item.Name, item.Id, item.Folder != null, item.File != null);
                yield return item;
            }
            page = page.NextPageRequest != null
                ? await page.NextPageRequest.GetAsync()
                : null;
        }
        _logger.Debug("Folder {FolderId} listing complete ({Pages} page(s))", folderId, pageNumber);
    }

    public async Task<DriveItem> GetDriveItemMetadataAsync(string itemId)
    {
        var client = await _clientFactory.CreateAsync();
        return await client.Me.Drive.Items[itemId].Request().GetAsync();
    }

    public async Task<Stream> DownloadItemContentAsync(string itemId)
    {
        var client = await _clientFactory.CreateAsync();
        // Graph SDK v4 RedirectHandler calls InnerHandler without ResponseHeadersRead,
        // so the CDN response is fully buffered. ReadAsStreamAsync returns a MemoryStream
        // but some SDK paths leave the position at Length. Always seek to 0 before copying.
        using var raw = await client.Me.Drive.Items[itemId].Content.Request().GetAsync();
        var buffer = new MemoryStream();
        if (raw != null)
        {
            if (raw.CanSeek && raw.Position != 0)
                raw.Position = 0;
            await raw.CopyToAsync(buffer);
        }
        buffer.Position = 0;
        if (buffer.Length == 0)
            _logger.Warning("DownloadItemContentAsync: empty buffer for item {ItemId} — SDK may not have followed redirect", itemId);
        return buffer;
    }
}
