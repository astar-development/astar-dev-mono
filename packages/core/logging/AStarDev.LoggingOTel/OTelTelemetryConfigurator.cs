using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Reflection;

namespace AStarDev.LoggingOTel;

/// <summary>Wires up OpenTelemetry metrics and traces for an application that is not hosted by the generic host.</summary>
public static class OTelTelemetryConfigurator
{
    /// <summary>
    ///     Starts exporting the named meter and activity source. Metrics and traces go to the OTLP endpoint in
    ///     <see cref="OTelLoggingConfigurator.OtlpEndpointConfigurationKey" /> when it is configured, and to Azure Monitor when the connection string is configured.
    ///     With neither configured nothing is exported, but the providers still run.
    /// </summary>
    /// <param name="configuration">The configuration providing the endpoint and the connection string.</param>
    /// <param name="serviceName">The <c>service.name</c> reported on every metric and span; when blank, the entry assembly name is used.</param>
    /// <param name="meterName">The name of the <see cref="System.Diagnostics.Metrics.Meter" /> to export.</param>
    /// <param name="activitySourceName">The name of the <see cref="System.Diagnostics.ActivitySource" /> to export.</param>
    /// <param name="connectionStringConfigurationKey">The configuration key holding the Azure Monitor connection string.</param>
    /// <returns>The running providers. Dispose it to flush and stop exporting.</returns>
    public static TelemetryProviders ConfigureOTelTelemetry(IConfigurationRoot configuration, string serviceName, string meterName, string activitySourceName, string connectionStringConfigurationKey = "ApplicationInsights:ConnectionString")
    {
        var resource = OTelLoggingConfigurator.CreateResourceBuilder(configuration, serviceName, Assembly.GetEntryAssembly(), Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"));
        string? connectionString = configuration[connectionStringConfigurationKey];
        bool hasConnectionString = !string.IsNullOrWhiteSpace(connectionString);
        bool hasEndpoint = Uri.TryCreate(configuration[OTelLoggingConfigurator.OtlpEndpointConfigurationKey], UriKind.Absolute, out Uri? endpoint) && (endpoint.Scheme == Uri.UriSchemeHttp || endpoint.Scheme == Uri.UriSchemeHttps);

        var metrics = Sdk.CreateMeterProviderBuilder().SetResourceBuilder(resource).AddMeter(meterName);
        var traces = Sdk.CreateTracerProviderBuilder().SetResourceBuilder(resource).AddSource(activitySourceName);

        if (hasConnectionString)
        {
            _ = metrics.AddAzureMonitorMetricExporter(options => options.ConnectionString = connectionString);
            _ = traces.AddAzureMonitorTraceExporter(options => options.ConnectionString = connectionString);
        }

        if (hasEndpoint)
        {
            _ = metrics.AddOtlpExporter(options => options.Endpoint = endpoint!);
            _ = traces.AddOtlpExporter(options => options.Endpoint = endpoint!);
        }

        return new TelemetryProviders(metrics.Build(), traces.Build());
    }
}
