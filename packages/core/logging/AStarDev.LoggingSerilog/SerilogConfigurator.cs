using AStarDev.LoggingSerilog.LogViewer;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Globalization;

namespace AStarDev.LoggingSerilog;

/// <summary>
/// This class provides a method to create a Serilog logger using the provided configuration and log file path. It allows for customization of the rolling interval and the number of retained log files.
/// </summary>
public static class SerilogConfigurator
{
    /// <summary>
    ///  This method creates a Serilog logger using the provided configuration and log file path. It allows for customization of the rolling interval and the number of retained log files.
    /// </summary>
    /// <param name="configuration">The configuration providing Serilog sink settings.</param>
    /// <param name="logFilePath">The path to the log file.</param>
    /// <param name="rollingInterval">The rolling interval for the log file.</param>
    /// <param name="retainedFileCountLimit">The number of retained log files.</param>
    /// <returns>The configured Serilog logger.</returns>
    public static ILogger CreateLogger(IConfigurationRoot configuration, string logFilePath, RollingInterval rollingInterval = RollingInterval.Day, int retainedFileCountLimit = 7)
        => new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
                .WriteTo.File(
                    formatter: new Serilog.Formatting.Json.JsonFormatter(),
                    path: logFilePath,
                    rollingInterval: rollingInterval,
                    retainedFileCountLimit: retainedFileCountLimit,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1)
                )
                .CreateLogger();

    /// <summary>
    ///  This method creates a Serilog logger using the provided configuration and log file path, additionally writing log events to the supplied <see cref="InMemoryLogSink"/>. It allows for customization of the rolling interval and the number of retained log files.
    /// </summary>
    /// <param name="configuration">The configuration providing Serilog sink settings.</param>
    /// <param name="logFilePath">The path to the log file.</param>
    /// <param name="inMemoryLogSink">The in-memory sink to additionally write log events to.</param>
    /// <param name="rollingInterval">The rolling interval for the log file.</param>
    /// <param name="retainedFileCountLimit">The number of retained log files.</param>
    /// <returns>The configured Serilog logger.</returns>
    public static ILogger CreateLogger(IConfigurationRoot configuration, string logFilePath, InMemoryLogSink inMemoryLogSink, RollingInterval rollingInterval = RollingInterval.Day, int retainedFileCountLimit = 7)
        => new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
                .WriteTo.File(
                    formatter: new Serilog.Formatting.Json.JsonFormatter(),
                    path: logFilePath,
                    rollingInterval: rollingInterval,
                    retainedFileCountLimit: retainedFileCountLimit,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1)
                )
                .WriteTo.Sink(inMemoryLogSink)
                .CreateLogger();
}
