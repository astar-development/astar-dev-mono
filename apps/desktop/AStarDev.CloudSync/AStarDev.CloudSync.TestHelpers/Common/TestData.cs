using System.Globalization;
using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Core.Common;
using AStarDev.CloudSync.Core.Overview;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.CloudSync.Infrastructure.Placeholder;
using AStarDev.CloudSync.TestHelpers.DisplaySettings;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.ViewModels;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.CloudSync.ViewModels.DisplaySettings;
using AStarDev.CloudSync.ViewModels.Layouts;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace AStarDev.CloudSync.TestHelpers.Common;

public static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    public static ILocalizationService CreateLocalization()
    {
        var localization = new LocalizationService(typeof(App).Assembly);
        localization.Initialise(new CultureInfo("en-GB"));

        return localization;
    }

    public static FakeTimeProvider CreateTime() => new(Now);

    public static LocalPathFormatter CreateLocalPathFormatter() => new(new FixedHomeFolder(Option.Some(new DirectoryInfo("/home/user"))));

    public static Task<SyncOverview> CreateOverviewAsync() => new PlaceholderSyncOverviewSource(CreateTime()).GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(overview => overview, error => throw new InvalidOperationException(error.Message));

    public static async Task<SyncOverviewViewModel> CreateLoadedOverviewViewModelAsync()
    {
        var viewModel = new SyncOverviewViewModel(new PlaceholderSyncOverviewSource(CreateTime()), new RecordingSyncActions(), CreateLocalization(), CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        return viewModel;
    }

    public static CloudAccount CreateAccount(SyncStatus status, Option<DateTimeOffset> lastSync) => CloudAccountFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), "Work", "work@example.com", CloudProviderFactory.CreateOneDrive(), StorageUsageFactory.Create(782_643_000_000, 1_099_511_627_776), 30, 240_686, status, lastSync);

    public static DisplaySettingsViewModel CreateDisplaySettings() => new(new InMemoryDisplayPreferencesStore(), new RecordingThemeApplier(), CreateLocalization());

    public static AddAccountViewModel CreateAddAccount(IAccountAdder accountAdder) => new(accountAdder, new TokenCacheConsentViewModel(new ImmediateUiDispatcher(), CreateLocalization()), CreateLocalization());

    public static MainWindowViewModel CreateMainWindowViewModel(SyncOverviewViewModel overview, DisplaySettingsViewModel displaySettings) => CreateMainWindowViewModel(overview, displaySettings, CreateAddAccount(new FakeAccountAdder()));

    public static MainWindowViewModel CreateMainWindowViewModel(SyncOverviewViewModel overview, DisplaySettingsViewModel displaySettings, AddAccountViewModel addAccount)
    {
        var localization = CreateLocalization();
        var syncActions = new RecordingSyncActions();

        return new MainWindowViewModel(localization, displaySettings, new LayoutAViewModel(overview, syncActions, localization), new LayoutBViewModel(overview, CreateLocalPathFormatter(), syncActions, localization), new LayoutCViewModel(overview, syncActions, localization), addAccount);
    }
}
