using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Startup;
using Microsoft.Extensions.Configuration;
using Testably.Abstractions.Testing;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Startup;

public class GivenSerilogConfigurator
{
    private readonly MockFileSystem fileSystem = new();

    private readonly IConfigurationRoot configuration = new ConfigurationBuilder().Build();

    [Fact]
    public void when_the_logs_directory_is_missing_then_it_is_created()
    {
        SerilogConfigurator.CreateLogger(fileSystem, configuration);

        fileSystem.Directory.Exists(ApplicationDirectories.LogsDirectory).ShouldBeTrue();
    }

    [Fact]
    public void when_the_logs_directory_already_exists_then_creating_the_logger_does_not_throw()
    {
        fileSystem.Directory.CreateDirectory(ApplicationDirectories.LogsDirectory);

        Should.NotThrow(() => SerilogConfigurator.CreateLogger(fileSystem, configuration));
    }

    [Fact]
    public void when_created_then_a_usable_logger_is_returned()
    {
        var logger = SerilogConfigurator.CreateLogger(fileSystem, configuration);

        logger.ShouldNotBeNull();
        Should.NotThrow(() => logger.Information("test message"));
    }
}
