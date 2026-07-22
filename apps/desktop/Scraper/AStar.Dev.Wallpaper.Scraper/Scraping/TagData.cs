namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Represents a tag and its associated category, used for classification purposes.
/// </summary>
/// <param name="Tag">The name of the tag.</param>
/// <param name="Category">The category to which the tag belongs. Nullable because not every scraped tag has a category attribute.</param>
public record TagData(string Tag, string? Category)
{
    /// <summary>
    ///     Gets a value indicating whether the tag is considered "famous" based on its content or category. A tag is considered famous if it contains certain keywords related to famous people.
    /// </summary>
    public bool IsFamous => IsPeopleTag(Tag) || IsPeopleTag(Category ?? string.Empty);

    /// <summary>
    ///     Gets a value indicating whether the tag is related to the internet based on its content or category. A tag is considered internet-related if it contains certain keywords related to people.
    /// </summary>
    public bool IsInternet => TagContains(Category ?? string.Empty, "people > model");

    private static bool IsPeopleTag(string tagValue)
        => TagContains(tagValue, "people > porn")
           || TagContains(tagValue, "people > actress")
           || TagContains(tagValue, "people > celebrity")
           || TagContains(tagValue, "people > actor")
           || TagContains(tagValue, "people > singer");

    private static bool TagContains(string category, string contains)
        => category.Contains(contains, StringComparison.OrdinalIgnoreCase);
}
