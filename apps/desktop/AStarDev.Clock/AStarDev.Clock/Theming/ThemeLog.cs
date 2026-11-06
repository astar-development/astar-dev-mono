using Microsoft.Extensions.Logging;

namespace AStarDev.Clock.Theming;

internal static partial class ThemeLog
{
    [LoggerMessage(EventId = 6000, Level = LogLevel.Warning, Message = "Could not parse the persisted theme settings ({ErrorType}); using the system theme.")]
    public static partial void ThemeSettingsUnreadable(ILogger logger, string errorType);
}
