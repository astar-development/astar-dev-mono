using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Layouts;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.CloudSync.ViewModels.TestsUnit.Overview;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Layouts;

public class GivenALayoutCViewModel
{
    [Fact]
    public async Task when_the_new_sync_pair_command_runs_then_a_sync_pair_is_added_without_a_chosen_account()
    {
        var syncActions = new RecordingSyncActions();
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), syncActions, TestData.CreateLocalization());

        await viewModel.NewSyncPairCommand.ExecuteAsync(null);

        syncActions.Calls.ShouldBe(["AddSyncPairAsync:none"]);
    }

    [Fact]
    public async Task when_the_add_account_dialog_closes_then_the_overview_is_read_again()
    {
        var source = new CountingSyncOverviewSource(await TestData.CreateOverviewAsync());
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        var layout = new LayoutCViewModel(overview, new RecordingSyncActions(), TestData.CreateLocalization());

        await layout.AddAccountCommand.ExecuteAsync(null);

        (source.Reads, overview.LoadState).ShouldBe((1, SyncOverviewLoadStateFactory.CreateLoaded()));
    }

    [Fact]
    public async Task when_the_add_account_command_runs_then_an_account_is_added()
    {
        var syncActions = new RecordingSyncActions();
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), syncActions, TestData.CreateLocalization());

        await viewModel.AddAccountCommand.ExecuteAsync(null);

        syncActions.Calls.ShouldBe(["AddAccountAsync"]);
    }

    [Fact]
    public async Task when_the_overview_is_loaded_then_the_headline_counts_pairs_in_sync_syncing_conflicts_and_paused()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());

        viewModel.Headline.ShouldBe("4 in sync, 1 syncing, 2 conflicts, 1 paused");
    }

    [Fact]
    public void when_the_overview_is_not_loaded_then_the_headline_counts_nothing()
    {
        var viewModel = new LayoutCViewModel(CreateUnloadedOverview(), new RecordingSyncActions(), TestData.CreateLocalization());

        viewModel.Headline.ShouldBe("0 in sync, 0 syncing, 0 conflicts, 0 paused");
    }

    [Fact]
    public async Task when_the_overview_is_loaded_then_the_moved_today_line_shows_the_bytes_moved_today()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());

        viewModel.MovedTodayText.ShouldBe("3.1 GB moved today");
    }

    [Fact]
    public void when_the_overview_is_not_loaded_then_the_moved_today_line_shows_nothing_moved()
    {
        var viewModel = new LayoutCViewModel(CreateUnloadedOverview(), new RecordingSyncActions(), TestData.CreateLocalization());

        viewModel.MovedTodayText.ShouldBe("0 B moved today");
    }

    [Fact]
    public async Task when_one_pair_of_each_status_is_loaded_then_the_headline_uses_the_singular_conflict()
    {
        var account = TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());
        var conflictedPair = SyncPairFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), account.Id, "Photos", RelativePaths.Of("Photos"), Option.None<RemoteItemId>(), SyncDirectionFactory.CreateTwoWay(), SyncPairStateFactory.Create(SyncStatusFactory.CreateHasConflicts(1), 1, Option.None<DateTimeOffset>()));
        var overviewData = SyncOverviewFactory.Create([account], [conflictedPair], [], [AttentionItems.CreateConflict(account.Id, "photo.jpg")], [], 0, Option.None<DateTimeOffset>());
        var overview = new SyncOverviewViewModel(new FixedSyncOverviewSource(overviewData), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await overview.LoadAsync(TestContext.Current.CancellationToken);

        var viewModel = new LayoutCViewModel(overview, new RecordingSyncActions(), TestData.CreateLocalization());

        viewModel.Headline.ShouldBe("0 in sync, 0 syncing, 1 conflict, 0 paused");
    }

    [Fact]
    public async Task when_the_overview_is_loaded_then_the_all_pairs_filter_counts_every_pair()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());

        viewModel.AllPairsFilterText.ShouldBe("All pairs · 7");
    }

    [Fact]
    public async Task when_the_overview_is_loaded_then_there_is_one_provider_filter_per_provider_in_the_data()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());

        viewModel.ProviderFilters.Select(filter => filter.Label).ShouldBe(["OneDrive · 7"]);
    }

    [Fact]
    public async Task when_the_overview_is_loaded_then_pairs_needing_attention_come_first_then_syncing_pairs()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());

        viewModel.SyncPairs.Select(pair => pair.Name).ShouldBe(["Holiday photos", "Documents", "Projects", "Desktop", "Recipes", "Music", "Backups"]);
    }

    [Fact]
    public async Task when_the_overview_loads_then_property_changed_is_raised_for_the_projections()
    {
        var overview = CreateUnloadedOverview();
        var viewModel = new LayoutCViewModel(overview, new RecordingSyncActions(), TestData.CreateLocalization());
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        await overview.LoadAsync(TestContext.Current.CancellationToken);

        changed.ShouldContain(nameof(LayoutCViewModel.Headline));
        changed.ShouldContain(nameof(LayoutCViewModel.MovedTodayText));
        changed.ShouldContain(nameof(LayoutCViewModel.ProviderFilters));
        changed.ShouldContain(nameof(LayoutCViewModel.SyncPairs));
        viewModel.Headline.ShouldBe("4 in sync, 1 syncing, 2 conflicts, 1 paused");
    }

    [Fact]
    public async Task when_created_then_the_all_pairs_filter_is_selected_and_no_provider_filter_is()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());

        viewModel.IsAllPairsSelected.ShouldBeTrue();
        viewModel.ProviderFilters.ShouldAllBe(filter => !filter.IsSelected);
    }

    [Fact]
    public async Task when_a_provider_filter_is_selected_then_it_is_highlighted_instead_of_all_pairs()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());

        viewModel.ProviderFilters[0].SelectCommand.Execute(null);

        viewModel.ProviderFilters[0].IsSelected.ShouldBeTrue();
        viewModel.IsAllPairsSelected.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_provider_filter_is_selected_then_only_pairs_matching_it_are_shown()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());
        var oneDriveFilter = viewModel.ProviderFilters[0];

        oneDriveFilter.SelectCommand.Execute(null);

        viewModel.SyncPairs.Count.ShouldBe(7);
        viewModel.SyncPairs.ShouldAllBe(pair => oneDriveFilter.Matches(pair));
    }

    [Fact]
    public async Task when_all_pairs_is_selected_after_a_provider_filter_then_every_pair_is_shown_and_all_pairs_is_highlighted()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());
        viewModel.ProviderFilters[0].SelectCommand.Execute(null);

        viewModel.SelectAllPairsCommand.Execute(null);

        viewModel.IsAllPairsSelected.ShouldBeTrue();
        viewModel.ProviderFilters.ShouldAllBe(filter => !filter.IsSelected);
        viewModel.SyncPairs.Count.ShouldBe(7);
    }

    [Fact]
    public async Task when_a_provider_filter_is_selected_then_property_changed_is_raised_for_the_pairs_and_the_all_pairs_highlight()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        viewModel.ProviderFilters[0].SelectCommand.Execute(null);

        changed.ShouldContain(nameof(LayoutCViewModel.SyncPairs));
        changed.ShouldContain(nameof(LayoutCViewModel.IsAllPairsSelected));
    }

    [Fact]
    public async Task when_the_overview_is_loaded_then_there_is_one_account_card_per_account_and_none_is_selected()
    {
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();
        var viewModel = new LayoutCViewModel(overview, new RecordingSyncActions(), TestData.CreateLocalization());

        viewModel.AccountCards.Select(card => card.Account).ShouldBe(overview.Accounts);
        viewModel.AccountCards.ShouldAllBe(card => !card.IsSelected);
    }

    [Fact]
    public async Task when_an_account_card_is_selected_then_it_is_highlighted_and_only_its_pairs_are_shown()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());
        var card = viewModel.AccountCards.First(candidate => viewModel.Overview.SyncPairs.Any(pair => pair.AccountId == candidate.Account.Id));

        card.SelectCommand.Execute(null);

        card.IsSelected.ShouldBeTrue();
        viewModel.AccountCards.Count(candidate => candidate.IsSelected).ShouldBe(1);
        viewModel.SyncPairs.ShouldNotBeEmpty();
        viewModel.SyncPairs.ShouldAllBe(pair => pair.AccountId == card.Account.Id);
        viewModel.SyncPairs.Count.ShouldBe(viewModel.Overview.SyncPairs.Count(pair => pair.AccountId == card.Account.Id));
    }

    [Fact]
    public async Task when_a_different_account_card_is_selected_then_the_selection_moves_to_it()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());
        viewModel.AccountCards[0].SelectCommand.Execute(null);

        viewModel.AccountCards[1].SelectCommand.Execute(null);

        viewModel.AccountCards.Select(card => card.IsSelected).ShouldBe([false, true, false, false]);
        viewModel.SyncPairs.ShouldAllBe(pair => pair.AccountId == viewModel.AccountCards[1].Account.Id);
    }

    [Fact]
    public async Task when_the_selected_account_card_is_selected_again_then_every_pair_is_shown()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());
        viewModel.AccountCards[0].SelectCommand.Execute(null);

        viewModel.AccountCards[0].SelectCommand.Execute(null);

        viewModel.AccountCards.ShouldAllBe(card => !card.IsSelected);
        viewModel.SyncPairs.Count.ShouldBe(7);
    }

    [Fact]
    public async Task when_an_account_card_and_a_provider_filter_are_selected_then_only_pairs_passing_both_are_shown()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());
        var oneDriveFilter = viewModel.ProviderFilters[0];
        var card = viewModel.AccountCards.First(candidate => viewModel.Overview.SyncPairs.Any(pair => pair.AccountId == candidate.Account.Id));

        card.SelectCommand.Execute(null);
        oneDriveFilter.SelectCommand.Execute(null);

        viewModel.SyncPairs.ShouldNotBeEmpty();
        viewModel.SyncPairs.ShouldAllBe(pair => pair.AccountId == card.Account.Id && oneDriveFilter.Matches(pair));
    }

    [Fact]
    public async Task when_all_pairs_is_selected_after_an_account_card_then_the_account_selection_is_cleared()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());
        viewModel.AccountCards[0].SelectCommand.Execute(null);

        viewModel.SelectAllPairsCommand.Execute(null);

        viewModel.AccountCards.ShouldAllBe(card => !card.IsSelected);
        viewModel.SyncPairs.Count.ShouldBe(7);
    }

    [Fact]
    public async Task when_an_account_card_is_selected_then_property_changed_is_raised_for_the_pairs_and_the_all_pairs_highlight()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        viewModel.AccountCards[0].SelectCommand.Execute(null);

        changed.ShouldContain(nameof(LayoutCViewModel.SyncPairs));
        changed.ShouldContain(nameof(LayoutCViewModel.IsAllPairsSelected));
    }

    [Fact]
    public async Task when_the_overview_loads_again_then_the_selected_account_stays_selected()
    {
        var overview = new SyncOverviewViewModel(new CountingSyncOverviewSource(await TestData.CreateOverviewAsync()), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await overview.LoadAsync(TestContext.Current.CancellationToken);
        var viewModel = new LayoutCViewModel(overview, new RecordingSyncActions(), TestData.CreateLocalization());
        var selectedId = viewModel.AccountCards[1].Account.Id;
        viewModel.AccountCards[1].SelectCommand.Execute(null);

        await overview.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.AccountCards.Single(card => card.IsSelected).Account.Id.ShouldBe(selectedId);
    }

    [Fact]
    public async Task when_the_search_text_matches_a_pair_then_only_matching_pairs_are_shown()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());

        viewModel.SearchText = "  RECIPES ";

        viewModel.SyncPairs.Select(pair => pair.Name).ShouldBe(["Recipes"]);
    }

    [Fact]
    public async Task when_the_search_text_matches_an_account_then_all_of_its_pairs_are_shown()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());
        var archive = viewModel.Overview.Accounts.Single(account => account.Name == "Archive");

        viewModel.SearchText = "archive@example";

        viewModel.SyncPairs.ShouldNotBeEmpty();
        viewModel.SyncPairs.ShouldAllBe(pair => pair.AccountId == archive.Id);
        viewModel.SyncPairs.Count.ShouldBe(viewModel.Overview.SyncPairs.Count(pair => pair.AccountId == archive.Id));
    }

    [Fact]
    public async Task when_the_search_text_is_blank_again_then_every_pair_and_account_card_is_shown()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());
        viewModel.SearchText = "recipes";

        viewModel.SearchText = "   ";

        (viewModel.SyncPairs.Count, viewModel.AccountCards.Count).ShouldBe((7, 4));
    }

    [Fact]
    public async Task when_the_search_text_is_set_then_only_accounts_that_match_or_own_a_matching_pair_keep_a_card()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());
        var owner = viewModel.Overview.SyncPairs.Single(pair => pair.Name == "Recipes").AccountId;

        viewModel.SearchText = "recipes";

        viewModel.AccountCards.Select(card => card.Account.Id).ShouldBe([owner]);
    }

    [Fact]
    public async Task when_the_search_text_changes_then_property_changed_is_raised_for_the_pairs_and_the_account_cards()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        viewModel.SearchText = "recipes";

        changed.ShouldContain(nameof(LayoutCViewModel.SyncPairs));
        changed.ShouldContain(nameof(LayoutCViewModel.AccountCards));
    }

    [Fact]
    public async Task when_the_search_text_and_a_provider_chip_and_an_account_card_are_combined_then_every_filter_applies()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());
        var owner = viewModel.Overview.SyncPairs.Single(pair => pair.Name == "Recipes").AccountId;
        viewModel.ProviderFilters[0].SelectCommand.Execute(null);

        viewModel.SearchText = "recipes";
        viewModel.AccountCards.Single(card => card.Account.Id == owner).SelectCommand.Execute(null);

        viewModel.SyncPairs.Select(pair => pair.Name).ShouldBe(["Recipes"]);
    }

    [Fact]
    public async Task when_created_then_the_command_search_placeholder_names_what_is_searched()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());

        viewModel.CommandSearchPlaceholder.ShouldBe("Search accounts and folders");
    }

    [Fact]
    public void when_created_then_the_sort_options_are_needs_attention_first_and_name()
    {
        var viewModel = new LayoutCViewModel(CreateUnloadedOverview(), new RecordingSyncActions(), TestData.CreateLocalization());

        viewModel.SortOptions.Select(option => option.Label).ShouldBe(["needs attention first", "name"]);
        viewModel.SortOptions[0].Order.ShouldBeOfType<LayoutCSortOrder.NeedsAttentionFirst>();
        viewModel.SortOptions[1].Order.ShouldBeOfType<LayoutCSortOrder.ByName>();
    }

    [Fact]
    public void when_created_then_the_sort_is_needs_attention_first()
    {
        var viewModel = new LayoutCViewModel(CreateUnloadedOverview(), new RecordingSyncActions(), TestData.CreateLocalization());

        viewModel.SelectedSortOption.ShouldBeSameAs(viewModel.SortOptions[0]);
        viewModel.SortText.ShouldBe("Sort: needs attention first");
    }

    [Fact]
    public async Task when_the_sort_is_changed_to_name_then_the_pairs_are_in_name_order_and_the_sort_text_follows()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());

        viewModel.SelectedSortOption = viewModel.SortOptions[1];

        viewModel.SyncPairs.Select(pair => pair.Name).ShouldBe(["Backups", "Desktop", "Documents", "Holiday photos", "Music", "Projects", "Recipes"]);
        viewModel.SortText.ShouldBe("Sort: name");
    }

    [Fact]
    public async Task when_the_sort_is_changed_then_property_changed_is_raised_for_the_pairs_and_the_sort_text()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), new RecordingSyncActions(), TestData.CreateLocalization());
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        viewModel.SelectedSortOption = viewModel.SortOptions[1];

        changed.ShouldContain(nameof(LayoutCViewModel.SyncPairs));
        changed.ShouldContain(nameof(LayoutCViewModel.SortText));
    }

    [Fact]
    public void when_created_then_the_static_text_is_localised()
    {
        var viewModel = new LayoutCViewModel(CreateUnloadedOverview(), new RecordingSyncActions(), TestData.CreateLocalization());

        string[] texts = [viewModel.Title, viewModel.CommandSearchPlaceholder, viewModel.CommandSearchShortcut, viewModel.NewSyncPairText, viewModel.StatusLabel, viewModel.SortText, viewModel.LiveActivityText, viewModel.LiveText, viewModel.AccountsText, viewModel.AddAccountText];

        texts.ShouldAllBe(text => !string.IsNullOrWhiteSpace(text) && !text.StartsWith("LayoutC."));
    }

    [Fact]
    public void when_created_then_the_shared_overview_is_exposed()
    {
        var overview = CreateUnloadedOverview();

        var viewModel = new LayoutCViewModel(overview, new RecordingSyncActions(), TestData.CreateLocalization());

        viewModel.Overview.ShouldBeSameAs(overview);
    }

    private static SyncOverviewViewModel CreateUnloadedOverview() => new(new PlaceholderSyncOverviewSource(TestData.CreateTime()), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
}
