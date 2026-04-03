using System.Collections.Frozen;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Features.Settings;
using AStar.Dev.OneDriveSync.Infrastructure.Localisation;
using AStar.Dev.OneDriveSync.Infrastructure.Theming;
using NSubstitute;
using ReactiveUI;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Features.Settings;

public sealed class GivenASettingsViewModel
{
    private readonly IThemeService        _themeService        = Substitute.For<IThemeService>();
    private readonly ILocalisationService _localisationService = Substitute.For<ILocalisationService>();

    public GivenASettingsViewModel()
    {
        RxApp.MainThreadScheduler = ImmediateScheduler.Instance;

        _ = _themeService.CurrentMode.Returns(ThemeMode.Auto);
        _ = _themeService.SetThemeAsync(Arg.Any<ThemeMode>(), Arg.Any<CancellationToken>())
            .Returns(new Result<ThemeMode, ErrorResponse>.Ok(ThemeMode.Auto));

        _ = _localisationService.SupportedLocales.Returns(FrozenSet.Create("en-GB"));
        _ = _localisationService.SetLocaleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<string, ErrorResponse>.Ok("en-GB"));
    }

    [Fact]
    public void when_constructed_then_theme_modes_contains_auto_light_and_dark()
    {
        _ = _localisationService.CurrentLocale.Returns("en-GB");
        var sut = new SettingsViewModel(_themeService, _localisationService);

        sut.ThemeModes.ShouldBe([ThemeMode.Auto, ThemeMode.Light, ThemeMode.Dark]);
    }

    [Fact]
    public void when_constructed_then_selected_theme_matches_current_service_mode()
    {
        _ = _themeService.CurrentMode.Returns(ThemeMode.Dark);
        _ = _localisationService.CurrentLocale.Returns("en-GB");

        var sut = new SettingsViewModel(_themeService, _localisationService);

        sut.SelectedTheme.ShouldBe(ThemeMode.Dark);
    }

    [Fact]
    public async Task when_selected_theme_is_changed_then_theme_service_receives_set_theme_call()
    {
        _ = _localisationService.CurrentLocale.Returns("en-GB");
        var sut = new SettingsViewModel(_themeService, _localisationService);

        sut.SelectedTheme = ThemeMode.Dark;

        await Task.Yield();
        _ = await _themeService.Received(1).SetThemeAsync(ThemeMode.Dark, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_constructed_then_initial_mode_does_not_trigger_set_theme()
    {
        _ = _localisationService.CurrentLocale.Returns("en-GB");
        _ = new SettingsViewModel(_themeService, _localisationService);

        _ = _themeService.DidNotReceive().SetThemeAsync(Arg.Any<ThemeMode>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_constructed_then_selected_locale_matches_current_service_locale()
    {
        _ = _localisationService.CurrentLocale.Returns("en-GB");
        var sut = new SettingsViewModel(_themeService, _localisationService);

        sut.SelectedLocale.ShouldBe("en-GB");
    }

    [Fact]
    public void when_constructed_then_supported_locales_contains_en_gb()
    {
        _ = _localisationService.CurrentLocale.Returns("en-GB");
        var sut = new SettingsViewModel(_themeService, _localisationService);

        sut.SupportedLocales.ShouldContain("en-GB");
    }

    [Fact]
    public async Task when_locale_command_is_executed_then_localisation_service_receives_set_locale_call()
    {
        _ = _localisationService.CurrentLocale.Returns("en-GB");
        var sut = new SettingsViewModel(_themeService, _localisationService);

        await sut.ChangeLocaleCommand.Execute("en-GB").FirstAsync();

        _ = await _localisationService.Received(1).SetLocaleAsync("en-GB", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_constructed_then_initial_locale_does_not_trigger_set_locale()
    {
        _ = _localisationService.CurrentLocale.Returns("en-GB");
        _ = new SettingsViewModel(_themeService, _localisationService);

        _ = _localisationService.DidNotReceive().SetLocaleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
