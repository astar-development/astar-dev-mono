using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Accounts.OneDrive;
using AStarDev.CloudSync.Accounts.Sqlite;
using AStarDev.CloudSync.Common;
using AStarDev.CloudSync.Core.Common;
using AStarDev.CloudSync.Core.Persistence;
using AStarDev.CloudSync.DisplaySettings;
using AStarDev.CloudSync.Infrastructure.Common;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.TestHelpers.DisplaySettings;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.Theming;
using AStarDev.CloudSync.ViewModels;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.CloudSync.ViewModels.DisplaySettings;
using AStarDev.CloudSync.ViewModels.Layouts;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

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
    public void when_application_services_are_added_then_the_overview_is_read_from_the_account_store()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<ISyncOverviewSource>().GetType().Assembly.ShouldBe(typeof(OverviewServiceCollectionExtensions).Assembly);
    }

    [Fact]
    public void when_application_services_are_added_then_the_add_account_dialog_and_the_token_cache_consent_are_the_shared_view_models()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<IAddAccountDialog>().ShouldBeSameAs(provider.GetRequiredService<AddAccountViewModel>());
        provider.GetRequiredService<ITokenCacheConsent>().ShouldBeSameAs(provider.GetRequiredService<TokenCacheConsentViewModel>());
        provider.GetRequiredService<AddAccountViewModel>().Consent.ShouldBeSameAs(provider.GetRequiredService<TokenCacheConsentViewModel>());
    }

    [Fact]
    public void when_application_services_are_added_then_the_account_adder_can_be_resolved()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<IAccountAdder>().ShouldBeOfType<MicrosoftAccountAdder>();
    }

    [Fact]
    public void when_application_services_are_added_then_the_avalonia_ui_dispatcher_is_registered()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<IUiDispatcher>().ShouldBeOfType<AvaloniaUiDispatcher>();
    }

    [Fact]
    public void when_application_services_are_added_then_the_main_window_shows_the_shared_add_account_dialog()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<MainWindowViewModel>().AddAccount.ShouldBeSameAs(provider.GetRequiredService<AddAccountViewModel>());
    }

    [Theory]
    [InlineData("AddAccount.Title")]
    [InlineData("AddAccount.SigningIn")]
    [InlineData("AddAccount.Cancel")]
    [InlineData("AddAccount.Close")]
    [InlineData("AddAccount.TryAgain")]
    [InlineData("AddAccount.AlreadyAdded")]
    [InlineData("AddAccount.Failure.BrowserUnavailable")]
    [InlineData("AddAccount.Failure.AuthRejected")]
    [InlineData("AddAccount.Failure.Network")]
    [InlineData("AddAccount.Failure.TokenCacheUnavailable")]
    [InlineData("AddAccount.Failure.DriveUnavailable")]
    [InlineData("AddAccount.Failure.PersistFailed")]
    [InlineData("TokenCacheConsent.Title")]
    [InlineData("TokenCacheConsent.Message")]
    [InlineData("TokenCacheConsent.Agree")]
    [InlineData("TokenCacheConsent.Decline")]
    public void when_application_services_are_added_then_the_add_account_dialog_keys_are_localised(string key)
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<ILocalizationService>().GetLocal(key).ShouldNotBe(key);
    }

    [Fact]
    public void when_application_services_are_added_then_the_placeholder_sync_actions_are_registered()
    {
        var services = CreateServices();

        var descriptor = services.Single(service => service.ServiceType == typeof(ISyncActions));
        descriptor.ImplementationType.ShouldBe(typeof(PlaceholderSyncActions));
    }

    [Fact]
    public void when_application_services_are_added_then_the_user_profile_home_folder_is_registered()
    {
        var services = CreateServices();

        var descriptor = services.Single(service => service.ServiceType == typeof(IHomeFolder));
        descriptor.ImplementationType.ShouldBe(typeof(UserProfileHomeFolder));
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
    public void when_application_services_are_added_then_the_account_store_and_database_come_from_the_sqlite_module()
    {
        using var provider = CreateServices().BuildServiceProvider();
        var sqliteModule = typeof(PersistenceServiceCollectionExtensions).Assembly;

        (provider.GetRequiredService<IAccountStore>(), provider.GetRequiredService<ICloudSyncDatabase>()).ShouldSatisfyAllConditions(
            services => services.Item1.GetType().Assembly.ShouldBe(sqliteModule),
            services => services.Item2.GetType().Assembly.ShouldBe(sqliteModule));
    }

    [Fact]
    public void when_application_services_are_added_then_the_microsoft_account_adder_is_registered()
    {
        var services = CreateServices();

        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IAccountAdder) && descriptor.ImplementationType == typeof(MicrosoftAccountAdder) && descriptor.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void when_application_services_are_added_then_the_msal_client_drive_lookup_and_public_client_application_are_registered()
    {
        using var provider = CreateServices().BuildServiceProvider();

        (provider.GetRequiredService<IMsalClient>(), provider.GetRequiredService<IDriveLookup>(), provider.GetRequiredService<IMsalTokenCacheStorage>(), provider.GetRequiredService<IPublicClientApplication>()).ShouldSatisfyAllConditions(
            services => services.Item1.ShouldBeOfType<MsalClient>(),
            services => services.Item2.ShouldBeOfType<GraphDriveLookup>(),
            services => services.Item3.ShouldBeOfType<MsalCacheHelperStorage>(),
            services => services.Item4.ShouldBeSameAs(provider.GetRequiredService<IPublicClientApplication>()));
    }

    [Fact]
    public void when_application_services_are_added_then_the_public_client_application_uses_the_configured_client_id_and_the_consumers_authority()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<IPublicClientApplication>().AppConfig.ShouldSatisfyAllConditions(
            config => config.ClientId.ShouldBe("3057f494-687d-4abb-a653-4b8066230b6e"),
            config => config.RedirectUri.ShouldBe("http://localhost"),
            config => config.EnablePiiLogging.ShouldBeFalse());
        provider.GetRequiredService<IPublicClientApplication>().Authority.ShouldStartWith("https://login.microsoftonline.com/consumers");
    }

    [Fact]
    public void when_application_services_are_added_then_the_plaintext_cache_consent_store_is_registered()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<IPlaintextCacheConsentStore>().ShouldBeOfType<JsonPlaintextCacheConsentStore>();
    }

    [Fact]
    public void when_application_services_are_added_then_the_plaintext_cache_consent_settings_are_the_token_cache_initialiser()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<IPlaintextCacheConsentSettings>().ShouldBeSameAs(provider.GetRequiredService<TokenCacheInitialiser>());
    }

    [Fact]
    public void when_application_services_are_added_then_the_display_settings_hold_the_plaintext_cache_consent_setting()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<DisplaySettingsViewModel>().PlaintextCacheConsent.ShouldBeSameAs(provider.GetRequiredService<PlaintextCacheConsentSettingsViewModel>());
    }

    [Fact]
    public void when_application_services_are_added_then_the_graph_http_client_uses_the_graph_retry_and_redirect_handlers()
    {
        using var provider = CreateServices().BuildServiceProvider();

        var handlerTypes = HandlerChain(provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(GraphDriveLookup.HttpClientName)).Select(handler => handler.GetType().Name).ToList();

        handlerTypes.ShouldContain("RetryHandler");
        handlerTypes.ShouldContain("RedirectHandler");
    }

    private static IEnumerable<HttpMessageHandler> HandlerChain(HttpMessageHandler handler)
    {
        var current = handler;

        while (current is DelegatingHandler delegating)
        {
            yield return delegating;
            current = delegating.InnerHandler;
        }

        if (current is not null)
            yield return current;
    }

    [Fact]
    public void when_the_shipped_settings_are_loaded_then_the_microsoft_account_section_has_the_client_id_and_graph_scopes()
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false).Build();

        using var provider = new ServiceCollection().AddApplicationServices(configuration).BuildServiceProvider();

        provider.GetRequiredService<IOptions<MicrosoftAccountOptions>>().Value.ShouldSatisfyAllConditions(
            options => options.ClientId.ShouldBe("3057f494-687d-4abb-a653-4b8066230b6e"),
            options => options.Scopes.ShouldBe(["User.Read", "Files.ReadWrite.All"]));
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
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Updates:GithubRepositoryUrl"] = "https://github.com/astar-development/astar-dev-mono", ["Updates:ChannelPrefix"] = "cloudsync", ["MicrosoftAccount:ClientId"] = "3057f494-687d-4abb-a653-4b8066230b6e" })
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
