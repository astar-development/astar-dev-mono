using Microsoft.Extensions.Logging;

namespace AStarDev.WallpaperScraper.Startup;

internal static partial class StartupLog
{
    [LoggerMessage(EventId = 6110, Level = LogLevel.Critical, Message = "The startup database migration failed ({ErrorType}); the scraper cannot use its database.")]
    public static partial void DatabaseMigrationFailed(ILogger logger, string errorType);
}
