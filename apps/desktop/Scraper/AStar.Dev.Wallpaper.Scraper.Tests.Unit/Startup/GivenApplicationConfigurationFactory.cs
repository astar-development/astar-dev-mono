using AStar.Dev.Wallpaper.Scraper.Startup;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Startup;

public class GivenApplicationConfigurationFactory
{
    [Fact]
    public void when_built_then_appsettings_json_is_loaded_from_the_base_path()
    {
        var configuration = ApplicationConfigurationFactory.Build(AppContext.BaseDirectory);

        configuration["scrapeConfiguration:applicationName"].ShouldBe("AStar Dev Wallpaper Scraper");
    }

    [Fact]
    public void when_built_then_environment_variables_are_layered_on_top_of_appsettings_json()
    {
        Environment.SetEnvironmentVariable("scrapeConfiguration__applicationName", "Overridden Application Name");

        try
        {
            var configuration = ApplicationConfigurationFactory.Build(AppContext.BaseDirectory);

            configuration["scrapeConfiguration:applicationName"].ShouldBe("Overridden Application Name");
        }
        finally
        {
            Environment.SetEnvironmentVariable("scrapeConfiguration__applicationName", null);
        }
    }

    [Fact]
    public void when_base_path_has_no_appsettings_json_then_build_throws()
    {
        string emptyDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(emptyDirectory);

        try
        {
            Should.Throw<FileNotFoundException>(() => ApplicationConfigurationFactory.Build(emptyDirectory));
        }
        finally
        {
            Directory.Delete(emptyDirectory);
        }
    }
}
