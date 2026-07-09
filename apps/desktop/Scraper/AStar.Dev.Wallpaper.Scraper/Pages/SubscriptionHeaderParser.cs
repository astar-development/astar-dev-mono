using System.Globalization;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Models;

namespace AStar.Dev.Wallpaper.Scraper.Pages;

/// <summary>Parses the header text shown on a subscriptions page into a <see cref="PageInfo" />.</summary>
public static class SubscriptionHeaderParser
{
    /// <summary>Parses <paramref name="headerText" />, frozen from the historical <c>SubscriptionsImagesListPage.PageInfoAsync</c> parsing quirks.</summary>
    public static Result<PageInfo, ScrapeError> Parse(string? headerText)
    {
        if (string.IsNullOrEmpty(headerText))
            return ScrapeErrorFactory.CreatePageParseFailed(headerText, "The subscriptions page header text was missing.");

        try
        {
            int firstSpaceIndex = headerText.IndexOf(' ');
            int hashIndex = headerText.IndexOf("New", StringComparison.Ordinal);
            string subDirectoryName = string.Empty;

            if (hashIndex > 0) subDirectoryName = headerText[hashIndex..].Replace(" ", "-").Replace("#", string.Empty);

            string searchResults = headerText.Replace(",", string.Empty)[..firstSpaceIndex];
            decimal imageCount = decimal.Parse(searchResults, CultureInfo.InvariantCulture);
            int pageCount = Convert.ToInt32(Math.Ceiling(imageCount / ScraperConstants.ImagesPerPage));

            return PageInfoFactory.Create(pageCount, (int)imageCount, subDirectoryName);
        }
        catch (Exception exception)
        {
            return ScrapeErrorFactory.CreatePageParseFailed(headerText, exception.Message);
        }
    }
}
