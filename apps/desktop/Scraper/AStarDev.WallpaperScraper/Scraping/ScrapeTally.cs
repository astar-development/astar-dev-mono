using AStarDev.WallpaperScraper.WallpaperIngestion;

namespace AStarDev.WallpaperScraper.Scraping;

/// <summary>Counts the wallpapers one search has got through (downloaded or skipped) against the total the search matches.</summary>
public sealed class ScrapeTally
{
    /// <summary>Gets or sets the total number of wallpapers the search matches, as reported by the latest page fetched.</summary>
    public int Total { get; set; }

    /// <summary>Gets or sets the number of the page being ingested.</summary>
    public int CurrentPage { get; set; }

    /// <summary>Gets or sets the total number of pages the search has, as reported by the latest page fetched.</summary>
    public int TotalPages { get; set; }

    /// <summary>Gets the number of wallpapers the search has got through so far.</summary>
    public int Current { get; private set; }

    /// <summary>Counts wallpapers that were skipped, such as ones already held or on pages resumed past.</summary>
    /// <param name="skipped">The number of wallpapers skipped.</param>
    public void RecordSkipped(int skipped) => Current += skipped;

    /// <summary>Counts one more downloaded wallpaper.</summary>
    /// <returns>The count including the wallpaper just downloaded, never more than the total once that is known.</returns>
    public SearchCount RecordDownload()
    {
        Current++;

        return new(Total > 0 ? Math.Min(Current, Total) : Current, Total, CurrentPage, TotalPages);
    }
}
