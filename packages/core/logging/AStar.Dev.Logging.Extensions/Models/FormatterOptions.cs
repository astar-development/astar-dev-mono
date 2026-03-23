namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     Represents the configuration options for formatting log output.
/// </summary>
public sealed class FormatterOptions
{
    /// <summary>
    ///     Indicates whether the log output should be formatted as a single line.
    /// </summary>
    public bool SingleLine { get; set; }

    /// <summary>
    ///     Determines whether scope information should be included in the log output.
    /// </summary>
    public bool IncludeScopes { get; set; }

    /// <summary>
    ///     Specifies the format for timestamps in log entries.
    /// </summary>
    public string TimestampFormat { get; set; } = "HH:mm:ss ";

    /// <summary>
    ///     Determines whether to use the UTC time zone for timestamps in the log output.
    /// </summary>
    public bool UseUtcTimestamp { get; set; } = true;

    /// <summary>
    ///     Represents configurable options for customizing the behavior of JSON writing
    ///     within the logging formatter.
    /// </summary>
    public JsonWriterOptions JsonWriterOptions { get; set; } = new();
}