using AStar.Dev.Wallpaper.Scrapper.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;

internal sealed class ScrapeConfigurationBuilder
{
    public ConnectionStrings ConnectionStrings { get; init; } = new("Data Source=:memory:");
    public UserConfiguration UserConfiguration { get; init; } = new("user@example.test", "username", "password", "session-cookie");
    public SearchConfiguration SearchConfiguration { get; init; } = new SearchConfigurationBuilder().Build();
    public ScrapeDirectories ScrapeDirectories { get; init; } = new("root-directory", "base-save-directory", "base-directory", "base-directory-famous", "sub-directory");

    public ScrapeConfiguration Build() => new(ConnectionStrings, UserConfiguration, SearchConfiguration, ScrapeDirectories);
}
