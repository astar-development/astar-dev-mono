using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Settings;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Settings;

public sealed class GivenASettingsViewModel
{
    private const string AccountIdValue = "account-1";
    private const string SecondAccountIdValue = "account-2";
    private const string TargetAccountIdValue = "target-id";
    private const string UnknownAccountIdValue = "unknown-id";
    private const int DefaultSyncIntervalMinutes = 30;
    private const int NewSyncIntervalMinutes = 15;

    private static ISettingsService BuildSettingsService(AppSettings? settings = null)
    {
        var service = Substitute.For<ISettingsService>();
        service.Current.Returns(settings ?? new AppSettings());
        service.SaveAsync().Returns(Task.CompletedTask);

        return service;
    }

    private static ILocalizationService BuildLocalizationService()
    {
        var loc = Substitute.For<ILocalizationService>();
        loc.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));
        loc.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(x => x.ArgAt<string>(0));
        loc.AvailableCultures.Returns([CultureInfo.GetCultureInfo("en-GB"), CultureInfo.GetCultureInfo("en-US")]);

        return loc;
    }

    private static SettingsViewModel BuildSut(ISettingsService? settingsService = null, IThemeService? themeService = null, ISyncScheduler? scheduler = null, IAccountRepository? repository = null, ILocalizationService? localizationService = null, IFolderPickerService? folderPickerService = null)
    {
        settingsService ??= BuildSettingsService();
        themeService ??= Substitute.For<IThemeService>();
        scheduler ??= Substitute.For<ISyncScheduler>();
        repository ??= Substitute.For<IAccountRepository>();
        localizationService ??= BuildLocalizationService();
        folderPickerService ??= Substitute.For<IFolderPickerService>();

        return new SettingsViewModel(settingsService, themeService, scheduler, repository, localizationService, folderPickerService);
    }

    private static OneDriveAccount BuildAccount(string accountId) => new()
    {
        Id = new AccountId(accountId),
        Profile = AccountProfileFactory.Create("Test User", "test@example.com")
    };

    [Fact]
    public void when_constructed_then_settings_are_not_saved()
    {
        var settingsService = BuildSettingsService();

        _ = BuildSut(settingsService: settingsService);

        settingsService.DidNotReceive().SaveAsync();
    }

    [Fact]
    public void when_constructed_then_theme_is_read_from_settings_service_current()
    {
        var settings = new AppSettings { Theme = AppTheme.Dark };
        var sut = BuildSut(settingsService: BuildSettingsService(settings));

        sut.Theme.ShouldBe(AppTheme.Dark);
    }

    [Fact]
    public void when_constructed_then_default_conflict_policy_is_read_from_settings_service_current()
    {
        var settings = new AppSettings { DefaultConflictPolicy = ConflictPolicy.LastWriteWins };
        var sut = BuildSut(settingsService: BuildSettingsService(settings));

        sut.DefaultConflictPolicy.ShouldBe(ConflictPolicy.LastWriteWins);
    }

    [Fact]
    public void when_constructed_then_sync_interval_minutes_is_read_from_settings_service_current()
    {
        var settings = new AppSettings { SyncIntervalMinutes = DefaultSyncIntervalMinutes };
        var sut = BuildSut(settingsService: BuildSettingsService(settings));

        sut.SyncIntervalMinutes.ShouldBe(DefaultSyncIntervalMinutes);
    }

    [Fact]
    public void when_constructed_then_theme_options_contains_exactly_four_entries()
    {
        var sut = BuildSut();

        sut.ThemeOptions.Count.ShouldBe(4);
    }

    [Fact]
    public void when_constructed_then_theme_options_covers_light_dark_system_and_hacker()
    {
        var sut = BuildSut();

        var themes = sut.ThemeOptions.Select(option => option.Theme).ToList();
        themes.ShouldContain(AppTheme.Light);
        themes.ShouldContain(AppTheme.Dark);
        themes.ShouldContain(AppTheme.System);
        themes.ShouldContain(AppTheme.Hacker);
    }

    [Fact]
    public void when_building_theme_options_then_light_uses_correct_key()
    {
        var loc = BuildLocalizationService();
        var sut = BuildSut(localizationService: loc);

        var lightOption = sut.ThemeOptions.Single(option => option.Theme == AppTheme.Light);

        lightOption.Label.ShouldBe("Settings.Theme.Light");
    }

    [Fact]
    public void when_building_theme_options_then_dark_uses_correct_key()
    {
        var loc = BuildLocalizationService();
        var sut = BuildSut(localizationService: loc);

        var darkOption = sut.ThemeOptions.Single(option => option.Theme == AppTheme.Dark);

        darkOption.Label.ShouldBe("Settings.Theme.Dark");
    }

    [Fact]
    public void when_building_theme_options_then_system_uses_correct_key()
    {
        var loc = BuildLocalizationService();
        var sut = BuildSut(localizationService: loc);

        var systemOption = sut.ThemeOptions.Single(option => option.Theme == AppTheme.System);

        systemOption.Label.ShouldBe("Settings.Theme.System");
    }

    [Fact]
    public void when_culture_changed_then_theme_options_is_rebuilt()
    {
        var loc = BuildLocalizationService();
        var sut = BuildSut(localizationService: loc);
        loc.ClearReceivedCalls();

        loc.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(new object(), CultureInfo.GetCultureInfo("fr-FR"));

        loc.Received(1).GetLocal("Settings.Theme.Light");
        loc.Received(1).GetLocal("Settings.Theme.Dark");
        loc.Received(1).GetLocal("Settings.Theme.System");
        loc.Received(1).GetLocal("Settings.Theme.Hacker");
    }

    [Fact]
    public void when_building_theme_options_then_hacker_uses_correct_key()
    {
        var loc = BuildLocalizationService();
        var sut = BuildSut(localizationService: loc);

        var hackerOption = sut.ThemeOptions.Single(option => option.Theme == AppTheme.Hacker);

        hackerOption.Label.ShouldBe("Settings.Theme.Hacker");
    }

    [Fact]
    public void when_culture_changed_then_theme_options_rebuild_includes_hacker_key()
    {
        var loc = BuildLocalizationService();
        var sut = BuildSut(localizationService: loc);
        loc.ClearReceivedCalls();

        loc.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(new object(), CultureInfo.GetCultureInfo("fr-FR"));

        loc.Received(1).GetLocal("Settings.Theme.Hacker");
    }

    [Fact]
    public void when_constructed_then_interval_options_contains_exactly_five_entries()
    {
        var sut = BuildSut();

        sut.IntervalOptions.Count.ShouldBe(5);
    }

    [Fact]
    public void when_constructed_then_interval_options_minutes_values_are_5_15_30_60_120()
    {
        var sut = BuildSut();

        var minutes = sut.IntervalOptions.Select(option => option.Minutes).ToList();
        minutes.ShouldContain(5);
        minutes.ShouldContain(15);
        minutes.ShouldContain(30);
        minutes.ShouldContain(60);
        minutes.ShouldContain(120);
    }

    [Fact]
    public void when_building_interval_options_then_minute_entries_use_minutes_key()
    {
        var loc = BuildLocalizationService();
        var sut = BuildSut(localizationService: loc);

        loc.Received().GetLocal("Settings.DeltaSyncInterval.Minutes", Arg.Any<object[]>());
    }

    [Fact]
    public void when_building_interval_options_then_2_hour_entry_uses_hours_key()
    {
        var loc = BuildLocalizationService();
        var sut = BuildSut(localizationService: loc);

        loc.Received(1).GetLocal("Settings.Interval.Hours", Arg.Any<object[]>());
    }

    [Fact]
    public void when_culture_changed_then_interval_options_is_rebuilt()
    {
        var loc = BuildLocalizationService();
        var sut = BuildSut(localizationService: loc);
        loc.ClearReceivedCalls();

        loc.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(new object(), CultureInfo.GetCultureInfo("fr-FR"));

        loc.Received().GetLocal("Settings.DeltaSyncInterval.Minutes", Arg.Any<object[]>());
        loc.Received(1).GetLocal("Settings.Interval.Hours", Arg.Any<object[]>());
    }

    [Fact]
    public void when_constructed_then_policy_options_contains_exactly_five_entries()
    {
        var sut = BuildSut();

        sut.PolicyOptions.Count.ShouldBe(5);
    }

    [Fact]
    public void when_constructed_then_policy_options_covers_all_five_conflict_policy_values()
    {
        var sut = BuildSut();

        var policies = sut.PolicyOptions.Select(option => option.Policy).ToList();
        policies.ShouldContain(ConflictPolicy.Ignore);
        policies.ShouldContain(ConflictPolicy.KeepBoth);
        policies.ShouldContain(ConflictPolicy.LastWriteWins);
        policies.ShouldContain(ConflictPolicy.LocalWins);
        policies.ShouldContain(ConflictPolicy.RemoteWins);
    }

    [Fact]
    public void when_constructed_then_policy_options_labels_are_retrieved_via_localisation_service()
    {
        var loc = BuildLocalizationService();
        var sut = BuildSut(localizationService: loc);

        loc.Received(1).GetLocal("ConflictPolicy.Ignore");
        loc.Received(1).GetLocal("ConflictPolicy.KeepBoth");
        loc.Received(1).GetLocal("ConflictPolicy.LastWriteWins");
        loc.Received(1).GetLocal("ConflictPolicy.LocalWins");
        loc.Received(1).GetLocal("ConflictPolicy.RemoteWins");
    }

    [Fact]
    public void when_constructed_then_policy_options_descriptions_are_retrieved_via_localisation_service()
    {
        var loc = BuildLocalizationService();
        var sut = BuildSut(localizationService: loc);

        loc.Received(1).GetLocal("ConflictPolicy.Ignore.Description");
        loc.Received(1).GetLocal("ConflictPolicy.KeepBoth.Description");
        loc.Received(1).GetLocal("ConflictPolicy.LastWriteWins.Description");
        loc.Received(1).GetLocal("ConflictPolicy.LocalWins.Description");
        loc.Received(1).GetLocal("ConflictPolicy.RemoteWins.Description");
    }

    [Fact]
    public void when_culture_changed_then_policy_options_is_rebuilt()
    {
        var loc = BuildLocalizationService();
        var sut = BuildSut(localizationService: loc);
        loc.ClearReceivedCalls();

        loc.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(new object(), CultureInfo.GetCultureInfo("fr-FR"));

        loc.Received(1).GetLocal("ConflictPolicy.Ignore");
        loc.Received(1).GetLocal("ConflictPolicy.KeepBoth");
        loc.Received(1).GetLocal("ConflictPolicy.LastWriteWins");
        loc.Received(1).GetLocal("ConflictPolicy.LocalWins");
        loc.Received(1).GetLocal("ConflictPolicy.RemoteWins");
    }

    [Fact]
    public void when_constructed_then_language_options_count_matches_available_cultures()
    {
        var loc = BuildLocalizationService();
        var sut = BuildSut(localizationService: loc);

        sut.LanguageOptions.Count.ShouldBe(2);
    }

    [Fact]
    public void when_constructed_then_language_options_cultures_match_available_cultures()
    {
        var loc = BuildLocalizationService();
        var sut = BuildSut(localizationService: loc);

        var names = sut.LanguageOptions.Select(option => option.Culture.Name).ToList();
        names.ShouldContain("en-GB");
        names.ShouldContain("en-US");
    }

    [Fact]
    public void when_culture_changed_then_language_options_is_rebuilt()
    {
        var loc = BuildLocalizationService();
        var sut = BuildSut(localizationService: loc);
        var initialOptions = sut.LanguageOptions;
        loc.AvailableCultures.Returns([CultureInfo.GetCultureInfo("en-GB")]);

        loc.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(new object(), CultureInfo.GetCultureInfo("en-GB"));

        sut.LanguageOptions.ShouldNotBeSameAs(initialOptions);
    }

    [Fact]
    public async Task when_select_culture_async_called_then_localization_service_set_culture_async_is_called()
    {
        var loc = BuildLocalizationService();
        var sut = BuildSut(localizationService: loc);
        var culture = CultureInfo.GetCultureInfo("en-US");

        await sut.SelectCultureAsync(culture);

        await loc.Received(1).SetCultureAsync(culture);
    }

    [Fact]
    public async Task when_select_culture_async_called_then_settings_locale_is_updated()
    {
        var settings = new AppSettings { Locale = "en-GB" };
        var settingsService = BuildSettingsService(settings);
        var sut = BuildSut(settingsService: settingsService);

        await sut.SelectCultureAsync(CultureInfo.GetCultureInfo("en-US"));

        settingsService.Current.Locale.ShouldBe("en-US");
    }

    [Fact]
    public async Task when_select_culture_async_called_then_settings_service_save_async_is_called()
    {
        var settingsService = BuildSettingsService();
        var sut = BuildSut(settingsService: settingsService);

        await sut.SelectCultureAsync(CultureInfo.GetCultureInfo("en-US"));

        await settingsService.Received().SaveAsync();
    }

    [Fact]
    public void when_constructed_then_account_settings_is_empty()
    {
        var sut = BuildSut();

        sut.AccountSettings.ShouldBeEmpty();
    }

    [Fact]
    public void when_theme_is_set_then_theme_service_apply_is_called_with_new_value()
    {
        var themeService = Substitute.For<IThemeService>();
        var sut = BuildSut(themeService: themeService);

        sut.Theme = AppTheme.Dark;

        themeService.Received(1).Apply(AppTheme.Dark);
    }

    [Fact]
    public void when_theme_is_set_then_settings_service_current_theme_is_updated()
    {
        var settings = new AppSettings { Theme = AppTheme.Light };
        var settingsService = BuildSettingsService(settings);
        var sut = BuildSut(settingsService: settingsService);

        sut.Theme = AppTheme.Dark;

        settingsService.Current.Theme.ShouldBe(AppTheme.Dark);
    }

    [Fact]
    public void when_theme_is_set_then_settings_service_save_async_is_called()
    {
        var settingsService = BuildSettingsService();
        var sut = BuildSut(settingsService: settingsService);

        sut.Theme = AppTheme.Dark;

        settingsService.Received().SaveAsync();
    }

    [Fact]
    public void when_default_conflict_policy_is_set_then_settings_service_current_default_conflict_policy_is_updated()
    {
        var settings = new AppSettings { DefaultConflictPolicy = ConflictPolicy.Ignore };
        var settingsService = BuildSettingsService(settings);
        var sut = BuildSut(settingsService: settingsService);

        sut.DefaultConflictPolicy = ConflictPolicy.RemoteWins;

        settingsService.Current.DefaultConflictPolicy.ShouldBe(ConflictPolicy.RemoteWins);
    }

    [Fact]
    public void when_default_conflict_policy_is_set_then_settings_service_save_async_is_called()
    {
        var settingsService = BuildSettingsService();
        var sut = BuildSut(settingsService: settingsService);

        sut.DefaultConflictPolicy = ConflictPolicy.LocalWins;

        settingsService.Received().SaveAsync();
    }

    [Fact]
    public void when_sync_interval_minutes_is_set_then_settings_service_current_sync_interval_minutes_is_updated()
    {
        var settings = new AppSettings { SyncIntervalMinutes = 60 };
        var settingsService = BuildSettingsService(settings);
        var sut = BuildSut(settingsService: settingsService);

        sut.SyncIntervalMinutes = NewSyncIntervalMinutes;

        settingsService.Current.SyncIntervalMinutes.ShouldBe(NewSyncIntervalMinutes);
    }

    [Fact]
    public void when_sync_interval_minutes_is_set_then_scheduler_set_interval_is_called_with_correct_timespan()
    {
        var scheduler = Substitute.For<ISyncScheduler>();
        var sut = BuildSut(scheduler: scheduler);

        sut.SyncIntervalMinutes = NewSyncIntervalMinutes;

        scheduler.Received(1).SetInterval(TimeSpan.FromMinutes(NewSyncIntervalMinutes));
    }

    [Fact]
    public void when_sync_interval_minutes_is_set_then_settings_service_save_async_is_called()
    {
        var settingsService = BuildSettingsService();
        var sut = BuildSut(settingsService: settingsService);

        sut.SyncIntervalMinutes = NewSyncIntervalMinutes;

        settingsService.Received().SaveAsync();
    }

    [Fact]
    public void when_load_accounts_called_with_two_accounts_then_account_settings_has_two_entries()
    {
        var sut = BuildSut();
        var accounts = new[] { BuildAccount(AccountIdValue), BuildAccount(SecondAccountIdValue) };

        sut.LoadAccounts(accounts);

        sut.AccountSettings.Count.ShouldBe(2);
    }

    [Fact]
    public void when_load_accounts_called_twice_then_previous_entries_are_replaced()
    {
        var sut = BuildSut();
        sut.LoadAccounts([BuildAccount(AccountIdValue)]);

        sut.LoadAccounts([BuildAccount(SecondAccountIdValue), BuildAccount(TargetAccountIdValue)]);

        sut.AccountSettings.Count.ShouldBe(2);
        sut.AccountSettings.ShouldNotContain(vm => vm.AccountId == AccountIdValue);
    }

    [Fact]
    public void when_load_accounts_called_with_empty_enumerable_then_account_settings_is_empty()
    {
        var sut = BuildSut();
        sut.LoadAccounts([BuildAccount(AccountIdValue)]);

        sut.LoadAccounts([]);

        sut.AccountSettings.ShouldBeEmpty();
    }

    [Fact]
    public void when_add_account_called_then_account_settings_gains_one_entry()
    {
        var sut = BuildSut();

        sut.AddAccount(BuildAccount(AccountIdValue));

        sut.AccountSettings.Count.ShouldBe(1);
    }

    [Fact]
    public void when_remove_account_called_with_existing_id_then_that_entry_is_removed()
    {
        var sut = BuildSut();
        sut.AddAccount(BuildAccount(TargetAccountIdValue));

        sut.RemoveAccount(TargetAccountIdValue);

        sut.AccountSettings.ShouldNotContain(vm => vm.AccountId == TargetAccountIdValue);
    }

    [Fact]
    public void when_remove_account_called_with_unknown_id_then_account_settings_is_unchanged()
    {
        var sut = BuildSut();
        sut.AddAccount(BuildAccount(AccountIdValue));

        sut.RemoveAccount(UnknownAccountIdValue);

        sut.AccountSettings.Count.ShouldBe(1);
    }
}
