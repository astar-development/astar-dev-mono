using AStarDev.WallpaperScraper.Scraping.WallhavenResponses.SearchResponse;

namespace AStarDev.WallpaperScraper.WallpaperIngestion;

/// <summary>A wallpaper from a search page together with the file extension it would be saved under.</summary>
/// <param name="Wallpaper">The wallpaper data.</param>
/// <param name="Extension">The file extension to save the image under.</param>
public sealed record WallpaperCandidate(Data Wallpaper, string Extension);
