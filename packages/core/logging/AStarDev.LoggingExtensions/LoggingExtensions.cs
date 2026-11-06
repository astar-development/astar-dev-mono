using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AStarDev.Utilities;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;

namespace AStarDev.LoggingExtensions;

/// <summary>
///     The <see cref="LoggingExtensions" /> class contains extension methods for configuring OpenTelemetry logging,
///     tracing, and metrics, exported to Azure Monitor / Application Insights.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    ///     The <see cref="AddOTelLogging(WebApplicationBuilder,string,string)" /> method will add OpenTelemetry logging,
    ///     tracing, and metrics, exported to Azure Monitor, to the logging providers.
    /// </summary>
    /// <param name="builder">The instance of <see cref="WebApplicationBuilder" /> to which OpenTelemetry logging will be added.</param>
    /// <param name="serviceName">The <c>service.name</c> on the OpenTelemetry resource (the cloud role name in Azure Monitor); when blank, the host application name is used.</param>
    /// <param name="externalSettingsFile">The name (including extension) of an optional file containing additional configuration settings to layer onto the application configuration.</param>
    /// <returns>The original instance of <see cref="WebApplicationBuilder" /> for further method chaining.</returns>
    public static WebApplicationBuilder AddOTelLogging(this WebApplicationBuilder builder, string serviceName, string externalSettingsFile = "")
    {
        if (externalSettingsFile.IsNotNullOrWhiteSpace()) _ = builder.Configuration.AddJsonFile(externalSettingsFile, true, true);

        _ = builder.Services.AddScoped(typeof(ILoggerAstar<>), typeof(AStarLogger<>));
        ConfigureAzureMonitor(builder.Services, builder.Configuration, builder.Environment, serviceName);

        return builder;
    }

    /// <summary>
    ///     The <see cref="AddOTelLogging(HostApplicationBuilder,string,string)" /> method will add OpenTelemetry logging,
    ///     tracing, and metrics, exported to Azure Monitor, to the logging providers.
    /// </summary>
    /// <param name="builder">The instance of <see cref="HostApplicationBuilder" /> to which OpenTelemetry logging will be added.</param>
    /// <param name="serviceName">The <c>service.name</c> on the OpenTelemetry resource (the cloud role name in Azure Monitor); when blank, the host application name is used.</param>
    /// <param name="externalSettingsFile">The name (including extension) of an optional file containing additional configuration settings to layer onto the application configuration.</param>
    /// <returns>The original instance of <see cref="HostApplicationBuilder" /> for further method chaining.</returns>
    public static HostApplicationBuilder AddOTelLogging(this HostApplicationBuilder builder, string serviceName, string externalSettingsFile = "")
    {
        if (externalSettingsFile.IsNotNullOrWhiteSpace()) _ = builder.Configuration.AddJsonFile(externalSettingsFile, true, true);

        _ = builder.Services.AddScoped(typeof(ILoggerAstar<>), typeof(AStarLogger<>));
        ConfigureAzureMonitor(builder.Services, builder.Configuration, builder.Environment, serviceName);

        return builder;
    }

    private static void ConfigureAzureMonitor(IServiceCollection services, ConfigurationManager configuration, IHostEnvironment environment, string serviceName)
    {
        string? connectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] ?? configuration["ApplicationInsights:ConnectionString"];

        if (connectionString.IsNotNullOrWhiteSpace())
            _ = services.AddOpenTelemetry().ConfigureResource(resource => ConfigureResource(resource, environment, serviceName, Assembly.GetEntryAssembly())).UseAzureMonitor(options => options.ConnectionString = connectionString);
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "OpenTelemetry deployment.environment.name values are lower case by convention.")]
    internal static void ConfigureResource(ResourceBuilder resource, IHostEnvironment environment, string serviceName, Assembly? entryAssembly)
    {
        string resolvedServiceName = serviceName.IsNotNullOrWhiteSpace() ? serviceName : environment.ApplicationName;
        string? serviceVersion = entryAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0];

        _ = resource.AddService(resolvedServiceName, serviceVersion: serviceVersion)
            .AddAttributes([new KeyValuePair<string, object>("deployment.environment.name", environment.EnvironmentName.ToLowerInvariant())]);
    }
}
