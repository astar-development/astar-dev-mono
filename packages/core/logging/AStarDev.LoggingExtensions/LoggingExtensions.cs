using AStarDev.Utilities;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AStar.Dev.Logging.Extensions;

/// <summary>
///     The <see cref="LoggingExtensions" /> class contains extension methods for configuring OpenTelemetry logging,
///     tracing, and metrics, exported to Azure Monitor / Application Insights.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    ///     The <see cref="AddOTelLogging(WebApplicationBuilder,string)" /> method will add OpenTelemetry logging,
    ///     tracing, and metrics, exported to Azure Monitor, to the logging providers.
    /// </summary>
    /// <param name="builder">
    /// </param>
    /// <param name="externalSettingsFile">
    ///     The name (including extension) of an optional file containing additional configuration settings to layer
    ///     onto the application configuration.
    /// </param>
    /// <returns>
    ///     The original instance of <see cref="WebApplicationBuilder" /> for further method chaining.
    /// </returns>
    public static WebApplicationBuilder AddOTelLogging(this WebApplicationBuilder builder, string externalSettingsFile = "")
    {
        if (externalSettingsFile.IsNotNullOrWhiteSpace()) _ = builder.Configuration.AddJsonFile(externalSettingsFile, true, true);

        _ = builder.Services.AddScoped(typeof(ILoggerAstar<>), typeof(AStarLogger<>));
        _ = builder.Services.AddOpenTelemetry().UseAzureMonitor();

        return builder;
    }

    /// <summary>
    ///     The <see cref="AddOTelLogging(HostApplicationBuilder,string)" /> method will add OpenTelemetry logging,
    ///     tracing, and metrics, exported to Azure Monitor, to the logging providers.
    /// </summary>
    /// <param name="builder">
    /// </param>
    /// <param name="externalSettingsFile">
    ///     The name (including extension) of an optional file containing additional configuration settings to layer
    ///     onto the application configuration.
    /// </param>
    /// <returns>
    ///     The original instance of <see cref="HostApplicationBuilder" /> for further method chaining.
    /// </returns>
    public static HostApplicationBuilder AddOTelLogging(this HostApplicationBuilder builder, string externalSettingsFile = "")
    {
        if (externalSettingsFile.IsNotNullOrWhiteSpace()) _ = builder.Configuration.AddJsonFile(externalSettingsFile, true, true);

        _ = builder.Services.AddScoped(typeof(ILoggerAstar<>), typeof(AStarLogger<>));
        _ = builder.Services.AddOpenTelemetry().UseAzureMonitor();

        return builder;
    }
}
