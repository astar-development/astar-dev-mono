using AStar.Dev.Wallpaper.Scraper.Support;
using Microsoft.Extensions.Configuration;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Support;

public sealed class GivenASqliteConnectionStringProvider
{
    [Fact]
    public void when_the_configuration_has_a_sqlite_connection_string_then_the_configured_value_is_used()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ScrapeConfiguration:ConnectionStrings:Sqlite"] = "Data Source=/configured/path/scraper.db", })
            .Build();

        SqliteConnectionStringProvider.Get(configuration).ShouldBe("Data Source=/configured/path/scraper.db");
    }

    [Fact]
    public void when_the_configuration_has_no_sqlite_connection_string_then_the_default_is_used()
    {
        var configuration = new ConfigurationBuilder().Build();

        SqliteConnectionStringProvider.Get(configuration).ShouldBe(SqliteConnectionStringProvider.DefaultConnectionString);
    }
}
