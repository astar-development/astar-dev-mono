namespace AStar.Dev.Wallpaper.Scraper.Pages;

/// <summary>Selects the wanted image links from a page's raw anchor <c>href</c> attributes.</summary>
public static class ImageLinkSelector
{
    /// <summary>Selects the non-null <paramref name="hrefs" /> that point at an image, up to <see cref="ScraperConstants.ImagesPerPage" />.</summary>
    public static IReadOnlyCollection<string> SelectWanted(IEnumerable<string?> hrefs)
        => [.. hrefs.Where(href => href is not null && href.Contains("/w/", StringComparison.Ordinal))
                    .Select(href => href!)
                    .Take(ScraperConstants.ImagesPerPage)];
}
