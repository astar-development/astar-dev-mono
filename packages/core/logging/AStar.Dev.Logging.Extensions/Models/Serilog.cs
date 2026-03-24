namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     Represents the configuration settings for the Serilog logging framework.
///     This class is used to define the settings such as log enrichment, log output destinations, and log level configurations.
/// </summary>
public sealed class Serilog
{
    /// <summary>
    ///     A collection of enrichers used to add additional contextual data to log events.
    ///     Each element in the array represents the name of an enricher to be applied during logging.
    /// </summary>
    public string[] Enrich { get; set; } = [];

    /// <summary>
    ///     A collection of targets where log events are written during logging.
    ///     Each element in the array specifies the name of the sink and its associated arguments.
    /// </summary>
    public WriteTo[] WriteTo { get; set; } = new WriteTo[] { new() { Args = new() { ServerUrl = "http://seq:5341" }, Name = "Seq" } };

    /// <summary>
    ///     Configures the minimum logging levels for log events.
    ///     This property determines the default logging level and allows overrides for specific components or namespaces.
    /// </summary>
    public MinimumLevel MinimumLevel { get; set; } = new();
}