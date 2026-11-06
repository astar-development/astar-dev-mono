using Microsoft.Extensions.Logging;

namespace AStar.Dev.Velopack.Publishing;

internal static partial class VelopackUpdateLog
{
    [LoggerMessage(EventId = 6400, Level = LogLevel.Information, Message = "Up to date. Current version: {CurrentVersion}.")]
    public static partial void UpToDate(ILogger logger, object? currentVersion);

    [LoggerMessage(EventId = 6401, Level = LogLevel.Information, Message = "Update available: {CurrentVersion} -> {TargetVersion}.")]
    public static partial void UpdateAvailable(ILogger logger, object? currentVersion, object targetVersion);
}
