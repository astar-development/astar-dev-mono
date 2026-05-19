using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using Microsoft.Extensions.Logging;
using Testably.Abstractions.Testing;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Settings;

[Collection("SettingsService")]
public sealed class GivenASettingsServiceWithDefaults
{
    private const string SettingsPath = "/app/settings.json";

    private static MockFileSystem CreateMockFileSystem()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Directory.CreateDirectory("/app");

        return mockFileSystem;
    }

    private static SettingsService CreateService()
        => new(CreateMockFileSystem(), Substitute.For<ILogger<SettingsService>>(), SettingsPath);

    [Fact]
    public void when_constructed_then_settings_have_default_values()
    {
        var service = CreateService();

        _ = service.Current.ShouldNotBeNull();
        service.Current.Theme.ShouldBe(AppTheme.System);
        service.Current.Locale.ShouldBe("en-GB");
        service.Current.DefaultConflictPolicy.ShouldBe(ConflictPolicy.Ignore);
        service.Current.SyncIntervalMinutes.ShouldBe(60);
    }

    [Fact]
    public void when_current_is_read_then_it_is_app_settings()
    {
        var service = CreateService();

        var settings = service.Current;

        _ = settings.ShouldNotBeNull();
        _ = settings.ShouldBeOfType<AppSettings>();
    }

    [Fact]
    public void when_theme_is_set_then_it_is_preserved()
    {
        var service = CreateService();

        service.Current.Theme = AppTheme.Dark;

        service.Current.Theme.ShouldBe(AppTheme.Dark);
    }

    [Fact]
    public void when_locale_is_set_then_it_is_preserved()
    {
        var service = CreateService();

        service.Current.Locale = "fr-FR";

        service.Current.Locale.ShouldBe("fr-FR");
    }

    [Fact]
    public void when_default_conflict_policy_is_set_then_it_is_preserved()
    {
        var service = CreateService();

        service.Current.DefaultConflictPolicy = ConflictPolicy.LastWriteWins;

        service.Current.DefaultConflictPolicy.ShouldBe(ConflictPolicy.LastWriteWins);
    }

    [Fact]
    public void when_sync_interval_minutes_is_set_then_it_is_preserved()
    {
        var service = CreateService();

        service.Current.SyncIntervalMinutes = 30;

        service.Current.SyncIntervalMinutes.ShouldBe(30);
    }

    [Fact]
    public async Task when_save_async_called_then_settingschanged_event_is_raised()
    {
        var service = CreateService();
        bool eventRaised = false;
        AppSettings? changedSettings = null;
        service.SettingsChanged += (s, settings) =>
        {
            eventRaised = true;
            changedSettings = settings;
        };

        service.Current.Theme = AppTheme.Light;
        await service.SaveAsync();

        eventRaised.ShouldBeTrue();
        _ = changedSettings.ShouldNotBeNull();
        changedSettings.Theme.ShouldBe(AppTheme.Light);
    }

    [Fact]
    public async Task when_save_async_called_then_settings_are_persisted()
    {
        var service = CreateService();
        service.Current.Locale = "de-DE";
        service.Current.SyncIntervalMinutes = 45;

        await service.SaveAsync();

        service.Current.Locale.ShouldBe("de-DE");
        service.Current.SyncIntervalMinutes.ShouldBe(45);
    }

    [Fact]
    public async Task when_load_async_called_then_no_exception_is_thrown()
    {
        var service = CreateService();

        await Should.NotThrowAsync(() => service.LoadAsync());
    }

    [Fact]
    public async Task when_load_async_called_then_current_settings_is_not_null()
    {
        var service = CreateService();

        await service.LoadAsync();

        _ = service.Current.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(AppTheme.System)]
    [InlineData(AppTheme.Light)]
    [InlineData(AppTheme.Dark)]
    public void when_any_theme_is_set_then_it_is_preserved(AppTheme theme)
    {
        var service = CreateService();

        service.Current.Theme = theme;

        service.Current.Theme.ShouldBe(theme);
    }

    [Theory]
    [InlineData("en-GB")]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    [InlineData("es-ES")]
    public void when_any_locale_is_set_then_it_is_preserved(string locale)
    {
        var service = CreateService();

        service.Current.Locale = locale;

        service.Current.Locale.ShouldBe(locale);
    }

    [Theory]
    [InlineData(ConflictPolicy.Ignore)]
    [InlineData(ConflictPolicy.KeepBoth)]
    [InlineData(ConflictPolicy.LastWriteWins)]
    [InlineData(ConflictPolicy.LocalWins)]
    public void when_any_conflict_policy_is_set_then_it_is_preserved(ConflictPolicy policy)
    {
        var service = CreateService();

        service.Current.DefaultConflictPolicy = policy;

        service.Current.DefaultConflictPolicy.ShouldBe(policy);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    public void when_any_sync_interval_is_set_then_it_is_preserved(int minutes)
    {
        var service = CreateService();

        service.Current.SyncIntervalMinutes = minutes;

        service.Current.SyncIntervalMinutes.ShouldBe(minutes);
    }

    [Fact]
    public void when_multiple_settings_are_changed_then_all_are_maintained()
    {
        var service = CreateService();

        service.Current.Theme = AppTheme.Dark;
        service.Current.Locale = "fr-FR";
        service.Current.DefaultConflictPolicy = ConflictPolicy.KeepBoth;
        service.Current.SyncIntervalMinutes = 45;

        service.Current.Theme.ShouldBe(AppTheme.Dark);
        service.Current.Locale.ShouldBe("fr-FR");
        service.Current.DefaultConflictPolicy.ShouldBe(ConflictPolicy.KeepBoth);
        service.Current.SyncIntervalMinutes.ShouldBe(45);
    }

    [Fact]
    public async Task when_save_async_called_after_multiple_changes_then_event_includes_all_changes()
    {
        var service = CreateService();
        bool eventRaised = false;
        AppSettings? changedSettings = null;
        service.SettingsChanged += (s, settings) =>
        {
            eventRaised = true;
            changedSettings = settings;
        };

        service.Current.Theme = AppTheme.Dark;
        service.Current.Locale = "es-ES";
        service.Current.SyncIntervalMinutes = 30;
        await service.SaveAsync();

        eventRaised.ShouldBeTrue();
        _ = changedSettings.ShouldNotBeNull();
        changedSettings.Theme.ShouldBe(AppTheme.Dark);
        changedSettings.Locale.ShouldBe("es-ES");
        changedSettings.SyncIntervalMinutes.ShouldBe(30);
    }
}
