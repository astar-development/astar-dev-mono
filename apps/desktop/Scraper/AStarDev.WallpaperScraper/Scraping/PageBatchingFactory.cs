namespace AStarDev.WallpaperScraper.Scraping;

/// <summary>Creates the cases of <see cref="PageBatching"/>.</summary>
public static class PageBatchingFactory
{
    /// <summary>A search limited to a batch of pages per run.</summary>
    public static PageBatching CreateBatched() => new BatchedPaging();

    /// <summary>A search not limited to a batch of pages per run.</summary>
    public static PageBatching CreateUnbatched() => new UnbatchedPaging();
}
