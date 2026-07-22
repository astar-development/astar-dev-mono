using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Theming;
using AStar.Dev.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Testably.Abstractions.Testing;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Theming;

public sealed class GivenThemeService
{
    private readonly MockFileSystem fileSystem = new();

    private readonly string settingsFilePath = ApplicationDirectories.DataDirectory.CombinePath("theme.json");

    private ThemeService CreateSut() => new(fileSystem, NullLogger<ThemeService>.Instance);

    [Fact]
    public void when_apply_theme_called_then_current_theme_reflects_the_selection()
    {
        var sut = CreateSut();

        sut.ApplyTheme(ThemeMode.Hacker);

        sut.CurrentTheme.ShouldBe(ThemeMode.Hacker);
    }

    [Fact]
    public void when_apply_theme_called_then_the_selection_is_persisted_to_disk()
    {
        var sut = CreateSut();

        sut.ApplyTheme(ThemeMode.Dark);

        fileSystem.File.Exists(settingsFilePath).ShouldBeTrue();
        fileSystem.File.ReadAllText(settingsFilePath).ShouldContain("Dark");
    }

    [Fact]
    public void when_initialize_called_with_no_persisted_file_then_current_theme_defaults_to_system()
    {
        var sut = CreateSut();

        sut.Initialize();

        sut.CurrentTheme.ShouldBe(ThemeMode.System);
    }

    [Fact]
    public void when_initialize_called_with_a_persisted_selection_then_current_theme_is_loaded()
    {
        fileSystem.Directory.CreateDirectory(ApplicationDirectories.DataDirectory);
        fileSystem.File.WriteAllText(settingsFilePath, """{"Theme":"Light"}""");
        var sut = CreateSut();

        sut.Initialize();

        sut.CurrentTheme.ShouldBe(ThemeMode.Light);
    }

    [Fact]
    public void when_initialize_called_with_a_corrupt_persisted_file_then_current_theme_defaults_to_system()
    {
        fileSystem.Directory.CreateDirectory(ApplicationDirectories.DataDirectory);
        fileSystem.File.WriteAllText(settingsFilePath, "not valid json");
        var sut = CreateSut();

        sut.Initialize();

        sut.CurrentTheme.ShouldBe(ThemeMode.System);
    }
}
