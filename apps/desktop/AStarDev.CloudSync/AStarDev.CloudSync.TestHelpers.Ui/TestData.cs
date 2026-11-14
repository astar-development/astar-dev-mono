using System.Globalization;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Conflicts;
using AStarDev.CloudSync.TestHelpers.DisplaySettings;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.ViewModels;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.CloudSync.ViewModels.Conflicts;
using AStarDev.CloudSync.ViewModels.DisplaySettings;
using AStarDev.CloudSync.ViewModels.FolderSelection;
using AStarDev.CloudSync.ViewModels.Layouts;
using AStarDev.CloudSync.ViewModels.SyncRestart;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Testably.Abstractions;
using Xunit;

namespace AStarDev.CloudSync.TestHelpers.Ui;

public static class TestData
{
    public static readonly DateTimeOffset Now = TestClock.Now;

    public static ILocalizationService CreateLocalization()
    {
        var localization = new LocalizationService(typeof(App).Assembly);
        localization.Initialise(new CultureInfo("en-GB"));

        return localization;
    }

    public static FakeTimeProvider CreateTime() => TestClock.CreateTime();

    public static LocalPathFormatter CreateLocalPathFormatter() => new(new FixedHomeFolder(Option.Some(new RealFileSystem().DirectoryInfo.New("/home/user"))));

    public static Task<SyncOverview> CreateOverviewAsync() => new PlaceholderSyncOverviewSource(CreateTime()).GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(overview => overview, error => throw new InvalidOperationException(error.Message));

