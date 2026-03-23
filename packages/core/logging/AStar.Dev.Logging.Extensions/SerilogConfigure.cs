using System.Globalization;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace AStar.Dev.Logging.Extensions;

/// <summary>
///     Provides an extension method for configuring Serilog logging with Application Insights and console output.
/// </summary>
internal static class SerilogConfigure
{
    /// <summary>
    ///     Configures the Serilog logger using the specified settings and telemetry configuration.
    /// </summary>
    /// <param name="loggerConfiguration">The <see cref="LoggerConfiguration" /> instance to be configured.</param>
    /// <param name="configuration">The application configuration settings to read from.</param>
    /// <param name="telemetryConfiguration">The <see cref="TelemetryConfiguration" /> instance used for Application Insights integration.</param>
    /// <returns>The configured instance of <see cref="LoggerConfiguration" />.</returns>
    public static LoggerConfiguration Configure(this LoggerConfiguration loggerConfiguration, IConfiguration configuration, TelemetryConfiguration telemetryConfiguration)
        => loggerConfiguration
           .WriteTo.ApplicationInsights(telemetryConfiguration,
                                        TelemetryConverter.Traces).WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {Message:lj}{NewLine}{Exception}",
                                                                                   formatProvider: new CultureInfo(0809))
           .ReadFrom.Configuration(configuration);
}