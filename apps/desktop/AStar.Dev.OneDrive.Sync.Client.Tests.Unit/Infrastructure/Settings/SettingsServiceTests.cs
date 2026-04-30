using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Tests.Unit.TestHelpers;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Settings;

[Collection("SettingsService")]
public sealed class SettingsServiceTests : IDisposable
{
    private readonly TemporaryDirectory tempDir = new();

    public void Dispose() => tempDir.Dispose();

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultSettings()
    {
        var service = new SettingsService(tempDir.SettingsFilePath);

        _ = service.Current.ShouldNotBeNull();
        service.Current.Theme.ShouldBe(AppTheme.System);
        service.Current.Locale.ShouldBe("en-GB");
        service.Current.DefaultConflictPolicy.ShouldBe(ConflictPolicy.Ignore);
        service.Current.SyncIntervalMinutes.ShouldBe(60);
    }

    [Fact]
    public void Current_ShouldReturnAppSettings()
    {
        var service = new SettingsService(tempDir.SettingsFilePath);

        var settings = service.Current;

        _ = settings.ShouldNotBeNull();
        _ = settings.ShouldBeOfType<AppSettings>();
    }

    [Fact]
    public void Theme_ShouldBeSettable()
    {
        var service = new SettingsService(tempDir.SettingsFilePath);

        service.Current.Theme = AppTheme.Dark;

        service.Current.Theme.ShouldBe(AppTheme.Dark);
    }

    [Fact]
    public void Locale_ShouldBeSettable()
    {
        var service = new SettingsService(tempDir.SettingsFilePath);

        service.Current.Locale = "fr-FR";

        service.Current.Locale.ShouldBe("fr-FR");
    }

    [Fact]
    public void DefaultConflictPolicy_ShouldBeSettable()
    {
        var service = new SettingsService(tempDir.SettingsFilePath);

        service.Current.DefaultConflictPolicy = ConflictPolicy.LastWriteWins;

        service.Current.DefaultConflictPolicy.ShouldBe(ConflictPolicy.LastWriteWins);
    }

    [Fact]
    public void SyncIntervalMinutes_ShouldBeSettable()
    {
        var service = new SettingsService(tempDir.SettingsFilePath);

        service.Current.SyncIntervalMinutes = 30;

        service.Current.SyncIntervalMinutes.ShouldBe(30);
    }

    [Fact]
    public async Task SaveAsync_ShouldInvokeSettingsChangedEvent()
    {
        var service = new SettingsService(tempDir.SettingsFilePath);
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
    public async Task SaveAsync_ShouldPersistSettings()
    {
        var service = new SettingsService(tempDir.SettingsFilePath);
        service.Current.Locale = "de-DE";
        service.Current.SyncIntervalMinutes = 45;

        await service.SaveAsync();

        service.Current.Locale.ShouldBe("de-DE");
        service.Current.SyncIntervalMinutes.ShouldBe(45);
    }

    [Fact]
    public async Task LoadAsync_ShouldNotThrow()
    {
        var service = new SettingsService(tempDir.SettingsFilePath);

        await Should.NotThrowAsync(() => service.LoadAsync());
    }

    [Fact]
    public async Task LoadAsync_ShouldInitializeCurrentSettings()
    {
        var service = new SettingsService(tempDir.SettingsFilePath);

        await service.LoadAsync();

        _ = service.Current.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(AppTheme.System)]
    [InlineData(AppTheme.Light)]
    [InlineData(AppTheme.Dark)]
    public void Theme_ShouldSupportAllThemeValues(AppTheme theme)
    {
        var service = new SettingsService(tempDir.SettingsFilePath);

        service.Current.Theme = theme;

        service.Current.Theme.ShouldBe(theme);
    }

    [Theory]
    [InlineData("en-GB")]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    [InlineData("es-ES")]
    public void Locale_ShouldSupportDifferentCultures(string locale)
    {
        var service = new SettingsService(tempDir.SettingsFilePath);

        service.Current.Locale = locale;

        service.Current.Locale.ShouldBe(locale);
    }

    [Theory]
    [InlineData(ConflictPolicy.Ignore)]
    [InlineData(ConflictPolicy.KeepBoth)]
    [InlineData(ConflictPolicy.LastWriteWins)]
    [InlineData(ConflictPolicy.LocalWins)]
    public void DefaultConflictPolicy_ShouldSupportAllPolicies(ConflictPolicy policy)
    {
        var service = new SettingsService(tempDir.SettingsFilePath);

        service.Current.DefaultConflictPolicy = policy;

        service.Current.DefaultConflictPolicy.ShouldBe(policy);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    public void SyncIntervalMinutes_ShouldSupportDifferentIntervals(int minutes)
    {
        var service = new SettingsService(tempDir.SettingsFilePath);

        service.Current.SyncIntervalMinutes = minutes;

        service.Current.SyncIntervalMinutes.ShouldBe(minutes);
    }

    [Fact]
    public void MultipleSettingsChanges_ShouldMaintainState()
    {
        var service = new SettingsService(tempDir.SettingsFilePath);

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
    public async Task SaveAsync_WithMultipleChanges_ShouldEventIncludeAllChanges()
    {
        var service = new SettingsService(tempDir.SettingsFilePath);
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
