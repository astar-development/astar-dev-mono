using Microsoft.Extensions.Logging;

namespace AStar.Dev.Velopack.Publishing;

internal static partial class VelopackUpdateLog
{
    [LoggerMessage(EventId = 6400, Level = LogLevel.Information, Message = "Update check completed. Current version: {CurrentVersion}; available update: {TargetVersion}.")]
    public static partial void UpdateCheckCompleted(ILogger logger, object? currentVersion, object? targetVersion);

    [LoggerMessage(EventId = 6401, Level = LogLevel.Warning, Message = "Could not read the update deferral ({ErrorType}); treating the update as not deferred.")]
    public static partial void DeferralUnreadable(ILogger logger, string errorType);

    [LoggerMessage(EventId = 6402, Level = LogLevel.Warning, Message = "Could not save the update deferral ({ErrorType}).")]
    public static partial void DeferralNotSaved(ILogger logger, string errorType);
}
