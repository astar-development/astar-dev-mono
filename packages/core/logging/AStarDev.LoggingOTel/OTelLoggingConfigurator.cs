using AStarDev.LoggingOTel.LogViewer;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

namespace AStarDev.LoggingOTel;

/// <summary>
///     This class provides extension methods to configure OpenTelemetry logging, exporting to the console and,
///     when a connection string is configured, to Azure Monitor (Application Insights).
/// </summary>
public static class OTelLoggingConfigurator
{
    /// <summary>
    ///     The <see cref="ConfigureOTelLogging(ILoggingBuilder,IConfigurationRoot,string)" /> method wires up OpenTelemetry
    ///     logging: console output plus an Azure Monitor log exporter when a connection string is configured.
    /// </summary>
    /// <param name="loggingBuilder">The logging builder to configure.</param>
    /// <param name="configuration">The configuration providing the Azure Monitor connection string.</param>
    /// <param name="connectionStringConfigurationKey">The configuration key holding the Azure Monitor connection string.</param>
    /// <returns>The original instance of <see cref="ILoggingBuilder" /> for further method chaining.</returns>
    public static ILoggingBuilder ConfigureOTelLogging(this ILoggingBuilder loggingBuilder, IConfigurationRoot configuration, string connectionStringConfigurationKey = "ApplicationInsights:ConnectionString")
        => loggingBuilder.AddOpenTelemetry(options => Configure(options, configuration, connectionStringConfigurationKey));

    /// <summary>
    ///     The <see cref="ConfigureOTelLogging(ILoggingBuilder,IConfigurationRoot,InMemoryLogProcessor,string)" /> method wires up
    ///     OpenTelemetry logging the same way as <see cref="ConfigureOTelLogging(ILoggingBuilder,IConfigurationRoot,string)" />,
    ///     additionally routing log records to the supplied <see cref="InMemoryLogProcessor"/>.
    /// </summary>
    /// <param name="loggingBuilder">The logging builder to configure.</param>
    /// <param name="configuration">The configuration providing the Azure Monitor connection string.</param>
    /// <param name="inMemoryLogProcessor">The in-memory processor to additionally route log records to.</param>
    /// <param name="connectionStringConfigurationKey">The configuration key holding the Azure Monitor connection string.</param>
    /// <returns>The original instance of <see cref="ILoggingBuilder" /> for further method chaining.</returns>
    public static ILoggingBuilder ConfigureOTelLogging(this ILoggingBuilder loggingBuilder, IConfigurationRoot configuration, InMemoryLogProcessor inMemoryLogProcessor, string connectionStringConfigurationKey = "ApplicationInsights:ConnectionString")
        => loggingBuilder.AddOpenTelemetry(options =>
        {
            Configure(options, configuration, connectionStringConfigurationKey);
            options.AddProcessor(inMemoryLogProcessor);
        });

    private static void Configure(OpenTelemetryLoggerOptions options, IConfigurationRoot configuration, string connectionStringConfigurationKey)
    {
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
        options.ParseStateValues = true;
        options.AddConsoleExporter();

        string? connectionString = configuration[connectionStringConfigurationKey];

        if (!string.IsNullOrWhiteSpace(connectionString))
            options.AddAzureMonitorLogExporter(exporterOptions => exporterOptions.ConnectionString = connectionString);
    }
}
