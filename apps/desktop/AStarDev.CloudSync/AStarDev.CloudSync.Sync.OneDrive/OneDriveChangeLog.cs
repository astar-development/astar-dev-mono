using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.OneDrive;

internal static partial class OneDriveChangeLog
{
    [LoggerMessage(EventId = 5410, Level = LogLevel.Information, Message = "Read a change page of {ItemCount} items, caught up: {CaughtUp}.")]
    public static partial void PageRead(ILogger logger, int itemCount, bool caughtUp);

    [LoggerMessage(EventId = 5411, Level = LogLevel.Warning, Message = "Reading the change feed failed: HTTP {StatusCode}, Graph error {ErrorCode} ({ExceptionType}).")]
    public static partial void ReadFailed(ILogger logger, int statusCode, string errorCode, string exceptionType);

    [LoggerMessage(EventId = 5412, Level = LogLevel.Warning, Message = "A change link was refused because it is not HTTPS on the Graph host, or the page had no continuation.")]
    public static partial void LinkRefused(ILogger logger);

    [LoggerMessage(EventId = 5413, Level = LogLevel.Information, Message = "Reading the change feed was cancelled.")]
    public static partial void ReadCancelled(ILogger logger);

    [LoggerMessage(EventId = 5414, Level = LogLevel.Information, Message = "No access token for the change feed: {Outcome}.")]
    public static partial void TokenUnavailable(ILogger logger, string outcome);

    [LoggerMessage(EventId = 5415, Level = LogLevel.Warning, Message = "The change cursor expired (HTTP 410), Graph error {ErrorCode}: a resync is needed.")]
    public static partial void CursorExpired(ILogger logger, string errorCode);
}
