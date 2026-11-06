using Microsoft.Extensions.Logging;

namespace AStarDev.WallpaperScraper.Scraping;

internal static partial class ScrapingLog
{
    [LoggerMessage(EventId = 6100, Level = LogLevel.Error, Message = "The {SearchLabel} search failed ({ErrorType}).")]
    public static partial void SearchFailed(ILogger logger, string searchLabel, string errorType);
}
