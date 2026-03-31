using System.Reactive.Concurrency;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Features.Settings;
using AStar.Dev.OneDriveSync.Infrastructure.Theming;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Features.Settings;

public sealed class GivenASettingsViewModel
{
    private readonly IThemeService _themeService = Substitute.For<IThemeService>();

    public GivenASettingsViewModel()
    {
        RxApp.MainThreadScheduler = ImmediateScheduler.Instance;

        _ = _themeService.CurrentMode.Returns(ThemeMode.Auto);
        _ = _themeService.SetThemeAsync(Arg.Any<ThemeMode>(), Arg.Any<CancellationToken>())
            .Returns(new Result<ThemeMode, ErrorResponse>.Ok(ThemeMode.Auto));
    }

    [Fact]
    public void when_constructed_then_theme_modes_contains_auto_light_and_dark()
    {
        var sut = new SettingsViewModel(_themeService);

        sut.ThemeModes.ShouldBe([ThemeMode.Auto, ThemeMode.Light, ThemeMode.Dark]);
    }

    [Fact]
    public void when_constructed_then_selected_theme_matches_current_service_mode()
    {
        _ = _themeService.CurrentMode.Returns(ThemeMode.Dark);

        var sut = new SettingsViewModel(_themeService);

        sut.SelectedTheme.ShouldBe(ThemeMode.Dark);
    }

    [Fact]
    public async Task when_selected_theme_is_changed_then_theme_service_receives_set_theme_call()
    {
        var sut = new SettingsViewModel(_themeService);

        sut.SelectedTheme = ThemeMode.Dark;

        await Task.Yield();
        _ = await _themeService.Received(1).SetThemeAsync(ThemeMode.Dark, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_constructed_then_initial_mode_does_not_trigger_set_theme()
    {
        _ = new SettingsViewModel(_themeService);

        _ = _themeService.DidNotReceive().SetThemeAsync(Arg.Any<ThemeMode>(), Arg.Any<CancellationToken>());
    }
}