    public static async Task<SyncOverviewViewModel> CreateLoadedOverviewViewModelAsync()
    {
        var viewModel = new SyncOverviewViewModel(new PlaceholderSyncOverviewSource(CreateTime()), CreateLocalization(), CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        return viewModel;
    }

    public static CloudAccount CreateAccount(SyncStatus status, Option<DateTimeOffset> lastSync) => CloudAccountFactory.Create(CloudAccountIdentityFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), "Work", "work@example.com", CloudProviderFactory.CreateOneDrive()), StorageUsageFactory.Create(782_643_000_000, 1_099_511_627_776), 30, 240_686, status, lastSync);

    public static DisplaySettingsViewModel CreateDisplaySettings() => CreateDisplaySettings(new FakePlaintextCacheConsentSettings());

    public static DisplaySettingsViewModel CreateDisplaySettings(FakePlaintextCacheConsentSettings plaintextCacheConsent) => new(new InMemoryDisplayPreferencesStore(), new RecordingThemeApplier(), CreatePlaintextCacheConsentSettings(plaintextCacheConsent), CreateSyncScheduleSettings(), CreateTrashRetentionSettings(), CreateLocalization());

    public static TrashRetentionSettingsViewModel CreateTrashRetentionSettings() => new(new FakeTrashRetentionSettings(), CreateLocalization());

    public static SyncScheduleSettingsViewModel CreateSyncScheduleSettings() => new(new FakeSyncScheduleSettings(), CreateLocalization());

    public static PlaintextCacheConsentSettingsViewModel CreatePlaintextCacheConsentSettings() => CreatePlaintextCacheConsentSettings(new FakePlaintextCacheConsentSettings());

    public static PlaintextCacheConsentSettingsViewModel CreatePlaintextCacheConsentSettings(FakePlaintextCacheConsentSettings settings) => new(settings, CreateLocalization());

    public static AddAccountViewModel CreateAddAccount(IAccountAdder accountAdder) => CreateAddAccount(accountAdder, new RecordingFolderSelectionDialog());

    public static AddAccountViewModel CreateAddAccount(IAccountAdder accountAdder, IFolderSelectionDialog folderSelection) => new(new SingleProviderCapability<IAccountAdder>(accountAdder), new TokenCacheConsentViewModel(new ImmediateUiDispatcher(), CreateLocalization()), folderSelection, CreateLocalization(), NullLogger<AddAccountViewModel>.Instance);

    public static AccountDialogViewModel CreateAccountDialog(FakeAccountActions actions) => CreateAccountDialog(actions, new FakeSyncRootReader(), new InMemoryAccountStore(), new FakeFolderPicker());

    public static AccountDialogViewModel CreateAccountDialog(FakeAccountActions actions, FakeSyncRootReader syncRoots, IAccountStore accounts, FakeFolderPicker folderPicker) => new(actions, syncRoots, accounts, folderPicker, CreateLocalization());

    public static CloudAccount CreateAccountWithUnknownCounts() => CloudAccountFactory.Create(CloudAccountIdentityFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), "Work", "work@example.com", CloudProviderFactory.CreateOneDrive()), StorageUsageFactory.Create(782_643_000_000, 1_099_511_627_776), Option.None<int>(), Option.None<int>(), SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>(), AccountSignInStateFactory.CreateSignedIn());

    public static CloudAccount CreateAccountNeedingSignIn() => CloudAccountFactory.Create(CloudAccountIdentityFactory.Create(AccountIdFactory.Create(Guid.Parse("6f7e8d9c-0b1a-4c3d-9e8f-7a6b5c4d3e2f")), "Personal", "personal@example.com", CloudProviderFactory.CreateOneDrive()), StorageUsageFactory.Create(12_884_901_888, 1_099_511_627_776), 4, 1_204, SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>(), AccountSignInStateFactory.CreateSignInRequired());

    public static AccountViewModel CreateAccountWithFailedSync(SyncFailureReason reason) => CreateAccountViewModel(CreateAccount(SyncStatusFactory.CreateFailed(reason), Option.None<DateTimeOffset>()), NoAccountCommands.Instance);

    public static AccountViewModel CreateAccountViewModel(CloudAccount account, IAccountCommands commands) => new(account, [], commands, CreateLocalization(), CreateTime());

    public static MainWindowViewModel CreateMainWindowViewModel(SyncOverviewViewModel overview, DisplaySettingsViewModel displaySettings) => CreateMainWindowViewModel(overview, displaySettings, CreateAddAccount(new FakeAccountAdder()));

    public static MainWindowViewModel CreateMainWindowViewModel(SyncOverviewViewModel overview, DisplaySettingsViewModel displaySettings, AddAccountViewModel addAccount) => CreateMainWindowViewModel(overview, displaySettings, addAccount, CreateFolderSelection());

    public static MainWindowViewModel CreateMainWindowViewModel(SyncOverviewViewModel overview, DisplaySettingsViewModel displaySettings, FolderSelectionViewModel folderSelection) => CreateMainWindowViewModel(overview, displaySettings, CreateAddAccount(new FakeAccountAdder()), folderSelection);

    public static MainWindowViewModel CreateMainWindowViewModel(SyncOverviewViewModel overview, DisplaySettingsViewModel displaySettings, AddAccountViewModel addAccount, FolderSelectionViewModel folderSelection)
    {
        var localization = CreateLocalization();
        var syncActions = new RecordingSyncActions();

        return new MainWindowViewModel(localization, displaySettings, new MainWindowLayouts(new LayoutAViewModel(overview, syncActions, localization), new LayoutBViewModel(overview, CreateLocalPathFormatter(), syncActions, localization), new LayoutCViewModel(overview, syncActions, localization)), new MainWindowDialogs(addAccount, CreateAccountDialog(new FakeAccountActions()), folderSelection, new ConfirmSyncRestartViewModel(localization), CreateConflictList()));
    }

    public static FolderSelectionViewModel CreateFolderSelection() => CreateFolderSelection(new FakeFolderTreeSource(), new FakeSelectionRuleReader(), new FakeSelectionRuleWriter(), new FakeSyncPairReader(), new InMemoryAccountStore());

    public static FolderSelectionViewModel CreateFolderSelection(FakeFolderTreeSource tree, FakeSelectionRuleReader rules, FakeSelectionRuleWriter writer, FakeSyncPairReader pairs, IAccountStore accounts) => new(tree, rules, writer, new SyncSelectionEvaluator(), pairs, accounts, CreateLocalization());

    public static ConflictListViewModel CreateConflictList() => CreateConflictList(new FixedAccountsSource(), new FakeAttentionReader(), new FakeConflictResolver());

    public static ConflictListViewModel CreateConflictList(ISyncOverviewSource accounts, FakeAttentionReader reader, FakeConflictResolver resolver, FakeHeldDeletionDecider decider, FakeSyncPairReader pairs) => new(new ConflictListSources(accounts, reader, pairs), resolver, decider, new ConflictResolutionPlanner(), CreateLocalization(), NullLogger<ConflictListViewModel>.Instance);

    public static ConflictListViewModel CreateConflictList(ISyncOverviewSource accounts, FakeAttentionReader reader, FakeConflictResolver resolver) => CreateConflictList(accounts, reader, resolver, NullLogger<ConflictListViewModel>.Instance);

    public static ConflictListViewModel CreateConflictList(ISyncOverviewSource accounts, FakeAttentionReader reader, FakeConflictResolver resolver, ILogger<ConflictListViewModel> logger) => new(new ConflictListSources(accounts, reader, new FakeSyncPairReader()), resolver, new FakeHeldDeletionDecider(), new ConflictResolutionPlanner(), CreateLocalization(), logger);
}
