using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.ViewModels.Overview;

internal static partial class SyncOverviewLog
{
    [LoggerMessage(EventId = 5300, Level = LogLevel.Error, Message = "Could not load the sync overview ({ErrorType}).")]
    public static partial void OverviewLoadThrew(ILogger logger, string errorType);

    [LoggerMessage(EventId = 5301, Level = LogLevel.Error, Message = "Could not load the sync overview: the source reported a failure.")]
    public static partial void OverviewSourceReportedFailure(ILogger logger);
}
