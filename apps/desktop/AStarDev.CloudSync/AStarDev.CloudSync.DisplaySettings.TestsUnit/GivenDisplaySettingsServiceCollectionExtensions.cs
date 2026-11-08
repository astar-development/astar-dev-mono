using AStarDev.CloudSync.DisplaySettings;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.DisplaySettings.TestsUnit;

public class GivenDisplaySettingsServiceCollectionExtensions
{
    [Fact]
    public void when_display_preferences_are_added_then_one_store_is_shared_by_every_caller()
    {
        var fileSystem = new MockFileSystem();
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddDisplayPreferences(_ => fileSystem.FileInfo.New("/home/user/.config/AStarDev.CloudSync/display-preferences.json"))
            .BuildServiceProvider();

        provider.GetRequiredService<IDisplayPreferencesStore>().ShouldBeSameAs(provider.GetRequiredService<IDisplayPreferencesStore>());
    }

    [Fact]
    public void when_display_preferences_are_added_then_the_settings_file_is_resolved_from_the_service_provider()
    {
        var fileSystem = new MockFileSystem();
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(fileSystem)
            .AddDisplayPreferences(services => services.GetRequiredService<MockFileSystem>().FileInfo.New("/home/user/.config/AStarDev.CloudSync/display-preferences.json"))
            .BuildServiceProvider();

        provider.GetRequiredService<IDisplayPreferencesStore>().Save(DisplayPreferencesFactory.CreateDefault());

        fileSystem.File.Exists("/home/user/.config/AStarDev.CloudSync/display-preferences.json").ShouldBeTrue();
    }
}
