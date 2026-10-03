using AStarDev.ControlDb.FileDetail;

namespace AStarDev.WallpaperScraper.WallpaperIngestion;

/// <summary>A wallpaper that was downloaded and recorded, with the details listeners are told once it is fully ingested.</summary>
/// <param name="Entity">The recorded file entity.</param>
/// <param name="Details">The saved path and wallpaper details to announce.</param>
public sealed record SavedWallpaper(FileEntity Entity, WallpaperDownloadDetails Details);
