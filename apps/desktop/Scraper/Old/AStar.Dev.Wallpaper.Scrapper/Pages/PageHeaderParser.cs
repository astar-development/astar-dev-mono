using System.Globalization;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

/// <summary>Parses the header text shown on a search-results page into a <see cref="PageInfo" />.</summary>
public static class PageHeaderParser
{
    /// <summary>Parses <paramref name="headerText" />, frozen from the historical <c>SearchResultsPage.GetPageInfoAsync</c> parsing quirks.</summary>
    public static Result<PageInfo, ScrapeError> Parse(string? headerText)
    {
        if (string.IsNullOrEmpty(headerText))
            return ScrapeErrorFactory.CreatePageParseFailed(headerText, "The search results page header text was missing.");

        try
        {
            var firstSpaceIndex = headerText.IndexOf(' ');
            var hashIndex = headerText.IndexOf("for ", StringComparison.Ordinal) + 3;
            var subDirectoryName = string.Empty;

            if (hashIndex > 0) subDirectoryName = headerText[(hashIndex + 1)..].Replace(" ", "-").Replace("#", string.Empty);

            var searchResults = headerText.Replace(",", string.Empty)[..firstSpaceIndex];
            var imageCount = decimal.Parse(searchResults, CultureInfo.InvariantCulture);
            var pageCount = Convert.ToInt32(Math.Ceiling(imageCount / ScrapperConstants.ImagesPerPage));

            return PageInfoFactory.Create(pageCount, (int)imageCount, subDirectoryName);
        }
        catch (Exception exception)
        {
            return ScrapeErrorFactory.CreatePageParseFailed(headerText, exception.Message);
        }
    }
}
