using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Conflicts;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Classification;
using AStarDev.CloudSync.ViewModels.Layouts;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.CloudSync.ViewModels.TestsUnit.Overview;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Layouts;

public class GivenALayoutAViewModel
{
    [Fact]
    public async Task when_the_add_account_dialog_closes_then_the_overview_is_read_again()
    {
        var source = new CountingSyncOverviewSource(await TestData.CreateOverviewAsync());
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        var layout = new LayoutAViewModel(overview, new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());

        await layout.AddAccountCommand.ExecuteAsync(null);

        (source.Reads, overview.LoadState).ShouldBe((1, SyncOverviewLoadStateFactory.CreateLoaded()));
    }

    [Fact]
    public async Task when_the_add_account_command_runs_then_an_account_is_added()
    {
        var syncActions = new RecordingSyncActions();
        var layout = new LayoutAViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), syncActions, TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());

        await layout.AddAccountCommand.ExecuteAsync(null);

        syncActions.Calls.ShouldBe(["AddAccountAsync"]);
    }

    [Fact]
    public async Task when_the_search_rail_button_is_chosen_then_only_the_search_page_is_shown()
    {
        var layout = new LayoutAViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());

        layout.ShowSearchCommand.Execute(null);

        (layout.IsSearchPageShown, layout.IsAccountsPageShown, layout.IsFoldersPageShown, layout.IsActivityPageShown, layout.IsConflictsPageShown).ShouldBe((true, false, false, false, false));
    }

    [Fact]
    public async Task when_the_search_page_is_shown_then_it_lists_every_account_and_folder_until_text_is_typed()
    {
        var layout = new LayoutAViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());
        layout.ShowSearchCommand.Execute(null);
        var before = (layout.Search.Accounts.Count, layout.Search.Folders.Count);

        layout.Search.SearchText = "recipes";

        before.ShouldBe((4, 7));
        layout.Search.Folders.Select(folder => folder.Name).ShouldBe(["Recipes"]);
    }

    [Fact]
    public async Task when_the_overview_loads_then_the_provider_chips_are_all_plus_each_provider_present()
    {
        var layout = await CreateLoadedLayoutAsync();

        layout.ProviderChips.Select(chip => chip.Label).ShouldBe(["All", "OneDrive"]);
    }

    [Fact]
    public void when_the_overview_has_no_accounts_then_only_the_all_chip_is_shown()
    {
        var layout = new LayoutAViewModel(new SyncOverviewViewModel(Substitute.For<ISyncOverviewSource>(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance), new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());

        layout.ProviderChips.Select(chip => chip.Label).ShouldBe(["All"]);
    }

    [Fact]
    public async Task when_the_overview_loads_then_the_all_chip_is_selected_and_every_account_is_visible()
    {
        var layout = await CreateLoadedLayoutAsync();

        layout.ProviderChips[0].IsSelected.ShouldBeTrue();
        layout.ProviderChips[1].IsSelected.ShouldBeFalse();
        layout.VisibleAccounts.Select(card => card.Account.Name).ShouldBe(["Work", "Personal", "Photos", "Archive"]);
    }

    [Fact]
    public async Task when_a_provider_chip_is_selected_then_only_it_is_selected_and_its_accounts_are_visible()
    {
        var layout = await CreateLoadedLayoutAsync();

        layout.ProviderChips[1].SelectCommand.Execute(null);

        layout.ProviderChips[0].IsSelected.ShouldBeFalse();
        layout.ProviderChips[1].IsSelected.ShouldBeTrue();
        layout.VisibleAccounts.Count.ShouldBe(4);
    }

    [Fact]
    public async Task when_the_overview_loads_then_the_account_count_is_shown()
    {
        var layout = await CreateLoadedLayoutAsync();

        layout.AccountCountText.ShouldBe("4");
    }

    [Fact]
    public async Task when_an_account_is_syncing_then_the_status_summary_counts_the_syncing_accounts()
    {
        var layout = await CreateLoadedLayoutAsync();

        layout.StatusSummaryText.ShouldBe("Syncing 1 of 4 accounts");
    }

    [Fact]
    public async Task when_the_only_account_is_syncing_then_the_status_summary_uses_the_singular()
    {
        var source = Substitute.For<ISyncOverviewSource>();
        source.GetOverviewAsync(Arg.Any<CancellationToken>()).Returns(SyncOverviewFactory.Create([TestData.CreateAccount(SyncStatusFactory.CreateSyncing(50), Option.None<DateTimeOffset>())], [], [], [], [], 0, Option.None<DateTimeOffset>()));
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        var layout = new LayoutAViewModel(overview, new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());

        await overview.LoadAsync(TestContext.Current.CancellationToken);

        layout.StatusSummaryText.ShouldBe("Syncing 1 of 1 account");
    }

    [Fact]
    public async Task when_no_account_is_syncing_then_the_status_summary_says_so()
    {
        var source = Substitute.For<ISyncOverviewSource>();
        source.GetOverviewAsync(Arg.Any<CancellationToken>()).Returns(SyncOverviewFactory.Create([TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>())], [], [], [], [], 0, Option.None<DateTimeOffset>()));
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        var layout = new LayoutAViewModel(overview, new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());

        await overview.LoadAsync(TestContext.Current.CancellationToken);

        layout.StatusSummaryText.ShouldBe("No accounts syncing");
    }

    [Fact]
    public async Task when_the_overview_loads_then_the_status_check_text_shows_the_overall_status_and_when_sync_was_last_checked()
    {
        var layout = await CreateLoadedLayoutAsync();

        layout.StatusCheckedText.ShouldBe("Syncing · checked 10 min ago");
    }

    [Fact]
    public async Task when_every_account_is_up_to_date_then_the_status_check_text_reads_up_to_date()
    {
        var source = Substitute.For<ISyncOverviewSource>();
        source.GetOverviewAsync(Arg.Any<CancellationToken>()).Returns(SyncOverviewFactory.Create([TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>())], [], [], [], [], 0, Option.Some(TestData.Now.AddMinutes(-10))));
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        var layout = new LayoutAViewModel(overview, new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());

        await overview.LoadAsync(TestContext.Current.CancellationToken);

        layout.StatusCheckedText.ShouldBe("Up to date · checked 10 min ago");
    }

    [Fact]
    public async Task when_the_overview_loads_then_the_status_check_tooltip_says_the_check_covers_all_accounts()
    {
        var layout = await CreateLoadedLayoutAsync();

        layout.StatusCheckedTooltip.ShouldBe("Most recent sync check across all accounts");
    }

    [Fact]
    public async Task when_sync_has_never_been_checked_then_the_status_check_text_says_so()
    {
        var source = Substitute.For<ISyncOverviewSource>();
        source.GetOverviewAsync(Arg.Any<CancellationToken>()).Returns(SyncOverviewFactory.Create([TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>())], [], [], [], [], 0, Option.None<DateTimeOffset>()));
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        var layout = new LayoutAViewModel(overview, new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());

        await overview.LoadAsync(TestContext.Current.CancellationToken);

        layout.StatusCheckedText.ShouldBe("Up to date · never checked");
    }

    [Fact]
    public void when_created_then_the_pane_and_rail_text_is_localised()
    {
        var layout = new LayoutAViewModel(new SyncOverviewViewModel(Substitute.For<ISyncOverviewSource>(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance), new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());

        layout.AccountsTitle.ShouldBe("Accounts");
        layout.AddAccountText.ShouldBe("+ Add account");
        layout.DashboardText.ShouldBe("Dashboard");
        layout.FoldersText.ShouldBe("Folders");
        layout.ActivityText.ShouldBe("Activity");
        layout.ConflictsText.ShouldBe("Conflicts");
        layout.SearchText.ShouldBe("Search");
    }

    [Fact]
    public async Task when_the_overview_loads_then_the_first_account_is_selected()
    {
        var layout = await CreateLoadedLayoutAsync();

        layout.VisibleAccounts.Where(card => card.IsSelected).Select(card => card.Account.Name).ShouldBe(["Work"]);
    }

    [Fact]
    public async Task when_an_account_is_selected_then_only_it_is_selected()
    {
        var layout = await CreateLoadedLayoutAsync();
        var photos = layout.VisibleAccounts.Single(card => card.Account.Name == "Photos");

        photos.SelectCommand.Execute(null);

        layout.VisibleAccounts.Where(card => card.IsSelected).ShouldHaveSingleItem().ShouldBeSameAs(photos);
    }

    [Fact]
    public async Task when_a_provider_chip_is_selected_then_the_selected_account_is_kept_when_still_visible()
    {
        var layout = await CreateLoadedLayoutAsync();
        var photos = layout.VisibleAccounts.Single(card => card.Account.Name == "Photos");
        photos.SelectCommand.Execute(null);

        layout.ProviderChips[1].SelectCommand.Execute(null);

        layout.VisibleAccounts.Where(card => card.IsSelected).ShouldHaveSingleItem().ShouldBeSameAs(photos);
    }

    private static async Task<LayoutAViewModel> CreateLoadedLayoutAsync()
    {
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();

        return new LayoutAViewModel(overview, new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());
    }

    [Fact]
    public async Task when_created_then_the_accounts_page_is_shown()
    {
        var layout = new LayoutAViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());

        (layout.IsAccountsPageShown, layout.IsConflictsPageShown).ShouldBe((true, false));
    }

    [Fact]
    public async Task when_the_conflicts_rail_button_is_chosen_then_the_conflicts_page_is_shown_and_every_accounts_conflicts_are_read()
    {
        var reader = new FakeAttentionReader();
        var photos = AttentionItems.Account("Photos");
        var work = AttentionItems.Account("Work");
        reader.Add(photos.Id, AttentionItems.CreateConflict(photos.Id, "Camera/1.jpg"));
        reader.Add(work.Id, AttentionItems.CreateConflict(work.Id, "Docs/a.docx"));
        var layout = new LayoutAViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateConflictListFactory(new FixedAccountsSource(photos, work), reader), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());

        layout.ShowConflictsCommand.Execute(null);
        await layout.ConflictList.RefreshCommand.ExecutionTask!;

        (layout.IsAccountsPageShown, layout.IsConflictsPageShown).ShouldBe((false, true));
        layout.ConflictList.Groups.Select(group => group.HeaderText).ShouldBe(["Photos · 1 conflict", "Work · 1 conflict"]);
    }

    [Fact]
    public async Task when_the_accounts_rail_button_is_chosen_after_the_conflicts_then_the_accounts_page_is_shown_again()
    {
        var layout = new LayoutAViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());
        layout.ShowConflictsCommand.Execute(null);

        layout.ShowAccountsCommand.Execute(null);

        (layout.IsAccountsPageShown, layout.IsConflictsPageShown).ShouldBe((true, false));
    }

    [Fact]
    public async Task when_the_conflicts_page_is_chosen_again_then_the_conflicts_are_read_again()
    {
        var reader = new FakeAttentionReader();
        var layout = new LayoutAViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateConflictListFactory(new FixedAccountsSource(AttentionItems.Account("Photos")), reader), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());
        layout.ShowConflictsCommand.Execute(null);
        await layout.ConflictList.RefreshCommand.ExecutionTask!;

        layout.ShowAccountsCommand.Execute(null);
        layout.ShowConflictsCommand.Execute(null);
        await layout.ConflictList.RefreshCommand.ExecutionTask!;

        reader.Reads.ShouldBe(2);
    }

    [Fact]
    public async Task when_the_folders_rail_button_is_chosen_then_the_folders_page_is_shown_and_the_selected_accounts_folders_are_read()
    {
        var tree = new FakeFolderTreeSource();
        var layout = new LayoutAViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(tree), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());

        layout.ShowFoldersCommand.Execute(null);
        await layout.Folders.ShowAccountCommand.ExecutionTask!;

        (layout.IsFoldersPageShown, layout.IsAccountsPageShown, layout.IsConflictsPageShown).ShouldBe((true, false, false));
        tree.RequestedAccounts.ShouldBe([layout.VisibleAccounts.Single(card => card.IsSelected).Account.Id]);
    }

    [Fact]
    public async Task when_another_account_is_selected_on_the_folders_page_then_its_folders_are_read()
    {
        var tree = new FakeFolderTreeSource();
        var layout = new LayoutAViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(tree), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());
        layout.ShowFoldersCommand.Execute(null);
        await layout.Folders.ShowAccountCommand.ExecutionTask!;
        var second = layout.VisibleAccounts[1];

        second.SelectCommand.Execute(null);
        await layout.Folders.ShowAccountCommand.ExecutionTask!;

        tree.RequestedAccounts[^1].ShouldBe(second.Account.Id);
    }

    [Fact]
    public async Task when_another_account_is_selected_on_the_accounts_page_then_no_folders_are_read()
    {
        var tree = new FakeFolderTreeSource();
        var layout = new LayoutAViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(tree), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());

        layout.VisibleAccounts[1].SelectCommand.Execute(null);

        tree.RequestedAccounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_activity_rail_button_is_chosen_then_the_activity_page_is_shown_and_every_accounts_activity_is_read()
    {
        var reader = new FakeActivityLogReader();
        var layout = new LayoutAViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(reader), TestData.CreateClassificationPage(), TestData.CreateLocalization());

        layout.ShowActivityCommand.Execute(null);
        await layout.Activity.RefreshCommand.ExecutionTask!;

        (layout.IsActivityPageShown, layout.IsAccountsPageShown, layout.IsFoldersPageShown, layout.IsConflictsPageShown).ShouldBe((true, false, false, false));
        reader.Requests.ShouldBe([(Option.None<AccountId>(), ActivityLogLimits.EntriesPerAccount)]);
    }

    [Fact]
    public async Task when_the_classification_rail_button_is_chosen_then_only_the_classification_page_is_shown_and_the_term_list_is_read()
    {
        var layout = new LayoutAViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());

        layout.ShowClassificationCommand.Execute(null);
        await layout.Classification.LoadCommand.ExecutionTask!;

        (layout.IsClassificationPageShown, layout.IsAccountsPageShown, layout.IsFoldersPageShown, layout.IsActivityPageShown, layout.IsConflictsPageShown, layout.IsSearchPageShown, layout.Classification.Tabs.Terms.State).ShouldBe((true, false, false, false, false, false, TermListLoadStateFactory.CreateLoaded()));
    }

    [Fact]
    public async Task when_another_page_is_chosen_after_the_classification_page_then_the_classification_page_is_hidden()
    {
        var layout = new LayoutAViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());
        layout.ShowClassificationCommand.Execute(null);

        layout.ShowAccountsCommand.Execute(null);

        (layout.IsClassificationPageShown, layout.IsAccountsPageShown).ShouldBe((false, true));
    }

    [Fact]
    public async Task when_the_classification_page_is_shown_then_the_classification_rail_text_is_localised()
    {
        var layout = new LayoutAViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());

        layout.ClassificationText.ShouldBe("Classification");
    }

    [Fact]
    public async Task when_the_activity_page_is_chosen_again_then_the_activity_is_read_again()
    {
        var reader = new FakeActivityLogReader();
        var layout = new LayoutAViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(reader), TestData.CreateClassificationPage(), TestData.CreateLocalization());
        layout.ShowActivityCommand.Execute(null);
        await layout.Activity.RefreshCommand.ExecutionTask!;

        layout.ShowAccountsCommand.Execute(null);
        layout.ShowActivityCommand.Execute(null);
        await layout.Activity.RefreshCommand.ExecutionTask!;

        reader.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_an_account_search_result_is_opened_then_that_account_is_selected_with_every_provider_shown_and_the_accounts_page_appears()
    {
        var layout = new LayoutAViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());
        var archive = layout.Overview.Accounts.Single(account => account.Name == "Archive");
        layout.ShowSearchCommand.Execute(null);

        layout.Search.OpenAccountCommand.Execute(archive);

        layout.VisibleAccounts.Single(card => card.IsSelected).Account.Id.ShouldBe(archive.Id);
        layout.ProviderChips[0].IsSelected.ShouldBeTrue();
        (layout.IsAccountsPageShown, layout.IsSearchPageShown).ShouldBe((true, false));
    }

    [Fact]
    public async Task when_a_folder_search_result_is_opened_then_its_account_is_selected_and_the_folders_page_reads_that_accounts_folders()
    {
        var tree = new FakeFolderTreeSource();
        var layout = new LayoutAViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateConflictListFactory(), TestData.CreateFolderSelectionFactory(tree), TestData.CreateActivityListFactory(), TestData.CreateClassificationPage(), TestData.CreateLocalization());
        var recipes = layout.Overview.SyncPairs.Single(pair => pair.Name == "Recipes");
        layout.ShowSearchCommand.Execute(null);

        layout.Search.OpenFolderCommand.Execute(recipes);
        await layout.Folders.ShowAccountCommand.ExecutionTask!;

        layout.VisibleAccounts.Single(card => card.IsSelected).Account.Id.ShouldBe(recipes.AccountId);
        layout.IsFoldersPageShown.ShouldBeTrue();
        tree.RequestedAccounts.ShouldBe([recipes.AccountId]);
    }
}
