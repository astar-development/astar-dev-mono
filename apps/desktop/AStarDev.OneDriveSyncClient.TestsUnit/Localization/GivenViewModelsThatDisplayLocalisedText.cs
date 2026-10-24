using System.Globalization;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Activity;
using AStarDev.OneDriveSyncClient.Classifications;
using AStarDev.OneDriveSyncClient.Conflicts;
using AStarDev.OneDriveSyncClient.Dashboard;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Home;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.OneDriveSyncClient.Localization;
using AStarDev.OneDriveSyncClient.Search;
using AStarDev.OneDriveSyncClient.TestsUnit.TestHelpers;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Localization;

public sealed class GivenViewModelsThatDisplayLocalisedText
{
    [Fact]
    public void when_the_culture_changes_then_the_activity_view_model_refreshes_its_text()
    {
        var localization = Substitute.For<ILocalizationService>();
        var sut = new ActivityViewModel(Substitute.For<ISyncRepository>(), Substitute.For<ISyncEventAggregator>(), Substitute.For<IConflictItemViewModelFactory>(), Substitute.For<IActivityItemViewModelFactory>(), Substitute.For<IUiDispatcher>(), localization);

        var raised = CultureChangeProbe.RaisedPropertyNamesAfterCultureChange(sut, localization);

        CultureChangeProbe.Refreshes(raised, nameof(ActivityViewModel.LogTabText)).ShouldBeTrue();
    }

    [Fact]
    public void when_the_culture_changes_then_the_dashboard_view_model_refreshes_the_overall_status()
    {
        var localization = Substitute.For<ILocalizationService>();
        var sut = new DashboardViewModel(localization, Substitute.For<ISyncEventAggregator>(), Substitute.For<IDashboardAccountViewModelFactory>(), Substitute.For<IActivityItemViewModelFactory>(), Substitute.For<IUiTimer>());

        var raised = CultureChangeProbe.RaisedPropertyNamesAfterCultureChange(sut, localization);

        CultureChangeProbe.Refreshes(raised, nameof(DashboardViewModel.OverallStatusText)).ShouldBeTrue();
    }

    [Fact]
    public void when_the_culture_changes_then_the_dashboard_view_model_relocalises_the_last_sync_text()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal("Common.Never").Returns("US-Never");
        var sut = new DashboardViewModel(localization, Substitute.For<ISyncEventAggregator>(), Substitute.For<IDashboardAccountViewModelFactory>(), Substitute.For<IActivityItemViewModelFactory>(), Substitute.For<IUiTimer>());
        localization.GetLocal("Common.Never").Returns("Never");

        localization.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(localization, new CultureInfo("en-GB"));

