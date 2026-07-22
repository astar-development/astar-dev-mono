using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Services;
using Avalonia.Controls;
using Microsoft.Extensions.Options;
using Testably.Abstractions.Testing;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Services;

public sealed class GivenUpdateService
{
    private const string LogFileName = "astar-dev-wallpaper-scraper-update.log";

    private readonly MockFileSystem fileSystem = new();

    public GivenUpdateService() =>
        fileSystem.Directory.CreateDirectory(Path.GetTempPath());

    [Fact]
    public async Task when_the_app_is_not_a_velopack_install_then_the_update_check_completes_without_prompting()
    {
        var sut = CreateUpdateService("https://github.com/astar-development/astar-dev-wallpaper-scraper");

        await Should.NotThrowAsync(() => sut.CheckForUpdatesAsync(null!));

        string logContent = ReadLog();
        logContent.ShouldContain("Not a Velopack install");
        logContent.ShouldNotContain("Update check failed");
    }

    [Fact]
    public async Task when_creating_the_update_manager_throws_then_the_error_tap_logs_the_failure_and_swallows_it()
    {
        var sut = CreateUpdateService(string.Empty);

        await Should.NotThrowAsync(() => sut.CheckForUpdatesAsync(null!));

        ReadLog().ShouldContain("Update check failed");
    }

    private UpdateService CreateUpdateService(string repositoryUrl) =>
        new(Options.Create(new UpdateConfiguration { RepositoryUrl = repositoryUrl }), fileSystem);

    private string ReadLog()
    {
        string logPath = Path.Combine(Path.GetTempPath(), LogFileName);

        return fileSystem.File.ReadAllText(logPath);
    }
}
