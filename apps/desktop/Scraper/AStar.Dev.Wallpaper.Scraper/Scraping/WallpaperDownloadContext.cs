namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Carries the per-wallpaper values resolved while visiting a wallpaper page that the download stage needs.
/// </summary>
/// <param name="ImageUrl">The URL of the full-size wallpaper image.</param>
/// <param name="DirectoryPath">The directory the wallpaper should be saved into.</param>
/// <param name="Tags">The curated tags to record against the downloaded wallpaper.</param>
public sealed record WallpaperDownloadContext(string ImageUrl, string DirectoryPath, IReadOnlyList<TagData> Tags);
