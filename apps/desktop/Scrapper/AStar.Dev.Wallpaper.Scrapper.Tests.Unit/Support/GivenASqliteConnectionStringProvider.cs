using AStar.Dev.Wallpaper.Scrapper.Support;
using Microsoft.Extensions.Configuration;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Support;

public sealed class GivenASqliteConnectionStringProvider
{
    [Fact]
    public void when_the_configuration_has_a_sqlite_connection_string_then_the_configured_value_is_used()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ScrapeConfiguration:ConnectionStrings:Sqlite"] = "Data Source=/configured/path/scrapper.db", })
            .Build();

        SqliteConnectionStringProvider.Get(configuration).ShouldBe("Data Source=/configured/path/scrapper.db");
    }

    [Fact]
    public void when_the_configuration_has_no_sqlite_connection_string_then_the_default_is_used()
    {
        var configuration = new ConfigurationBuilder().Build();

        SqliteConnectionStringProvider.Get(configuration).ShouldBe(SqliteConnectionStringProvider.DefaultConnectionString);
    }
}
