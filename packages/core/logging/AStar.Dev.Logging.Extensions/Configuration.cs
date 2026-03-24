namespace AStar.Dev.Logging.Extensions;

/// <summary>
///     Provides configuration settings for the logging extensions.
/// </summary>
public static class Configuration
{
    /// <summary>
    ///     Gets the default filename for the external logging settings configuration file.
    /// </summary>
    /// <remarks>
    ///     The value returned by <c>ExternalSettingsFile</c> is a constant string representing
    ///     the filename used to store external logging configuration settings. This property
    ///     ensures consistent reference to the expected configuration file across the application.
    /// </remarks>
    public static string ExternalSettingsFile => "astar-logging-settings.json";
}