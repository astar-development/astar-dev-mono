using System.Globalization;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

/// <summary>Parses the header text shown on the top-wallpapers page into a total page count.</summary>
public static class TopWallpapersHeaderParser
{
    /// <summary>Parses <paramref name="headerText" />, frozen from the historical <c>TopWallpapersPage.PageInfoAsync</c> parsing quirks.</summary>
    public static Result<int, ScrapeError> Parse(string? headerText)
    {
        if (string.IsNullOrEmpty(headerText))
            return ScrapeErrorFactory.CreatePageParseFailed(headerText, "The top wallpapers page header text was missing.");

        try
        {
            var firstSlashIndex = headerText.IndexOf('/') + 1;
            var pages = headerText[firstSlashIndex..].Trim();

            return int.Parse(pages, CultureInfo.InvariantCulture);
        }
        catch (Exception exception)
        {
            return ScrapeErrorFactory.CreatePageParseFailed(headerText, exception.Message);
        }
    }
}
