namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Factory for <see cref="TagData"/> — the validation boundary for raw, scraped tag text and category
///     read from a wallpaper detail page.
/// </summary>
public static class TagDataFactory
{
    /// <summary>
    ///     Creates a <see cref="TagData"/> from a scraped tag's raw text and category attribute, trimming
    ///     surrounding whitespace from the tag and normalizing a blank category to <see langword="null"/>.
    /// </summary>
    /// <param name="tag">The raw tag text scraped from the wallpaper detail page.</param>
    /// <param name="category">The raw category attribute scraped from the wallpaper detail page, if present.</param>
    public static TagData Create(string tag, string? category)
    {
        string normalizedTag = tag.Trim();
        string? normalizedCategory = string.IsNullOrWhiteSpace(category) ? null : category.Trim();

        return new TagData(normalizedTag, normalizedCategory);
    }
}
