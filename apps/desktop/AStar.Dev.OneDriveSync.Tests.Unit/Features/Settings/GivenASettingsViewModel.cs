using System;
using System.Collections.Frozen;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Features.Dashboard;
using AStar.Dev.OneDriveSync.Features.Onboarding;
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
    private readonly IUserTypeService     _userTypeService     = Substitute.For<IUserTypeService>();
    private readonly IDialogService       _dialogService       = Substitute.For<IDialogService>();
    private readonly INotificationsService _notificationsService = Substitute.For<INotificationsService>();

    public GivenASettingsViewModel()
    {
        RxApp.MainThreadScheduler = ImmediateScheduler.Instance;

        _ = _themeService.CurrentMode.Returns(ThemeMode.Auto);
        _ = _themeService.SetThemeAsync(Arg.Any<ThemeMode>(), Arg.Any<CancellationToken>())
            .Returns(new Result<ThemeMode, ErrorResponse>.Ok(ThemeMode.Auto));

        _ = _localisationService.SupportedLocales.Returns(FrozenSet.Create("en-GB"));
        _ = _localisationService.CurrentLocale.Returns("en-GB");
        _ = _localisationService.SetLocaleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<string, ErrorResponse>.Ok("en-GB"));
        _ = _localisationService.GetString(Arg.Any<string>()).Returns(string.Empty);

        _ = _userTypeService.CurrentUserType.Returns(UserType.Casual);

        _ = _dialogService.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _ = _notificationsService.NotificationsEnabled.Returns(true);
        _ = _notificationsService.SetEnabledAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new Result<bool, ErrorResponse>.Ok(true));
    }

    private SettingsViewModel BuildSut() =>
        new(_themeService, _localisationService, _userTypeService, _dialogService, _notificationsService);

    [Fact]
    public void when_constructed_then_theme_modes_contains_auto_light_and_dark()
    {
        var sut = BuildSut();

        sut.ThemeModes.ShouldBe([ThemeMode.Auto, ThemeMode.Light, ThemeMode.Dark]);
    }

    [Fact]
    public void when_constructed_then_selected_theme_matches_current_service_mode()
    {
        _ = _themeService.CurrentMode.Returns(ThemeMode.Dark);

        var sut = BuildSut();

        sut.SelectedTheme.ShouldBe(ThemeMode.Dark);
    }

    [Fact]
    public async Task when_selected_theme_is_changed_then_theme_service_receives_set_theme_call()
    {
        var sut = BuildSut();

        sut.SelectedTheme = ThemeMode.Dark;

        await Task.Yield();
        _ = await _themeService.Received(1).SetThemeAsync(ThemeMode.Dark, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_constructed_then_initial_mode_does_not_trigger_set_theme()
    {
        _ = BuildSut();

        _ = _themeService.DidNotReceive().SetThemeAsync(Arg.Any<ThemeMode>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_constructed_then_selected_locale_matches_current_service_locale()
    {
        var sut = BuildSut();

        sut.SelectedLocale.ShouldBe("en-GB");
    }

    [Fact]
    public void when_constructed_then_supported_locales_contains_en_gb()
    {
        var sut = BuildSut();

        sut.SupportedLocales.ShouldContain("en-GB");
    }

    [Fact]
    public async Task when_locale_command_is_executed_then_localisation_service_receives_set_locale_call()
    {
        var sut = BuildSut();

        await sut.ChangeLocaleCommand.Execute("en-GB").FirstAsync();

        _ = await _localisationService.Received(1).SetLocaleAsync("en-GB", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_constructed_then_initial_locale_does_not_trigger_set_locale()
    {
        _ = BuildSut();

        _ = _localisationService.DidNotReceive().SetLocaleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_constructed_then_user_types_contains_casual_and_power_user()
    {
        var sut = BuildSut();

        sut.UserTypes.ShouldBe([UserType.Casual, UserType.PowerUser]);
    }

    [Fact]
    public void when_constructed_then_selected_user_type_matches_current_service_type()
    {
        _ = _userTypeService.CurrentUserType.Returns(UserType.PowerUser);

        var sut = BuildSut();

        sut.SelectedUserType.ShouldBe(UserType.PowerUser);
    }

    [Fact]
    public async Task when_selected_user_type_changes_to_power_user_then_user_type_service_receives_request_change_call()
    {
        var sut = BuildSut();

        sut.SelectedUserType = UserType.PowerUser;

        await Task.Yield();
        _userTypeService.Received(1).RequestUserTypeChange(UserType.PowerUser);
    }

    [Fact]
    public async Task when_selected_user_type_changes_to_power_user_then_confirmation_dialog_is_shown()
    {
        _userTypeService.When(svc => svc.RequestUserTypeChange(UserType.PowerUser))
            .Do(_ => _userTypeService.ConfirmationRequested += Raise.EventWith(EventArgs.Empty));

        var sut = BuildSut();

        sut.SelectedUserType = UserType.PowerUser;

        await Task.Delay(50, TestContext.Current.CancellationToken);
        _ = await _dialogService.Received(1).ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_selected_user_type_changes_to_casual_then_no_confirmation_dialog_is_shown()
    {
        _ = _userTypeService.CurrentUserType.Returns(UserType.PowerUser);
        var sut = BuildSut();

        sut.SelectedUserType = UserType.Casual;

        await Task.Yield();
        _ = _dialogService.DidNotReceive().ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_constructed_then_notifications_enabled_matches_service_state()
    {
        _ = _notificationsService.NotificationsEnabled.Returns(false);

        var sut = BuildSut();

        sut.NotificationsEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task when_notifications_enabled_is_changed_then_notifications_service_receives_set_enabled_call()
    {
        var sut = BuildSut();

        sut.NotificationsEnabled = false;

        await Task.Yield();
        _ = await _notificationsService.Received(1).SetEnabledAsync(false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_constructed_then_initial_notifications_state_does_not_trigger_set_enabled()
    {
        _ = BuildSut();

        _ = _notificationsService.DidNotReceive().SetEnabledAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }
}
