namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     Represents the configuration for Serilog logging within the application.
///     This class provides properties to define the setup for logging,
///     including Serilog-related configuration and general logging options.
/// </summary>
public sealed class SerilogConfig
{
    /// <summary>
    ///     Represents the configuration for Serilog logging.
    /// </summary>
    public Serilog Serilog { get; set; } = new();

    /// <summary>
    ///     Represents the configuration for standard logging in the application.
    ///     This class provides properties to define logging behavior, including settings
    ///     for console output and Application Insights integration.
    /// </summary>
    public Logging Logging { get; set; } = new();
}