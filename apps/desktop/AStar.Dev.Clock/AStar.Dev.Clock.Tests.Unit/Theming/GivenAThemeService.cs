using System.Text.Json;
using AStar.Dev.Clock.Theming;
using AStarDev.Utilities;
using Microsoft.Extensions.Logging;
using Testably.Abstractions.Testing;

namespace AStar.Dev.Clock.Tests.Unit.Theming;

public sealed class GivenAThemeService
{
    private static readonly string settingsFilePath = ApplicationDirectories.DataDirectory.CombinePath("theme.json");

    private static ThemeService CreateSut(MockFileSystem fileSystem) => new(fileSystem, Substitute.For<ILogger<ThemeService>>());

    [Fact]
    public void when_initialized_with_no_persisted_theme_then_current_theme_defaults_to_system()
    {
        var sut = CreateSut(new MockFileSystem());

        sut.Initialize();

        sut.CurrentTheme.ShouldBe(ThemeMode.System);
    }

    [Fact]
    public void when_initialized_with_a_valid_persisted_theme_then_current_theme_is_loaded()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory(ApplicationDirectories.DataDirectory);
        fileSystem.File.WriteAllText(settingsFilePath, """{"Theme":"Dark"}""");
        var sut = CreateSut(fileSystem);

        sut.Initialize();

        sut.CurrentTheme.ShouldBe(ThemeMode.Dark);
    }

    [Fact]
    public void when_initialized_with_a_corrupt_persisted_theme_then_current_theme_falls_back_to_system()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory(ApplicationDirectories.DataDirectory);
        fileSystem.File.WriteAllText(settingsFilePath, "{ not valid json");
        var sut = CreateSut(fileSystem);

        sut.Initialize();

        sut.CurrentTheme.ShouldBe(ThemeMode.System);
    }

    [Fact]
    public void when_initialized_with_a_corrupt_persisted_theme_then_no_exception_is_thrown()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory(ApplicationDirectories.DataDirectory);
        fileSystem.File.WriteAllText(settingsFilePath, "{ not valid json");
        var sut = CreateSut(fileSystem);

        Should.NotThrow(sut.Initialize);
    }

    [Fact]
    public void when_theme_is_applied_then_current_theme_reflects_the_new_value()
    {
        var sut = CreateSut(new MockFileSystem());

        sut.ApplyTheme(ThemeMode.Dark);

        sut.CurrentTheme.ShouldBe(ThemeMode.Dark);
    }

    [Fact]
    public void when_theme_is_applied_then_the_selection_is_persisted_to_the_settings_file()
    {
        var fileSystem = new MockFileSystem();
        var sut = CreateSut(fileSystem);

        sut.ApplyTheme(ThemeMode.Light);

        fileSystem.File.Exists(settingsFilePath).ShouldBeTrue();
    }

    [Fact]
    public void when_theme_is_applied_then_the_persisted_file_contains_the_selected_theme()
    {
        var fileSystem = new MockFileSystem();
        var sut = CreateSut(fileSystem);

        sut.ApplyTheme(ThemeMode.Dark);

        using var document = JsonDocument.Parse(fileSystem.File.ReadAllText(settingsFilePath));
        document.RootElement.GetProperty("Theme").GetString().ShouldBe("Dark");
    }
}
