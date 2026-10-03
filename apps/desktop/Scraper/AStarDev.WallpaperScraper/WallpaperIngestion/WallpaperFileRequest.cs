using AStarDev.ControlDb.FileDetail;
using AStarDev.WallpaperScraper.Scraping.WallhavenResponses.SearchResponse;

namespace AStarDev.WallpaperScraper.WallpaperIngestion;

/// <summary>A wallpaper together with the file name and directory its image file should be downloaded to and its file record processed against.</summary>
/// <param name="Wallpaper">The wallpaper data being downloaded/processed.</param>
/// <param name="Directory">The directory to save the wallpaper's image into.</param>
/// <param name="FileName">The file name to save the image and record its file entity under.</param>
/// <param name="CategoryLabel">The search category the wallpaper came from, or "Top Wallpapers" when it did not come from a specific search category.</param>
public sealed record WallpaperFileRequest(Data Wallpaper, string Directory, FileName FileName, string CategoryLabel);
