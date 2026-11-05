using System.Net;
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
    private static IConfigurationRoot CreateConfiguration() =>
        new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();

    [Fact]
    public void when_configure_logging_is_called_then_a_usable_logger_is_produced()
    {
        var configuration = CreateConfiguration();
        var services = new ServiceCollection();
        _ = services.AddLogging(builder => builder.ConfigureOTelLogging(configuration));
        using var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<GivenOTelLoggingConfigurator>>();

        logger.ShouldNotBeNull();
    }

    [Fact]
    public void when_configure_logging_is_called_then_log_messages_can_be_written_without_throwing()
    {
        var configuration = CreateConfiguration();
        var services = new ServiceCollection();
        _ = services.AddLogging(builder => builder.ConfigureOTelLogging(configuration));
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
        _ = services.AddLogging(builder => builder.ConfigureOTelLogging(configuration, inMemoryLogProcessor));
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
    public void when_no_service_name_is_supplied_then_the_sdk_default_service_name_is_kept()
    {
        var configuration = CreateConfiguration();
        var services = new ServiceCollection();
        _ = services.AddLogging(builder => builder.ConfigureOTelLogging(configuration));
        using var serviceProvider = services.BuildServiceProvider();

        ResourceServiceName(serviceProvider).ShouldStartWith("unknown_service");
    }

    [Fact]
    public async Task when_an_otlp_endpoint_is_configured_then_log_records_are_exported_to_it()
    {
        using var otlpListener = new TcpListener(IPAddress.Loopback, 0);
        otlpListener.Start();
        var otlpPort = ((IPEndPoint)otlpListener.LocalEndpoint).Port;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["OpenTelemetry:OtlpEndpoint"] = $"http://127.0.0.1:{otlpPort}" }).Build();
        var services = new ServiceCollection();
        _ = services.AddLogging(builder => builder.ConfigureOTelLogging(configuration));
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
        _ = services.AddLogging(builder => builder.ConfigureOTelLogging(configuration));
        using var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<GivenOTelLoggingConfigurator>>();

        Action act = () => logger.LogInformation("a message with no valid otlp endpoint");

        act.ShouldNotThrow();
    }

    private static string ResourceServiceName(ServiceProvider serviceProvider)
        => serviceProvider.GetRequiredService<LoggerProvider>().GetResource().Attributes
            .Single(attribute => attribute.Key == "service.name").Value.ToString()!;
}
