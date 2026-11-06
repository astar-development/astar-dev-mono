using Microsoft.Extensions.Logging;

namespace Fab4Kids.Web.Configuration;

internal static partial class ConfigurationLog
{
    [LoggerMessage(EventId = 6500, Message = "Required configuration is missing for {Feature}; that feature is unavailable.")]
    public static partial void RequiredConfigurationMissing(ILogger logger, LogLevel level, string feature);

    [LoggerMessage(EventId = 6501, Level = LogLevel.Debug, Message = "{Feature} was requested but is not configured.")]
    public static partial void FeatureNotConfigured(ILogger logger, string feature);
}
