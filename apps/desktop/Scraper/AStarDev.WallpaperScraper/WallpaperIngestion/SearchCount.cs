namespace AStarDev.WallpaperScraper.WallpaperIngestion;

/// <summary>How far through a search the ingestion is.</summary>
/// <param name="Current">The number of wallpapers of the search downloaded so far.</param>
/// <param name="Total">The total number of wallpapers the search matches, or 0 when that is not known.</param>
public readonly record struct SearchCount(int Current, int Total);
