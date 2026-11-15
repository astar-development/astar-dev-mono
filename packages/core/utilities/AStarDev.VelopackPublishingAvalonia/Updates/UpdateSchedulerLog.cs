using Microsoft.Extensions.Logging;

namespace AStar.Dev.Velopack.Publishing.Avalonia.Updates;

internal static partial class UpdateSchedulerLog
{
    [LoggerMessage(EventId = 6410, Level = LogLevel.Information, Message = "The update schedule was stopped.")]
    public static partial void Stopped(ILogger logger);
}
