namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     The <see cref="ApplicationInsights" /> class which is used to configure the logging.
/// </summary>
public sealed class ApplicationInsights
{
    /// <summary>
    ///     The Log level to use.
    /// </summary>
    public LogLevel LogLevel { get; set; } = new();
}