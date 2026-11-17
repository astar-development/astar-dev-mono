using AStarDev.CloudSync.ViewModels.Logging;
using AStarDev.CloudSync.Logging;
using AStarDev.CloudSync.ViewModels.Tray;
using AStarDev.CloudSync.Tray;
using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Accounts.OneDrive;
using AStarDev.CloudSync.Accounts.Sqlite;
using AStarDev.CloudSync.Sync.Sqlite;
using AStarDev.CloudSync.Classification;
using AStarDev.CloudSync.Classification.Sqlite;
using AStarDev.CloudSync.Common;
using AStarDev.CloudSync.DisplaySettings;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.DisplaySettings;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.Theming;
using AStarDev.CloudSync.ViewModels;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Classification;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.CloudSync.ViewModels.DisplaySettings;
using AStarDev.CloudSync.ViewModels.Layouts;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using AStarDev.SingleInstance;
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
    public void when_application_services_are_added_then_the_update_scheduler_and_the_deferral_store_resolve()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<IUpdateScheduler>().ShouldBeOfType<UpdateScheduler>();
        provider.GetRequiredService<IUpdateDeferralStore>().ShouldBeOfType<JsonUpdateDeferralStore>();
    }

    [Fact]
    public void when_application_services_are_added_then_the_log_reader_and_the_log_folder_locator_are_registered()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<ILogEntryReader>().ShouldBeOfType<LogEntryReader>();
        provider.GetRequiredService<ILogFileLocator>().ShouldBeOfType<CloudSyncLogFileLocator>();
        provider.GetRequiredService<LogViewerLauncher>().ShouldNotBeNull();
        provider.GetRequiredService<UnhandledExceptionLogger>().ShouldNotBeNull();
        provider.GetRequiredService<ILogDiagnosticsClipboard>().ShouldBeOfType<AvaloniaLogDiagnosticsClipboard>();
    }

    [Fact]
    public void when_application_services_are_added_then_the_tray_close_notice_and_its_marker_file_store_are_registered()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<ITrayNoticeStore>().ShouldBeOfType<FileTrayNoticeStore>();
        provider.GetRequiredService<TrayCloseNotice>().ShouldNotBeNull();
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
        localization.GetLocal("App.WindowTitle", "1.71.1").ShouldBe("AStar Dev Cloud Sync V1.71.1");
    }

    [Fact]
    public void when_application_services_are_added_then_the_running_version_is_registered_without_a_v_prefix_or_build_metadata()
    {
        using var provider = CreateServices().BuildServiceProvider();

        var version = provider.GetRequiredService<AppVersion>();

        version.Text.ShouldNotBeNullOrWhiteSpace();
        version.Text.ShouldNotStartWith("v", Case.Insensitive);
        version.Text.ShouldNotContain("+");
    }

    [Theory]
    [InlineData("Update.Title")]
    [InlineData("Update.Message")]
    [InlineData("Update.ForcedMessage")]
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
    public void when_application_services_are_added_then_the_account_dialog_is_the_account_commands_and_the_reloader_resolves()
    {
        using var provider = CreateServices().BuildServiceProvider();

        (provider.GetRequiredService<IAccountCommands>() == provider.GetRequiredService<AccountDialogViewModel>(), provider.GetRequiredService<MainWindowViewModel>().AccountDialog == provider.GetRequiredService<AccountDialogViewModel>(), provider.GetRequiredService<SignInStateOverviewReloader>() is not null).ShouldBe((true, true, true));
    }

    [Fact]
    public void when_application_services_are_added_then_the_folder_selection_dialog_is_the_shared_view_model()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<IFolderSelectionDialog>().ShouldBeSameAs(provider.GetRequiredService<AStarDev.CloudSync.ViewModels.FolderSelection.FolderSelectionViewModel>());
    }

    [Fact]
    public void when_application_services_are_added_then_the_account_actions_and_startup_sign_in_check_resolve()
    {
        using var provider = CreateServices().BuildServiceProvider();

        (provider.GetRequiredService<IAccountActions>().GetType().Assembly, provider.GetRequiredService<ISignInStateRefresher>().GetType().Assembly).ShouldBe((typeof(SyncServiceCollectionExtensions).Assembly, typeof(SyncServiceCollectionExtensions).Assembly));
    }

    [Fact]
    public void when_application_services_are_added_then_the_sync_pass_runner_resolves_and_shares_one_activity_registry()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<ISyncPassRunner>().ShouldNotBeNull();
        provider.GetRequiredService<ISyncActivitySource>().ShouldBeSameAs(provider.GetRequiredService<ISyncCancelAction>());
    }

    [Fact]
    public void when_application_services_are_added_then_the_conflict_resolution_services_resolve_as_singletons()
    {
        using var provider = CreateServices().BuildServiceProvider();

        (provider.GetRequiredService<IConflictResolver>() == provider.GetRequiredService<IConflictResolver>(), provider.GetRequiredService<IHeldDeletionDecider>() == provider.GetRequiredService<IHeldDeletionDecider>(), provider.GetRequiredService<IConflictResolutionPlanner>().GetType(), provider.GetRequiredService<IKeepBothNamer>().GetType()).ShouldBe((true, true, typeof(ConflictResolutionPlanner), typeof(KeepBothNamer)));
    }

    [Fact]
    public void when_application_services_are_added_then_one_sync_lock_and_one_throttle_gate_are_shared()
    {
        using var provider = CreateServices().BuildServiceProvider();

        (provider.GetRequiredService<IAccountSyncLock>() == provider.GetRequiredService<IAccountSyncLock>(), provider.GetRequiredService<IAccountThrottleGate>() == provider.GetRequiredService<IAccountThrottleGate>(), provider.GetRequiredService<IAccountSyncLock>().GetType().Assembly).ShouldBe((true, true, typeof(SyncServiceCollectionExtensions).Assembly));
    }

    [Fact]
    public void when_application_services_are_added_then_the_single_instance_guard_uses_the_cloudsync_lock_in_the_data_folder()
    {
        using var provider = CreateServices().BuildServiceProvider();

        (provider.GetRequiredService<ISingleInstanceGuard>().GetType().Assembly, provider.GetRequiredService<SingleInstanceOptions>().Name).ShouldBe((typeof(SingleInstanceServiceCollectionExtensions).Assembly, "cloudsync"));
    }

    [Fact]
    public void when_application_services_are_added_then_the_onedrive_access_token_source_resolves_against_the_real_account_store()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<IAccessTokenSource>().GetType().Assembly.ShouldBe(typeof(OneDriveServiceCollectionExtensions).Assembly);
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
    public void when_application_services_are_added_then_the_onedrive_account_adder_is_resolved_by_provider()
    {
        using var provider = CreateServices().BuildServiceProvider();

        var adder = provider.GetRequiredService<IProviderCapability<IAccountAdder>>().ForProvider(CloudProviderFactory.CreateOneDrive());

        adder.Match(found => found, _ => throw new InvalidOperationException()).ShouldBeOfType<InstrumentedAccountAdder>();
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
    public void when_application_services_are_added_then_the_conflict_list_is_resolved_and_is_the_one_in_the_main_window()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<AStarDev.CloudSync.ViewModels.Conflicts.ConflictListViewModel>().ShouldBeSameAs(provider.GetRequiredService<AStarDev.CloudSync.ViewModels.MainWindowViewModel>().ConflictList);
    }

    [Theory]
    [InlineData("Conflict.Title")]
    [InlineData("Conflict.Review")]
    [InlineData("Conflict.Close")]
    [InlineData("Conflict.Refresh")]
    [InlineData("Conflict.FilterLabel")]
    [InlineData("Conflict.Filter.All")]
    [InlineData("Conflict.Kind.BothModified")]
    [InlineData("Conflict.Kind.LocalModifiedRemoteDeleted")]
    [InlineData("Conflict.Kind.LocalDeletedRemoteModified")]
    [InlineData("Conflict.Kind.BothAdded")]
    [InlineData("Conflict.Kind.MoveTargetTaken")]
    [InlineData("Conflict.Kind.RemoteChangedDuringTransfer")]
    [InlineData("Conflict.Skip")]
    [InlineData("Conflict.Skipped")]
    [InlineData("Conflict.Empty")]
    [InlineData("Conflict.LoadFailed")]
    [InlineData("Conflict.Detected")]
    [InlineData("Conflict.Group.One")]
    [InlineData("Conflict.Group.Many")]
    [InlineData("Conflict.Action.AutomationName")]
    [InlineData("Conflict.Unavailable.LocalSideAbsent")]
    [InlineData("Conflict.Unavailable.RemoteSideAbsent")]
    [InlineData("Conflict.Unavailable.NotAConflict")]
    [InlineData("Conflict.Refused.Busy")]
    [InlineData("Conflict.Refused.Draining")]
    [InlineData("Conflict.Refused.QueueUnreadable")]
    [InlineData("Conflict.Select.AutomationName")]
    [InlineData("Conflict.SelectAll")]
    [InlineData("Conflict.ClearSelection")]
    [InlineData("Conflict.Selected")]
    [InlineData("Conflict.Bulk.Label")]
    [InlineData("Conflict.Bulk.AutomationName")]
    [InlineData("Conflict.Default.Text")]
    [InlineData("Held.Title")]
    [InlineData("Held.Header")]
    [InlineData("Held.DeleteLocal.One")]
    [InlineData("Held.DeleteLocal.Many")]
    [InlineData("Held.DeleteRemote.One")]
    [InlineData("Held.DeleteRemote.Many")]
    [InlineData("Held.Confirm")]
    [InlineData("Held.Restore")]
    [InlineData("Held.Later")]
    [InlineData("Held.Show")]
    [InlineData("Held.Hide")]
    [InlineData("Held.Deferred")]
    [InlineData("Held.Error.NotSettled")]
    [InlineData("Held.Refused.Busy")]
    [InlineData("Held.Refused.Draining")]
    [InlineData("Held.Refused.QueueUnreadable")]
    [InlineData("Held.Refused.NoLongerHeld")]
    [InlineData("Held.PairUnknown")]
    [InlineData("Notification.Synced.One")]
    [InlineData("Notification.Synced.Many")]
    [InlineData("Notification.Conflicts.One")]
    [InlineData("Notification.Conflicts.Many")]
    [InlineData("Notification.Held.One")]
    [InlineData("Notification.Held.Many")]
    [InlineData("Notification.SignInRequired")]
    [InlineData("Notification.Failed")]
    [InlineData("AccountDialog.Transfers.Current")]
    [InlineData("AccountDialog.Transfers.Help")]
    [InlineData("AccountDialog.Transfers.Fewer")]
    [InlineData("AccountDialog.Transfers.More")]
    [InlineData("AccountDialog.Transfers.SaveFailed")]
    [InlineData("Settings.Autostart")]
    [InlineData("Settings.Autostart.Toggle")]
    [InlineData("Settings.Autostart.NotInstalled")]
    [InlineData("Settings.Autostart.PlatformNotSupported")]
    [InlineData("Settings.Autostart.LoadFailed")]
    [InlineData("Settings.Autostart.ChangeFailed")]
    [InlineData("Tray.Open")]
    [InlineData("Tray.SyncNow")]
    [InlineData("Tray.Quit")]
    [InlineData("Tray.Tooltip")]
    [InlineData("Conflict.Error.NotAllowed")]
    [InlineData("Conflict.Error.NotOpen")]
    [InlineData("Conflict.Error.Unsupported")]
    [InlineData("Conflict.Error.LocalFileMissing")]
    [InlineData("Conflict.Error.RemoteFileMissing")]
    [InlineData("Conflict.Error.LocalChanged")]
    [InlineData("Conflict.Error.RemoteChanged")]
    [InlineData("Conflict.Error.Transfer")]
    [InlineData("Conflict.Error.Local")]
    [InlineData("Conflict.Error.NotRecorded")]
    [InlineData("Conflict.Error.Unexpected")]
    public void when_application_services_are_added_then_the_conflict_list_keys_are_localised(string key)
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<ILocalizationService>().GetLocal(key).ShouldNotBe(key);
    }

    [Fact]
    public void when_application_services_are_added_then_the_sync_restart_question_and_the_sync_now_action_are_resolved_and_the_dialog_is_the_one_in_the_main_window()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<IConfirmSyncRestartDialog>().ShouldBeSameAs(provider.GetRequiredService<AStarDev.CloudSync.ViewModels.SyncRestart.ConfirmSyncRestartViewModel>());
        provider.GetRequiredService<AStarDev.CloudSync.ViewModels.MainWindowViewModel>().SyncRestart.ShouldBeSameAs(provider.GetRequiredService<AStarDev.CloudSync.ViewModels.SyncRestart.ConfirmSyncRestartViewModel>());
        provider.GetRequiredService<ISyncNowAction>().ShouldNotBeNull();
        provider.GetRequiredService<AStarDev.CloudSync.ViewModels.Overview.SyncRunOverviewReloader>().ShouldNotBeNull();
        provider.GetRequiredService<ISyncActions>().ShouldNotBeNull();
    }

    [Fact]
    public void when_application_services_are_added_then_the_scheduler_and_the_interval_setting_resolve_and_the_setting_is_the_one_in_the_display_settings()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<ISyncScheduler>().ShouldBeSameAs(provider.GetRequiredService<ISyncScheduler>());
        provider.GetRequiredService<AStarDev.CloudSync.ViewModels.DisplaySettings.DisplaySettingsViewModel>().Schedule.ShouldBeSameAs(provider.GetRequiredService<AStarDev.CloudSync.ViewModels.DisplaySettings.SyncScheduleSettingsViewModel>());
        provider.GetRequiredService<ISyncScheduleSettings>().ShouldNotBeNull();
    }

    [Fact]
    public void when_application_services_are_added_then_the_file_audit_resolves_and_is_the_one_in_the_display_settings()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<IFileAuditor>().ShouldNotBeNull();
        provider.GetRequiredService<AStarDev.CloudSync.ViewModels.DisplaySettings.DisplaySettingsViewModel>().FileAudit.ShouldBeSameAs(provider.GetRequiredService<AStarDev.CloudSync.ViewModels.DisplaySettings.FileAuditViewModel>());
        provider.GetRequiredService<AStarDev.CloudSync.ViewModels.DisplaySettings.FileAuditViewModel>().HeadingText.ShouldBe("File audit");
    }

    [Fact]
    public void when_application_services_are_added_then_the_trash_retention_setting_resolves_and_is_the_one_in_the_display_settings()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<AStarDev.CloudSync.ViewModels.DisplaySettings.DisplaySettingsViewModel>().TrashRetention.ShouldBeSameAs(provider.GetRequiredService<AStarDev.CloudSync.ViewModels.DisplaySettings.TrashRetentionSettingsViewModel>());
        provider.GetRequiredService<ITrashRetentionSettings>().ShouldBeSameAs(provider.GetRequiredService<ITrashRetentionSettings>());
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
    public void when_application_services_are_added_then_the_folder_picker_is_the_avalonia_one()
    {
        var services = CreateServices();

        var descriptor = services.Single(service => service.ServiceType == typeof(AStarDev.CloudSync.ViewModels.Common.IFolderPicker));
        descriptor.ImplementationType.ShouldBe(typeof(AvaloniaFolderPicker));
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
    public void when_application_services_are_added_then_the_account_store_comes_from_the_accounts_sqlite_module_and_the_combined_database_runs_one_migration_at_a_time()
    {
        using var provider = CreateServices().BuildServiceProvider();
        var sqliteModule = typeof(PersistenceServiceCollectionExtensions).Assembly;

        (provider.GetRequiredService<IAccountStore>(), provider.GetRequiredService<ICloudSyncDatabase>()).ShouldSatisfyAllConditions(
            services => services.Item1.GetType().Assembly.ShouldBe(sqliteModule),
            services => services.Item2.ShouldBeOfType<SerialCloudSyncDatabase>());
    }

    [Fact]
    public void when_application_services_are_added_then_the_microsoft_account_adder_is_registered_keyed_by_onedrive()
    {
        var services = CreateServices();

        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IAccountAdder) && descriptor.IsKeyedService && Equals(descriptor.ServiceKey, CloudProviderFactory.CreateOneDrive()) && descriptor.KeyedImplementationFactory != null && descriptor.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void when_application_services_are_added_then_the_msal_client_token_cache_storage_and_public_client_application_are_registered()
    {
        using var provider = CreateServices().BuildServiceProvider();
        var oneDriveModule = typeof(OneDriveServiceCollectionExtensions).Assembly;

        (provider.GetRequiredService<IMsalClient>(), provider.GetRequiredService<IMsalTokenCacheStorage>(), provider.GetRequiredService<IPublicClientApplication>()).ShouldSatisfyAllConditions(
            services => services.Item1.GetType().Assembly.ShouldBe(oneDriveModule),
            services => services.Item2.GetType().Assembly.ShouldBe(oneDriveModule),
            services => services.Item3.ShouldBeSameAs(provider.GetRequiredService<IPublicClientApplication>()));
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
    public void when_application_services_are_added_then_one_plaintext_cache_consent_setting_comes_from_the_onedrive_module()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<IPlaintextCacheConsentSettings>().ShouldSatisfyAllConditions(
            settings => settings.GetType().Assembly.ShouldBe(typeof(OneDriveServiceCollectionExtensions).Assembly),
            settings => settings.ShouldBeSameAs(provider.GetRequiredService<IPlaintextCacheConsentSettings>()));
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

        var handlerTypes = HandlerChain(provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler("MicrosoftGraph")).Select(handler => handler.GetType().Name).ToList();

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

    [Fact]
    public void when_application_services_are_added_then_the_baseline_writer_is_the_classifying_decorator_over_the_sqlite_store()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<ISyncBaselineWriter>().ShouldBeOfType<ClassifyingSyncBaselineWriter>();
        provider.GetRequiredService<ISyncBaselineReader>().ShouldNotBeOfType<ClassifyingSyncBaselineWriter>();
    }

    [Fact]
    public void when_application_services_are_added_then_the_upload_recorder_is_the_classifying_decorator()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<IUploadRecorder>().ShouldBeOfType<ClassifyingUploadRecorder>();
    }

    [Fact]
    public void when_application_services_are_added_then_the_upload_executor_receives_the_classifying_recorder()
    {
        var services = CreateServices();
        using var provider = services.BuildServiceProvider();
        var executorType = services.Single(descriptor => descriptor.ServiceType.Name == "UploadItemExecutor").ServiceType;

        var executor = provider.GetRequiredService(executorType);

        var recorders = executorType.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Where(field => field.FieldType == typeof(IUploadRecorder)).Select(field => field.GetValue(executor)).ToList();
        recorders.ShouldHaveSingleItem().ShouldBeOfType<ClassifyingUploadRecorder>();
    }

    [Theory]
    [InlineData("UploadItemExecutor")]
    [InlineData("DownloadExecutor")]
    [InlineData("LocalChangeDetector")]
    [InlineData("ResolutionTransfers")]
    public void when_application_services_are_added_then_each_sync_consumer_receives_the_classifying_baseline_writer(string consumerName)
    {
        var services = CreateServices();
        using var provider = services.BuildServiceProvider();
        var registered = services.First(descriptor => descriptor.ServiceType.Name == consumerName || descriptor.ImplementationType?.Name == consumerName).ServiceType;

        var consumer = provider.GetRequiredService(registered);

        var writers = consumer.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Where(field => field.FieldType == typeof(ISyncBaselineWriter)).Select(field => field.GetValue(consumer)).ToList();
        writers.ShouldNotBeEmpty();
        writers.ShouldAllBe(writer => writer is ClassifyingSyncBaselineWriter);
    }

    [Fact]
    public void when_application_services_are_added_then_the_stores_are_the_caching_decorators_and_the_provider_resolves_without_a_cycle()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<ITermStore>().ShouldBeOfType<CachingTermStore>();
        provider.GetRequiredService<ICategoryStore>().ShouldBeOfType<CachingCategoryStore>();
        provider.GetRequiredService<IClassificationVocabularyProvider>().ShouldBeOfType<ClassificationVocabularyProvider>();
    }

    [Fact]
    public void when_application_services_are_added_then_the_engine_signals_and_seeding_services_resolve()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<IFileClassifier>().ShouldBeOfType<FileClassifier>();
        provider.GetServices<IClassificationSignal>().Select(signal => signal.GetType()).ShouldBe([typeof(ExtensionSignal), typeof(FolderSegmentSignal), typeof(DictionaryTermSignal)], ignoreOrder: true);
        provider.GetRequiredService<IClassificationSeedSource>().ShouldBeOfType<EmbeddedClassificationSeedSource>();
        provider.GetRequiredService<IClassificationSeeder>().ShouldBeOfType<ClassificationSeeder>();
        provider.GetRequiredService<IClassificationStartup>().ShouldBeOfType<ClassificationStartup>();
        provider.GetRequiredService<SyncStartup>().ShouldBeSameAs(provider.GetRequiredService<SyncStartup>());
        provider.GetRequiredService<IFileClassificationStore>().ShouldNotBeNull();
        provider.GetRequiredService<ISeedStateStore>().ShouldNotBeNull();
        provider.GetRequiredService<IReclassifyRunStore>().ShouldNotBeNull();
        provider.GetRequiredService<IReclassifyFreshnessCheck>().ShouldBeOfType<ReclassifyFreshnessCheck>();
        provider.GetRequiredService<IClassifiableFileSource>().ShouldNotBeNull();
    }

    [Fact]
    public void when_application_services_are_added_then_the_word_lists_are_one_instance_and_the_suggestions_and_reclassifier_are_singletons()
    {
        using var provider = CreateServices().BuildServiceProvider();

        provider.GetRequiredService<IKnownWordList>().ShouldBeSameAs(provider.GetRequiredService<IFirstNameList>());
        provider.GetRequiredService<IClassificationSuggestions>().ShouldBeSameAs(provider.GetRequiredService<IClassificationSuggestions>());
        provider.GetRequiredService<IClassificationReclassifier>().ShouldBeSameAs(provider.GetRequiredService<IClassificationReclassifier>());
        provider.GetRequiredService<IClassificationReclassifier>().ShouldBeOfType<RunRecordingReclassifier>();
        provider.GetRequiredService<IReclassifyRunRecordedSource>().ShouldBeSameAs(provider.GetRequiredService<IClassificationReclassifier>());
        provider.GetRequiredService<IReclassifyProgressSource>().ShouldBeSameAs(provider.GetRequiredService<IReclassifyProgressSource>());
        provider.GetRequiredService<IReclassifyProgressSource>().ShouldBeSameAs(provider.GetRequiredService<ReclassifyProgressSource>());
    }

    [Fact]
    public void when_application_services_are_added_then_the_classification_page_and_the_layout_share_one_view_model()
    {
        using var provider = CreateServices().BuildServiceProvider();

        var page = provider.GetRequiredService<ClassificationPageViewModel>();

        provider.GetRequiredService<LayoutAViewModel>().Classification.ShouldBeSameAs(page);
        page.Tabs.ShouldBeSameAs(provider.GetRequiredService<ClassificationTabsViewModel>());
        page.Reclassify.ShouldBeSameAs(provider.GetRequiredService<ReclassifyViewModel>());
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
