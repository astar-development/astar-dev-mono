namespace AStarDev.WallpaperScraper.WallpaperIngestion;

/// <summary>Whether everything attempted was dealt with, or something failed and must be attempted again on a later scrape.</summary>
public enum IngestOutcome
{
    /// <summary>Everything was ingested, already existed or was deliberately ignored.</summary>
    Complete,

    /// <summary>At least one wallpaper failed (its tags, download, recording or linking), so it should be retried on a later scrape.</summary>
    Incomplete
}
