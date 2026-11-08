using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.DisplaySettings;

internal static partial class DisplayPreferencesLog
{
    [LoggerMessage(EventId = 5200, Level = LogLevel.Warning, Message = "Could not read the display preferences ({ErrorType}); using the defaults.")]
    public static partial void PreferencesUnreadable(ILogger logger, string errorType);

    [LoggerMessage(EventId = 5201, Level = LogLevel.Warning, Message = "Could not save the display preferences ({ErrorType}).")]
    public static partial void PreferencesNotSaved(ILogger logger, string errorType);
}
