using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.OneDrive;

internal static partial class OneDriveBrowseLog
{
    [LoggerMessage(EventId = 5400, Level = LogLevel.Information, Message = "Listed {FolderCount} remote folders in {PageCount} pages.")]
    public static partial void ListingCompleted(ILogger logger, int folderCount, int pageCount);

    [LoggerMessage(EventId = 5401, Level = LogLevel.Warning, Message = "Listing remote folders failed: HTTP {StatusCode}, Graph error {ErrorCode}, {PageCount} pages read ({ExceptionType}).")]
    public static partial void ListingFailed(ILogger logger, int statusCode, string errorCode, int pageCount, string exceptionType);

    [LoggerMessage(EventId = 5402, Level = LogLevel.Warning, Message = "A next link was refused because it is not HTTPS on the Graph host or was seen before, so paging stopped after {PageCount} pages.")]
    public static partial void NextLinkRefused(ILogger logger, int pageCount);

    [LoggerMessage(EventId = 5403, Level = LogLevel.Information, Message = "The remote folder listing was cancelled.")]
    public static partial void ListingCancelled(ILogger logger);

    [LoggerMessage(EventId = 5404, Level = LogLevel.Information, Message = "No access token for the remote folder listing: {Outcome}.")]
    public static partial void TokenUnavailable(ILogger logger, string outcome);
}
