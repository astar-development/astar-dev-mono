namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     Represents the configuration settings for logging within the system.
/// </summary>
/// <remarks>
///     This class provides properties for configuring console logging and Application Insights logging.
/// </remarks>
public sealed class Logging
{
    /// <summary>
    ///     Represents the configuration settings for console-based logging within the application.
    /// </summary>
    public Console Console { get; set; } = new();

    /// <summary>
    ///     Represents the configuration settings for Application Insights logging functionality.
    /// </summary>
    public ApplicationInsights ApplicationInsights { get; set; } = new();
}