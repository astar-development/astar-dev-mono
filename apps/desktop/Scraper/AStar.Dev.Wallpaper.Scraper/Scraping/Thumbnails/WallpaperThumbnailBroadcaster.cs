using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace AStar.Dev.Wallpaper.Scraper.Scraping.Thumbnails;

/// <summary>
///     Broadcasts wallpaper thumbnails from wherever they are generated during a scrape to any interested subscribers,
///     such as the main window's preview panel.
/// </summary>
public sealed class WallpaperThumbnailBroadcaster : IWallpaperThumbnailPublisher, IWallpaperThumbnailFeed, IDisposable
{
    private readonly Subject<WallpaperThumbnailPayload> thumbnails = new();
    private readonly Subject<string> categorySkipped = new();
    private bool disposed;

    /// <inheritdoc />
    public IObservable<WallpaperThumbnailPayload> Thumbnails => thumbnails.AsObservable();

    /// <inheritdoc />
    public IObservable<string> CategorySkipped => categorySkipped.AsObservable();

    /// <inheritdoc />
    public void Publish(WallpaperThumbnailPayload payload) => thumbnails.OnNext(payload);

    /// <inheritdoc />
    public void PublishCategorySkipped(string categoryName) => categorySkipped.OnNext(categoryName);

    /// <summary>
    ///     Completes and disposes the underlying subjects.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (!disposing)
            return;

        thumbnails.Dispose();
        categorySkipped.Dispose();
    }
}
