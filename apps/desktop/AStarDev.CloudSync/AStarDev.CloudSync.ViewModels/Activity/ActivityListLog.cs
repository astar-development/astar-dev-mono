using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.ViewModels.Activity;

internal static partial class ActivityListLog
{
    [LoggerMessage(EventId = 5330, Level = LogLevel.Warning, Message = "Could not read the activity list: the activity log reported a failure.")]
    public static partial void ReadFailed(ILogger logger);
}
