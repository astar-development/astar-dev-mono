using AStarDev.LoggingSerilog.LogViewer;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace AStarDev.LoggingSerilog.Tests.Unit;

public sealed class GivenSerilogConfigurator
{
    private static IConfigurationRoot CreateConfiguration() =>
        new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();

    [Fact]
    public void when_the_create_logger_method_is_called_then_it_returns_the_logger_configured_as_expected()
    {
        var configuration = CreateConfiguration();
        var logFilePath   = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

        var logger = SerilogConfigurator.CreateLogger(configuration, logFilePath);

        logger.ShouldNotBeNull();
    }

    [Fact]
    public void when_the_create_logger_method_is_called_then_it_writes_log_entries_to_the_specified_file()
    {
        var configuration = CreateConfiguration();
        var logFilePath   = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

        var logger = SerilogConfigurator.CreateLogger(configuration, logFilePath);
        logger.Information("the expected message was logged");
        (logger as IDisposable)?.Dispose();

        var writtenFiles = Directory.GetFiles(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(logFilePath)}*");

        writtenFiles.ShouldNotBeEmpty();
        File.ReadAllText(writtenFiles.Single()).ShouldContain("the expected message was logged");

        foreach (var writtenFile in writtenFiles)
        {
            File.Delete(writtenFile);
        }
    }

    [Fact]
    public void when_the_create_logger_method_is_called_with_a_custom_rolling_interval_then_it_returns_the_logger_configured_as_expected()
    {
        var configuration = CreateConfiguration();
        var logFilePath   = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

        var logger = SerilogConfigurator.CreateLogger(configuration, logFilePath, RollingInterval.Hour);

        logger.ShouldNotBeNull();
    }

    [Fact]
    public void when_the_create_logger_method_is_called_with_a_custom_retained_file_count_limit_then_it_returns_the_logger_configured_as_expected()
    {
        var configuration = CreateConfiguration();
        var logFilePath   = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

        var logger = SerilogConfigurator.CreateLogger(configuration, logFilePath, retainedFileCountLimit: 3);

        logger.ShouldNotBeNull();
    }

    [Fact]
    public void when_the_create_logger_method_is_called_with_an_in_memory_log_sink_then_it_writes_log_entries_to_the_sink()
    {
        var configuration = CreateConfiguration();
        var logFilePath   = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        var inMemoryLogSink = new InMemoryLogSink();

        var logger = SerilogConfigurator.CreateLogger(configuration, logFilePath, inMemoryLogSink);
        logger.Information("the expected message was logged to the in memory sink");
        (logger as IDisposable)?.Dispose();

        var snapshot = inMemoryLogSink.GetSnapshot();

        snapshot.ShouldContain(entry => entry.RenderedMessage.Contains("the expected message was logged to the in memory sink"));

        var writtenFiles = Directory.GetFiles(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(logFilePath)}*");

        foreach (var writtenFile in writtenFiles)
        {
            File.Delete(writtenFile);
        }
    }
}
