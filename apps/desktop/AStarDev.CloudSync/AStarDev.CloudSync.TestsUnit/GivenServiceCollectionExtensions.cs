using AStarDev.CloudSync.Core;
using AStarDev.CloudSync.Core.Placeholder;
using AStarDev.CloudSync.ViewModels;
using AStarDev.Localization;
using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.TestsUnit;

public class GivenServiceCollectionExtensions
{
    [Fact]
    public void when_application_services_are_added_then_the_main_window_view_model_is_registered()
    {
        var services = CreateServices();

        services.ShouldContain(service => service.ServiceType == typeof(MainWindowViewModel));
    }

    [Fact]
    public void when_application_services_are_added_then_the_velopack_update_service_is_registered()
    {
        var services = CreateServices();

        services.ShouldContain(service => service.ServiceType == typeof(IVelopackUpdateService));
    }

    [Fact]
    public void when_application_services_are_added_then_the_update_notification_service_is_registered()
    {
        var services = CreateServices();

        services.ShouldContain(service => service.ServiceType == typeof(IUpdateNotificationService));
    }

    [Fact]
    public void when_application_services_are_added_then_the_localized_update_dialog_text_provider_is_registered()
    {
        var services = CreateServices();

        var descriptor = services.Single(service => service.ServiceType == typeof(IUpdateDialogTextProvider));
        descriptor.ImplementationType.ShouldBe(typeof(LocalizedUpdateDialogTextProvider));
    }

    [Fact]
    public void when_application_services_are_added_then_the_localisation_service_reads_the_cloudsync_strings()
    {
        using var provider = CreateServices().BuildServiceProvider();

        var localization = provider.GetRequiredService<ILocalizationService>();

        localization.GetLocal("App.Title").ShouldBe("AStar Dev CloudSync");
    }

    [Theory]
    [InlineData("Update.Title")]
    [InlineData("Update.Message")]
    [InlineData("Update.ReleaseNotesLabel")]
    [InlineData("Update.RestartNow")]
    [InlineData("Update.Later")]
    [InlineData("Update.Downloading")]
    public void when_application_services_are_added_then_the_update_dialog_keys_are_localised(string key)
    {
        using var provider = CreateServices().BuildServiceProvider();

        var localization = provider.GetRequiredService<ILocalizationService>();

        localization.GetLocal(key).ShouldNotBe(key);
    }

    [Fact]
    public void when_application_services_are_added_then_the_placeholder_sync_overview_source_is_registered()
    {
        var services = CreateServices();

        var descriptor = services.Single(service => service.ServiceType == typeof(ISyncOverviewSource));
        descriptor.ImplementationType.ShouldBe(typeof(PlaceholderSyncOverviewSource));
    }

    [Fact]
    public void when_application_services_are_added_then_the_sync_overview_view_model_resolves()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<SyncOverviewViewModel>().ShouldNotBeNull();
    }

    [Fact]
    public void when_application_services_are_added_then_the_layout_b_view_model_resolves_over_the_shared_overview()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<LayoutBViewModel>().Overview.ShouldBeSameAs(provider.GetRequiredService<SyncOverviewViewModel>());
    }

    [Fact]
    public void when_application_services_are_added_then_the_layout_c_view_model_resolves_over_the_shared_overview()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<LayoutCViewModel>().Overview.ShouldBeSameAs(provider.GetRequiredService<SyncOverviewViewModel>());
    }

    [Fact]
    public void when_application_services_are_added_then_the_main_window_view_model_holds_the_layout_a_view_model_for_the_shared_overview()
    {
        using var provider = CreateServices().BuildServiceProvider();

        var mainWindow = provider.GetRequiredService<MainWindowViewModel>();

        mainWindow.LayoutA.ShouldBeSameAs(provider.GetRequiredService<LayoutAViewModel>());
        mainWindow.LayoutA.Overview.ShouldBeSameAs(provider.GetRequiredService<SyncOverviewViewModel>());
    }

    [Fact]
    public void when_application_services_are_added_then_the_system_time_provider_is_registered()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(TimeProvider.System);
    }

    private static IServiceCollection CreateServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Updates:GithubRepositoryUrl"] = "https://github.com/astar-development/astar-dev-mono", ["Updates:ChannelPrefix"] = "cloudsync" })
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddApplicationServices(configuration)
            .AddSingleton<IDisplayPreferencesStore>(new InMemoryDisplayPreferencesStore())
            .AddSingleton<IThemeApplier>(new RecordingThemeApplier());
    }

    [Fact]
    public void when_application_services_are_added_then_the_display_preferences_store_and_theme_applier_are_registered()
    {
        var services = new ServiceCollection().AddApplicationServices(new ConfigurationBuilder().Build());

        services.ShouldContain(service => service.ServiceType == typeof(IDisplayPreferencesStore));
        services.ShouldContain(service => service.ServiceType == typeof(IThemeApplier) && service.ImplementationType == typeof(AStarDev.CloudSync.Theming.AvaloniaThemeApplier));
    }

    [Fact]
    public void when_application_services_are_added_then_the_main_window_view_model_shares_the_display_settings_and_layouts()
    {
        using var provider = CreateServices().BuildServiceProvider();

        var mainWindow = provider.GetRequiredService<MainWindowViewModel>();

        mainWindow.DisplaySettings.ShouldBeSameAs(provider.GetRequiredService<DisplaySettingsViewModel>());
        mainWindow.LayoutB.ShouldBeSameAs(provider.GetRequiredService<LayoutBViewModel>());
        mainWindow.LayoutC.ShouldBeSameAs(provider.GetRequiredService<LayoutCViewModel>());
    }
}
