using AStar.Dev.Utilities;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AStar.Dev.Logging.Extensions;

/// <summary>
///     The <see cref="LoggingExtensions" /> class contains, as you might expect, extension methods for configuring Serilog
///     / Application Insights.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    ///     The <see cref="AddSerilogLogging(Microsoft.AspNetCore.Builder.WebApplicationBuilder,string)" /> method will add Serilog to the logging providers.
    /// </summary>
    /// <param name="builder">
    /// </param>
    /// <param name="externalSettingsFile">
    ///     The name (including extension) of the file containing the Serilog Configuration settings.
    /// </param>
    /// <returns>
    ///     The original instance of <see cref="WebApplicationBuilder" /> for further method chaining.
    /// </returns>
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder, string externalSettingsFile = "")
    {
        if(externalSettingsFile.IsNotNullOrWhiteSpace())
        {
            _ = builder.Configuration.AddJsonFile(externalSettingsFile, true, true);
        }

        _ = builder.Services.AddScoped(typeof(ILoggerAstar<>), typeof(AStarLogger<>));
        _ = builder.Services.AddApplicationInsightsTelemetry(builder.Configuration);
        var serviceProvider = builder.Services.BuildServiceProvider();

        var logger = new LoggerConfiguration().Configure(builder.Configuration, serviceProvider.GetRequiredService<TelemetryConfiguration>()).CreateLogger();

        logger.Debug("Serilog has been configured.");

        Log.Logger = logger;

        _ = builder.Host
                   .UseSerilog((_, loggerConfig) => loggerConfig.Configure(builder.Configuration, serviceProvider.GetRequiredService<TelemetryConfiguration>()));

        return builder;
    }

    /// <summary>
    ///     The <see cref="AddSerilogLogging(Microsoft.Extensions.Hosting.HostApplicationBuilder,string)" /> method will add Serilog to the logging providers.
    /// </summary>
    /// <param name="builder">
    /// </param>
    /// <param name="externalSettingsFile">
    ///     The name (including extension) of the file containing the Serilog Configuration settings.
    /// </param>
    /// <returns>
    ///     The original instance of <see cref="HostApplicationBuilder" /> for further method chaining.
    /// </returns>
    public static HostApplicationBuilder AddSerilogLogging(this HostApplicationBuilder builder,
                                                           string                      externalSettingsFile = "")
    {
        if(externalSettingsFile.IsNotNullOrWhiteSpace())
        {
            _ = builder.Configuration.AddJsonFile(externalSettingsFile, true, true);
        }

        // Register a default TelemetryConfiguration if not present, for test/integration scenarios.
        _ = builder.Services.AddSingleton<TelemetryConfiguration>(_ => new());
        var serviceProvider = builder.Services.BuildServiceProvider();

        var logger = new LoggerConfiguration()
                     .Configure(builder.Configuration, serviceProvider.GetRequiredService<TelemetryConfiguration>())
                     .CreateLogger();

        logger.Debug("Serilog has been configured.");

        Log.Logger = logger;

        _ = builder.Services
                   .AddSerilog((context, loggerConfig) => loggerConfig.Configure(builder.Configuration, serviceProvider.GetRequiredService<TelemetryConfiguration>()));

        return builder;
    }
}