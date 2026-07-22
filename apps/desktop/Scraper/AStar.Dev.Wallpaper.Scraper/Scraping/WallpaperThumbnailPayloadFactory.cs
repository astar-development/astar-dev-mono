namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Factory for <see cref="WallpaperThumbnailPayload"/> — the validation boundary for a thumbnail published
///     alongside the category and tags it was downloaded under.
/// </summary>
public static class WallpaperThumbnailPayloadFactory
{
    /// <summary>
    ///     Creates a <see cref="WallpaperThumbnailPayload"/>, normalizing a blank category name to
    ///     "Uncategorized" and a missing tags list to an empty list rather than throwing.
    /// </summary>
    /// <param name="bytes">The PNG-encoded thumbnail bytes.</param>
    /// <param name="categoryName">The name of the search category the wallpaper was downloaded under, if known.</param>
    /// <param name="tags">The tags kept for the wallpaper after curation, if any.</param>
    public static WallpaperThumbnailPayload Create(byte[] bytes, string? categoryName, IReadOnlyList<string>? tags)
    {
        string normalizedCategoryName = string.IsNullOrWhiteSpace(categoryName) ? "Uncategorized" : categoryName.Trim();
        var normalizedTags = tags ?? [];

        return new WallpaperThumbnailPayload(bytes, normalizedCategoryName, normalizedTags);
    }
}
