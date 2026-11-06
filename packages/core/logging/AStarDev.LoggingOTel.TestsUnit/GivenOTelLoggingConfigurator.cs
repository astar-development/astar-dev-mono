using System.Net;
using System.Reflection;
using System.Net.Sockets;
using AStarDev.LoggingOTel.LogViewer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace AStarDev.LoggingOTel.TestsUnit;

public sealed class GivenOTelLoggingConfigurator
{
    private const string TestServiceName = "the-test-service";

    private static IConfigurationRoot CreateConfiguration() =>
        new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();

    [Fact]
    public void when_configure_logging_is_called_then_a_usable_logger_is_produced()
    {
        var configuration = CreateConfiguration();
        var services = new ServiceCollection();
        _ = services.AddLogging(builder => builder.ConfigureOTelLogging(configuration, TestServiceName));
        using var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<GivenOTelLoggingConfigurator>>();

        logger.ShouldNotBeNull();
    }

    [Fact]
    public void when_configure_logging_is_called_then_log_messages_can_be_written_without_throwing()
    {
        var configuration = CreateConfiguration();
        var services = new ServiceCollection();
        _ = services.AddLogging(builder => builder.ConfigureOTelLogging(configuration, TestServiceName));
        using var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<GivenOTelLoggingConfigurator>>();

        Action act = () => logger.LogInformation("the expected message was logged");

        act.ShouldNotThrow();
    }

    [Fact]
    public void when_configure_logging_is_called_with_an_in_memory_log_processor_then_log_entries_reach_the_processor()
    {
        var configuration = CreateConfiguration();
        using var inMemoryLogProcessor = new InMemoryLogProcessor();
        var services = new ServiceCollection();
        _ = services.AddLogging(builder => builder.ConfigureOTelLogging(configuration, inMemoryLogProcessor, TestServiceName));
        using var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<GivenOTelLoggingConfigurator>>();

        logger.LogInformation("the expected message was logged to the in memory processor");

        var snapshot = inMemoryLogProcessor.GetSnapshot();

        snapshot.ShouldContain(entry => entry.RenderedMessage.Contains("the expected message was logged to the in memory processor"));
    }

    [Fact]
    public void when_a_service_name_is_supplied_then_log_records_carry_it_as_the_resource_service_name()
    {
        var configuration = CreateConfiguration();
        using var inMemoryLogProcessor = new InMemoryLogProcessor();
        var services = new ServiceCollection();
        _ = services.AddLogging(builder => builder.ConfigureOTelLogging(configuration, inMemoryLogProcessor, serviceName: "the-expected-service"));
        using var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<GivenOTelLoggingConfigurator>>();

        logger.LogInformation("a message");

        ResourceServiceName(serviceProvider).ShouldBe("the-expected-service");
    }

    [Fact]
    public void when_a_blank_service_name_is_supplied_then_the_entry_assembly_name_is_used_instead_of_unknown_service()
    {
        var configuration = CreateConfiguration();
        var services = new ServiceCollection();
        _ = services.AddLogging(builder => builder.ConfigureOTelLogging(configuration, " "));
        using var serviceProvider = services.BuildServiceProvider();

        ResourceAttribute(serviceProvider, "service.name").ShouldBe(Assembly.GetEntryAssembly()!.GetName().Name);
    }

    [Fact]
    public void when_logging_is_configured_then_the_resource_carries_the_entry_assembly_version_without_build_metadata()
    {
        var configuration = CreateConfiguration();
        var services = new ServiceCollection();
        _ = services.AddLogging(builder => builder.ConfigureOTelLogging(configuration, TestServiceName));
        using var serviceProvider = services.BuildServiceProvider();
        var expectedVersion = Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];

        ResourceAttribute(serviceProvider, "service.version").ShouldBe(expectedVersion);
    }

    [Fact]
    public void when_logging_is_configured_then_the_resource_carries_a_service_instance_id()
    {
        var configuration = CreateConfiguration();
        var services = new ServiceCollection();
        _ = services.AddLogging(builder => builder.ConfigureOTelLogging(configuration, TestServiceName));
        using var serviceProvider = services.BuildServiceProvider();

        ResourceAttribute(serviceProvider, "service.instance.id").ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void when_a_deployment_environment_is_configured_then_the_resource_carries_it_in_lower_case()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["OpenTelemetry:DeploymentEnvironment"] = "Staging" }).Build();
        var services = new ServiceCollection();
        _ = services.AddLogging(builder => builder.ConfigureOTelLogging(configuration, TestServiceName));
        using var serviceProvider = services.BuildServiceProvider();

        ResourceAttribute(serviceProvider, "deployment.environment.name").ShouldBe("staging");
    }

    [Fact]
    public async Task when_an_otlp_endpoint_is_configured_then_log_records_are_exported_to_it()
    {
        using var otlpListener = new TcpListener(IPAddress.Loopback, 0);
        otlpListener.Start();
        var otlpPort = ((IPEndPoint)otlpListener.LocalEndpoint).Port;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["OpenTelemetry:OtlpEndpoint"] = $"http://127.0.0.1:{otlpPort}" }).Build();
        var services = new ServiceCollection();
        _ = services.AddLogging(builder => builder.ConfigureOTelLogging(configuration, TestServiceName));
        using var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<GivenOTelLoggingConfigurator>>();
        var exportConnection = otlpListener.AcceptTcpClientAsync(TestContext.Current.CancellationToken).AsTask();

        logger.LogInformation("a message for the otlp endpoint");
        _ = serviceProvider.GetRequiredService<LoggerProvider>().ForceFlush(500);

        using var connectedClient = await exportConnection.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        connectedClient.Connected.ShouldBeTrue();
    }

    [Fact]
    public void when_the_otlp_endpoint_is_not_a_valid_uri_then_log_messages_can_still_be_written()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["OpenTelemetry:OtlpEndpoint"] = "not a uri" }).Build();
        var services = new ServiceCollection();
        _ = services.AddLogging(builder => builder.ConfigureOTelLogging(configuration, TestServiceName));
        using var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<GivenOTelLoggingConfigurator>>();

        Action act = () => logger.LogInformation("a message with no valid otlp endpoint");

        act.ShouldNotThrow();
    }

    private static string ResourceServiceName(ServiceProvider serviceProvider) => ResourceAttribute(serviceProvider, "service.name");

    private static string ResourceAttribute(ServiceProvider serviceProvider, string key)
        => serviceProvider.GetRequiredService<LoggerProvider>().GetResource().Attributes
            .Single(attribute => attribute.Key == key).Value.ToString()!;
}
