using AStarDev.CloudSync.Core.DisplaySettings;
using AStarDev.CloudSync.Domain.DisplaySettings;
using AStarDev.CloudSync.Infrastructure.DisplaySettings;
using Microsoft.Extensions.Logging.Abstractions;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Infrastructure.TestsUnit.DisplaySettings;

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

    private static JsonDisplayPreferencesStore CreateStore(MockFileSystem fileSystem) => new(fileSystem.FileInfo.New(SettingsPath), NullLogger<JsonDisplayPreferencesStore>.Instance);
}
