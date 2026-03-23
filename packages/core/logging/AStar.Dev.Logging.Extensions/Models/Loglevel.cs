namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     Represents logging levels configuration for various components in the application.
/// </summary>
public sealed class LogLevel
{
    /// <summary>
    ///     Gets or sets the default logging level for the application.
    /// </summary>
    public string Default { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the logging level configuration specific to Microsoft.AspNetCore components.
    /// </summary>
    public string MicrosoftAspNetCore { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the logging level configuration specific to the AStar component.
    /// </summary>
    public string AStar { get; set; } = string.Empty;
}