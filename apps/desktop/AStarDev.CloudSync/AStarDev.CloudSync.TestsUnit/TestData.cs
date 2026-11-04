using System.Globalization;
using AStarDev.CloudSync.Core;
using AStarDev.CloudSync.Core.Placeholder;
using AStarDev.CloudSync.ViewModels;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.TestsUnit;

internal static class TestData
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

    public static Task<SyncOverview> CreateOverviewAsync() => new PlaceholderSyncOverviewSource(CreateTime()).GetOverviewAsync(TestContext.Current.CancellationToken);

    public static async Task<SyncOverviewViewModel> CreateLoadedOverviewViewModelAsync()
    {
        var viewModel = new SyncOverviewViewModel(new PlaceholderSyncOverviewSource(CreateTime()), CreateLocalization(), CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        return viewModel;
    }

    public static CloudAccount CreateAccount(SyncStatus status, Option<DateTimeOffset> lastSync) => CloudAccountFactory.Create(AccountIdFactory.Create(), "Work", "work@example.com", CloudProviderFactory.CreateOneDrive(), StorageUsageFactory.Create(782_643_000_000, 1_099_511_627_776), 30, 240_686, status, lastSync);

    public static DisplaySettingsViewModel CreateDisplaySettings() => new(new InMemoryDisplayPreferencesStore(), new RecordingThemeApplier(), CreateLocalization());

    public static MainWindowViewModel CreateMainWindowViewModel(SyncOverviewViewModel overview, DisplaySettingsViewModel displaySettings)
    {
        var localization = CreateLocalization();

        return new MainWindowViewModel(localization, displaySettings, new LayoutAViewModel(overview, localization), new LayoutBViewModel(overview, CreateLocalPathFormatter(), localization), new LayoutCViewModel(overview, localization));
    }
}
