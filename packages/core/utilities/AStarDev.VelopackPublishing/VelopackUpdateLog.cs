using Microsoft.Extensions.Logging;

namespace AStar.Dev.Velopack.Publishing;

internal static partial class VelopackUpdateLog
{
    [LoggerMessage(EventId = 6400, Level = LogLevel.Information, Message = "Update check completed. Current version: {CurrentVersion}; available update: {TargetVersion}.")]
    public static partial void UpdateCheckCompleted(ILogger logger, object? currentVersion, object? targetVersion);
}
