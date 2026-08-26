using AStarDev.LoggingOTel.LogViewer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
}
