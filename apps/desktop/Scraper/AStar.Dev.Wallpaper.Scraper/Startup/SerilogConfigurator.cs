using AStar.Dev.Wallpaper.Scraper.Configuration;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Abstractions;

namespace AStar.Dev.Wallpaper.Scraper.Startup;

/// <summary>Builds the application's Serilog pipeline, ensuring the logs directory exists before sinks attach to it.</summary>
[ExcludeFromCodeCoverage]
public static class SerilogConfigurator
{
    /// <summary>Creates the configured Serilog logger, creating <see cref="ApplicationDirectories.LogsDirectory" /> if required.</summary>
    /// <param name="fileSystem">The file system abstraction used to create the logs directory.</param>
    /// <param name="configuration">The configuration providing Serilog sink settings.</param>
    public static ILogger CreateLogger(IFileSystem fileSystem, IConfigurationRoot configuration)
    {
        _ = fileSystem.Directory.CreateDirectory(ApplicationDirectories.LogsDirectory);

        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .ReadFrom.Configuration(configuration)
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(
                formatter: new Serilog.Formatting.Json.JsonFormatter(),
                path: $"{ApplicationDirectories.LogsDirectory}/log.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();
    }
}
