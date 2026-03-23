namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     Represents the configuration target for writing logs in a Serilog configuration.
/// </summary>
public sealed class WriteTo
{
    /// <summary>
    ///     Gets or sets the name of the logging sink to which log events are written.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the arguments for configuring the logging sink.
    /// </summary>
    public Args Args { get; set; } = new();
}