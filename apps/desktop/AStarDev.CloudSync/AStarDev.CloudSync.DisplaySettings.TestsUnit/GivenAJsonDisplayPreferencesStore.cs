using AStarDev.CloudSync.DisplaySettings;
using AStarDev.CloudSync.TestHelpers.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.DisplaySettings.TestsUnit;

public class GivenAJsonDisplayPreferencesStore
{
    private const string SettingsPath = "/home/user/.config/AStarDev.CloudSync/display-preferences.json";

    [Fact]
    public void when_nothing_has_been_saved_then_the_defaults_are_loaded()
    {
        var store = CreateStore(new MockFileSystem());

        var preferences = store.Load();

        preferences.ShouldBe(DisplayPreferencesFactory.CreateDefault());
    }

    [Fact]
    public void when_preferences_are_saved_then_a_new_store_on_the_same_file_loads_them()
    {
        var fileSystem = new MockFileSystem();

        CreateStore(fileSystem).Save(DisplayPreferencesFactory.Create(MainLayoutFactory.CreateSyncPairsFirst(), AppThemeFactory.CreateLight()));
        var preferences = CreateStore(fileSystem).Load();

        preferences.Layout.ShouldBeOfType<MainLayout.SyncPairsFirst>();
        preferences.Theme.ShouldBeOfType<AppTheme.Light>();
    }

    [Fact]
    public void when_preferences_are_saved_then_the_missing_settings_directory_is_created()
    {
        var fileSystem = new MockFileSystem();

        CreateStore(fileSystem).Save(DisplayPreferencesFactory.CreateDefault());

        fileSystem.File.Exists(SettingsPath).ShouldBeTrue();
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"Layout\":\"Q\",\"Theme\":\"Purple\"}")]
    [InlineData("null")]
    public void when_the_saved_file_is_unreadable_or_holds_unknown_values_then_the_defaults_are_loaded(string content)
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/home/user/.config/AStarDev.CloudSync");
        fileSystem.File.WriteAllText(SettingsPath, content);

        var preferences = CreateStore(fileSystem).Load();

        preferences.ShouldBe(DisplayPreferencesFactory.CreateDefault());
    }

    [Fact]
    public void when_the_preferences_cannot_be_saved_then_a_warning_names_the_exception_type_but_not_the_path()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory(SettingsPath);
        var loggerProvider = new RecordingLoggerProvider();
        var logger = loggerProvider.Logger;

        CreateStore(fileSystem, loggerProvider).Save(DisplayPreferencesFactory.CreateDefault());

        string warning = logger.Messages.ShouldHaveSingleItem();
        logger.Entries[0].Level.ShouldBe(LogLevel.Warning);
        logger.ValuesOf("PreferencesNotSaved")["ErrorType"].ShouldBe(typeof(UnauthorizedAccessException).FullName);
        warning.ShouldNotContain("/home/user");
    }

    [Fact]
    public void when_the_saved_file_is_unreadable_then_a_dedicated_warning_carries_the_error_type()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/home/user/.config/AStarDev.CloudSync");
        fileSystem.File.WriteAllText(SettingsPath, "not json");
        var loggerProvider = new RecordingLoggerProvider();
        var logger = loggerProvider.Logger;

        _ = CreateStore(fileSystem, loggerProvider).Load();

        logger.ValuesOf("PreferencesUnreadable")["ErrorType"].ShouldBe(typeof(System.Text.Json.JsonException).FullName);
    }

    private static IDisplayPreferencesStore CreateStore(MockFileSystem fileSystem) => CreateStore(fileSystem, new RecordingLoggerProvider());

    private static IDisplayPreferencesStore CreateStore(MockFileSystem fileSystem, RecordingLoggerProvider loggerProvider) =>
        new ServiceCollection()
            .AddLogging(logging => logging.ClearProviders().AddProvider(loggerProvider))
            .AddDisplayPreferences(_ => fileSystem.FileInfo.New(SettingsPath))
            .BuildServiceProvider()
            .GetRequiredService<IDisplayPreferencesStore>();
}
