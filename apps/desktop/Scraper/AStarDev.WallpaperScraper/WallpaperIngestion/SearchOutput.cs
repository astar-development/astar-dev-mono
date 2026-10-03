namespace AStarDev.WallpaperScraper.WallpaperIngestion;

/// <summary>Where and under what label the wallpapers of one search are saved.</summary>
/// <param name="Directories">The directories to save wallpaper images into.</param>
/// <param name="CategoryLabel">The search category being ingested, or "Top Wallpapers" when it is not a specific search category.</param>
public sealed record SearchOutput(SaveDirectories Directories, string CategoryLabel);