        sut.LastSyncText.ShouldBe("Never");
    }

    [Fact]
    public void when_the_culture_changes_then_the_dashboard_account_view_model_relocalises_the_last_sync_text()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal("Common.NeverSynced").Returns("US-Never synced");
        var sut = new DashboardAccountViewModel(new OneDriveAccount { Id = new AccountId("test-account") }, Substitute.For<ISyncScheduler>(), Substitute.For<IAccountRepository>(), localization, Substitute.For<IActivityItemViewModelFactory>(), Substitute.For<ILogger<DashboardAccountViewModel>>());
        sut.RefreshTimeDisplays();
        localization.GetLocal("Common.NeverSynced").Returns("Never synced");

        localization.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(localization, new CultureInfo("en-GB"));

        sut.LastSyncText.ShouldBe("Never synced");
    }

    [Fact]
    public void when_the_culture_changes_then_the_dashboard_account_view_model_refreshes_its_labels()
    {
        var localization = Substitute.For<ILocalizationService>();
        var sut = new DashboardAccountViewModel(new OneDriveAccount { Id = new AccountId("test-account") }, Substitute.For<ISyncScheduler>(), Substitute.For<IAccountRepository>(), localization, Substitute.For<IActivityItemViewModelFactory>(), Substitute.For<ILogger<DashboardAccountViewModel>>());

        var raised = CultureChangeProbe.RaisedPropertyNamesAfterCultureChange(sut, localization);

        CultureChangeProbe.Refreshes(raised, nameof(DashboardAccountViewModel.StatusLabel)).ShouldBeTrue();
    }

    [Fact]
    public void when_the_culture_changes_then_the_account_card_view_model_relocalises_the_last_sync_text()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal("Common.NeverSynced").Returns("US-Never synced");
        var sut = new AccountCardViewModel(new OneDriveAccount { Id = new AccountId("test-account") }, localization);
        localization.GetLocal("Common.NeverSynced").Returns("Never synced");

        localization.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(localization, new CultureInfo("en-GB"));

        sut.LastSyncText.ShouldBe("Never synced");
    }

    [Fact]
    public void when_the_culture_changes_then_an_activity_item_refreshes_its_type_label()
    {
        var localization = Substitute.For<ILocalizationService>();
        var sut = new ActivityItemViewModel(localization);

        var raised = CultureChangeProbe.RaisedPropertyNamesAfterCultureChange(sut, localization);

        CultureChangeProbe.Refreshes(raised, nameof(ActivityItemViewModel.TypeLabel)).ShouldBeTrue();
    }

    [Fact]
    public void when_the_culture_changes_then_the_files_view_model_refreshes_its_text()
    {
        var localization = Substitute.For<ILocalizationService>();
        var sut = new FilesViewModel(Substitute.For<IAccountFilesViewModelFactory>(), localization);

        var raised = CultureChangeProbe.RaisedPropertyNamesAfterCultureChange(sut, localization);

        CultureChangeProbe.Refreshes(raised, nameof(FilesViewModel.NoAccountsConnectedText)).ShouldBeTrue();
    }

    [Fact]
    public void when_the_culture_changes_then_the_search_view_model_refreshes_its_text()
    {
        var localization = Substitute.For<ILocalizationService>();
        var sut = new SyncedFileSearchViewModel(Substitute.For<ISyncedItemRepository>(), Substitute.For<IFileOpenerService>(), Substitute.For<IFileTypeClassifier>(), Substitute.For<IUiDispatcher>(), localization);

        var raised = CultureChangeProbe.RaisedPropertyNamesAfterCultureChange(sut, localization);

        CultureChangeProbe.Refreshes(raised, nameof(SyncedFileSearchViewModel.SearchTitleText)).ShouldBeTrue();
    }

    [Fact]
    public void when_the_culture_changes_then_the_search_view_model_keeps_the_selected_sort_order()
    {
        var localization = Substitute.For<ILocalizationService>();
        var sut = new SyncedFileSearchViewModel(Substitute.For<ISyncedItemRepository>(), Substitute.For<IFileOpenerService>(), Substitute.For<IFileTypeClassifier>(), Substitute.For<IUiDispatcher>(), localization);
        sut.SelectedSortOrderIndex = 2;

        localization.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(localization, new CultureInfo("en-GB"));

        sut.SelectedSortOrderIndex.ShouldBe(2);
    }

    [Fact]
    public void when_the_culture_changes_then_the_search_view_model_relocalises_the_sort_orders()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal("Search.SortOrder.NameAsc").Returns("US-Name");
        var sut = new SyncedFileSearchViewModel(Substitute.For<ISyncedItemRepository>(), Substitute.For<IFileOpenerService>(), Substitute.For<IFileTypeClassifier>(), Substitute.For<IUiDispatcher>(), localization);
        localization.GetLocal("Search.SortOrder.NameAsc").Returns("Name");

        localization.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(localization, new CultureInfo("en-GB"));

        sut.AvailableSortOrders[0].ShouldBe("Name");
    }

    [Fact]
    public void when_the_culture_changes_then_the_classification_rules_view_model_refreshes_its_text()
    {
        var localization = Substitute.For<ILocalizationService>();
        var sut = new FileClassificationRulesViewModel(Substitute.For<IFileClassificationRepository>(), Substitute.For<IFileClassificationExportImportService>(), Substitute.For<IFilePickerService>(), Substitute.For<IConfirmationDialogService>(), Substitute.For<ICategoryEditDialogService>(), localization, new MockFileSystem());

        var raised = CultureChangeProbe.RaisedPropertyNamesAfterCultureChange(sut, localization);

        CultureChangeProbe.Refreshes(raised, nameof(FileClassificationRulesViewModel.LoadingText)).ShouldBeTrue();
    }
}
