using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.OneDrive;

internal static partial class OneDriveQuotaLog
{
    private const int MaxErrorCodeLength = 64;

    public static string Capped(string errorCode) => errorCode.Length > MaxErrorCodeLength ? errorCode[..MaxErrorCodeLength] : errorCode;

    [LoggerMessage(EventId = 5440, Level = LogLevel.Warning, Message = "Reading the drive quota failed: HTTP {StatusCode}, Graph error {ErrorCode} ({ExceptionType}).")]
    public static partial void ReadFailed(ILogger logger, int statusCode, string errorCode, string exceptionType);

    [LoggerMessage(EventId = 5441, Level = LogLevel.Information, Message = "No access token for the drive quota read: {Outcome}.")]
    public static partial void TokenUnavailable(ILogger logger, string outcome);

    [LoggerMessage(EventId = 5442, Level = LogLevel.Warning, Message = "Graph returned a drive without a quota.")]
    public static partial void QuotaMissing(ILogger logger);
}
