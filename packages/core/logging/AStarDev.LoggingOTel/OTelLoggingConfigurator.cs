using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AStarDev.LoggingOTel.LogViewer;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace AStarDev.LoggingOTel;

/// <summary>
///     This class provides extension methods to configure OpenTelemetry logging, exporting to the console and,
///     when a connection string is configured, to Azure Monitor (Application Insights) and, when an endpoint is configured,
///     to an OTLP (gRPC) collector such as the Aspire dashboard.
/// </summary>
public static class OTelLoggingConfigurator
{
    /// <summary>
    ///     The configuration key holding the OTLP (gRPC) endpoint, e.g. <c>http://localhost:4317</c>. When it is missing or not an
    ///     absolute <c>http</c>/<c>https</c> URI, no OTLP exporter is added.
    /// </summary>
    public const string OtlpEndpointConfigurationKey = "OpenTelemetry:OtlpEndpoint";

    /// <summary>
    ///     The configuration key holding the <c>deployment.environment.name</c>, e.g. <c>development</c>, <c>staging</c>, <c>production</c> or <c>test</c>.
    ///     When it is missing, the <c>DOTNET_ENVIRONMENT</c> environment variable is used, then <c>production</c>. The value is lower-cased.
    /// </summary>
    public const string DeploymentEnvironmentConfigurationKey = "OpenTelemetry:DeploymentEnvironment";

    /// <summary>
    ///     The <see cref="ConfigureOTelLogging(ILoggingBuilder,IConfigurationRoot,string,string)" /> method wires up OpenTelemetry
    ///     logging: console output plus an Azure Monitor log exporter when a connection string is configured.
    /// </summary>
    /// <param name="loggingBuilder">The logging builder to configure.</param>
    /// <param name="configuration">The configuration providing the Azure Monitor connection string.</param>
    /// <param name="serviceName">The <c>service.name</c> reported on every log record (the cloud role name in Azure Monitor); when blank, the entry assembly name is used.</param>
    /// <param name="connectionStringConfigurationKey">The configuration key holding the Azure Monitor connection string.</param>
    /// <returns>The original instance of <see cref="ILoggingBuilder" /> for further method chaining.</returns>
    public static ILoggingBuilder ConfigureOTelLogging(this ILoggingBuilder loggingBuilder, IConfigurationRoot configuration, string serviceName, string connectionStringConfigurationKey = "ApplicationInsights:ConnectionString")
        => loggingBuilder.AddOpenTelemetry(options => Configure(options, configuration, connectionStringConfigurationKey, serviceName));

    /// <summary>
    ///     The <see cref="ConfigureOTelLogging(ILoggingBuilder,IConfigurationRoot,InMemoryLogProcessor,string,string)" /> method wires up
    ///     OpenTelemetry logging the same way as <see cref="ConfigureOTelLogging(ILoggingBuilder,IConfigurationRoot,string,string)" />,
    ///     additionally routing log records to the supplied <see cref="InMemoryLogProcessor"/>.
    /// </summary>
    /// <param name="loggingBuilder">The logging builder to configure.</param>
    /// <param name="configuration">The configuration providing the Azure Monitor connection string.</param>
    /// <param name="inMemoryLogProcessor">The in-memory processor to additionally route log records to.</param>
    /// <param name="serviceName">The <c>service.name</c> reported on every log record (the cloud role name in Azure Monitor); when blank, the entry assembly name is used.</param>
    /// <param name="connectionStringConfigurationKey">The configuration key holding the Azure Monitor connection string.</param>
    /// <returns>The original instance of <see cref="ILoggingBuilder" /> for further method chaining.</returns>
    public static ILoggingBuilder ConfigureOTelLogging(this ILoggingBuilder loggingBuilder, IConfigurationRoot configuration, InMemoryLogProcessor inMemoryLogProcessor, string serviceName, string connectionStringConfigurationKey = "ApplicationInsights:ConnectionString")
        => loggingBuilder.AddOpenTelemetry(options =>
        {
            Configure(options, configuration, connectionStringConfigurationKey, serviceName);
            _ = options.AddProcessor(inMemoryLogProcessor);
        });

    private static void Configure(OpenTelemetryLoggerOptions options, IConfigurationRoot configuration, string connectionStringConfigurationKey, string serviceName)
    {
        _ = options.SetResourceBuilder(CreateResourceBuilder(configuration, serviceName));

        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
        options.ParseStateValues = true;
        _ = options.AddConsoleExporter();

        string? connectionString = configuration[connectionStringConfigurationKey];

        if (!string.IsNullOrWhiteSpace(connectionString))
            _ = options.AddAzureMonitorLogExporter(exporterOptions => exporterOptions.ConnectionString = connectionString);

        if (Uri.TryCreate(configuration[OtlpEndpointConfigurationKey], UriKind.Absolute, out Uri? otlpEndpoint) && (otlpEndpoint.Scheme == Uri.UriSchemeHttp || otlpEndpoint.Scheme == Uri.UriSchemeHttps))
            _ = options.AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = otlpEndpoint);
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "OpenTelemetry deployment.environment.name values are lower case by convention.")]
    private static ResourceBuilder CreateResourceBuilder(IConfigurationRoot configuration, string serviceName)
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        string resolvedServiceName = string.IsNullOrWhiteSpace(serviceName) ? entryAssembly?.GetName().Name ?? "unknown_service" : serviceName;
        string? serviceVersion = entryAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0];
        string deploymentEnvironment = (configuration[DeploymentEnvironmentConfigurationKey] ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "production").ToLowerInvariant();

        return ResourceBuilder.CreateDefault()
            .AddService(resolvedServiceName, serviceVersion: serviceVersion)
            .AddAttributes([new KeyValuePair<string, object>("deployment.environment.name", deploymentEnvironment)]);
    }
}
