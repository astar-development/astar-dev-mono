namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Carries a newly generated wallpaper thumbnail together with the category and tags it was downloaded under.
/// </summary>
/// <param name="Bytes">The PNG-encoded thumbnail bytes.</param>
/// <param name="CategoryName">The name of the search category the wallpaper was downloaded under.</param>
/// <param name="Tags">The tags kept for the wallpaper after curation.</param>
public sealed record WallpaperThumbnailPayload(byte[] Bytes, string CategoryName, IReadOnlyList<string> Tags);
