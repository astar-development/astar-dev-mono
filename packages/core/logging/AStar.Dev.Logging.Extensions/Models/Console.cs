namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     Represents a console logging configuration for formatting output in a logging system.
/// </summary>
public sealed class Console
{
    /// <summary>
    ///     Gets or sets the name of the formatter used for console logging output.
    /// </summary>
    public string FormatterName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the options for configuring the console log formatter.
    /// </summary>
    public FormatterOptions FormatterOptions { get; set; } = new();
}