using AStarDev.CloudSync.Core;
using AStarDev.CloudSync.Core.Placeholder;
using AStarDev.CloudSync.ViewModels;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.TestsUnit.ViewModels;

public class GivenALayoutCViewModel
{
    [Fact]
    public async Task when_the_overview_is_loaded_then_the_headline_counts_pairs_in_sync_syncing_conflicts_and_paused()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateLocalization());

        viewModel.Headline.ShouldBe("4 in sync, 1 syncing, 2 conflicts, 1 paused");
    }

    [Fact]
    public void when_the_overview_is_not_loaded_then_the_headline_counts_nothing()
    {
        var viewModel = new LayoutCViewModel(CreateUnloadedOverview(), TestData.CreateLocalization());

        viewModel.Headline.ShouldBe("0 in sync, 0 syncing, 0 conflicts, 0 paused");
    }

    [Fact]
    public async Task when_the_overview_is_loaded_then_the_moved_today_line_shows_the_bytes_moved_today()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateLocalization());

        viewModel.MovedTodayText.ShouldBe("3.1 GB moved today");
    }

    [Fact]
    public void when_the_overview_is_not_loaded_then_the_moved_today_line_shows_nothing_moved()
    {
        var viewModel = new LayoutCViewModel(CreateUnloadedOverview(), TestData.CreateLocalization());

        viewModel.MovedTodayText.ShouldBe("0 B moved today");
    }

    [Fact]
    public async Task when_one_pair_of_each_status_is_loaded_then_the_headline_uses_the_singular_conflict()
    {
        var account = TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());
        var conflictedPair = SyncPairFactory.Create(SyncPairIdFactory.Create(), account.Id, "Photos", new DirectoryInfo("/home/user/CloudSync/Photos"), "/Photos", SyncDirectionFactory.CreateTwoWay(), SyncStatusFactory.CreateHasConflicts(1), 1, Option.None<DateTimeOffset>());
        var overviewData = SyncOverviewFactory.Create([account], [conflictedPair], [], [SyncConflictFactory.Create(conflictedPair.Id, "photo.jpg", TestData.CreateTime().GetUtcNow())], [], 0);
        var overview = new SyncOverviewViewModel(new FixedSyncOverviewSource(overviewData), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await overview.LoadAsync(TestContext.Current.CancellationToken);

        var viewModel = new LayoutCViewModel(overview, TestData.CreateLocalization());

        viewModel.Headline.ShouldBe("0 in sync, 0 syncing, 1 conflict, 0 paused");
    }

    [Fact]
    public async Task when_the_overview_is_loaded_then_the_all_pairs_filter_counts_every_pair()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateLocalization());

        viewModel.AllPairsFilterText.ShouldBe("All pairs · 7");
    }

    [Fact]
    public async Task when_the_overview_is_loaded_then_there_is_one_provider_filter_per_provider_in_the_data()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateLocalization());

        viewModel.ProviderFilters.Select(filter => filter.Label).ShouldBe(["OneDrive · 7"]);
    }

    [Fact]
    public async Task when_the_overview_is_loaded_then_pairs_needing_attention_come_first_then_syncing_pairs()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateLocalization());

        viewModel.SyncPairs.Select(pair => pair.Name).ShouldBe(["Holiday photos", "Documents", "Projects", "Desktop", "Recipes", "Music", "Backups"]);
    }

    [Fact]
    public async Task when_the_overview_loads_then_property_changed_is_raised_for_the_projections()
    {
        var overview = CreateUnloadedOverview();
        var viewModel = new LayoutCViewModel(overview, TestData.CreateLocalization());
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
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateLocalization());

        viewModel.IsAllPairsSelected.ShouldBeTrue();
        viewModel.ProviderFilters.ShouldAllBe(filter => !filter.IsSelected);
    }

    [Fact]
    public async Task when_a_provider_filter_is_selected_then_it_is_highlighted_instead_of_all_pairs()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateLocalization());

        viewModel.ProviderFilters[0].SelectCommand.Execute(null);

        viewModel.ProviderFilters[0].IsSelected.ShouldBeTrue();
        viewModel.IsAllPairsSelected.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_provider_filter_is_selected_then_only_pairs_matching_it_are_shown()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateLocalization());
        var oneDriveFilter = viewModel.ProviderFilters[0];

        oneDriveFilter.SelectCommand.Execute(null);

        viewModel.SyncPairs.Count.ShouldBe(7);
        viewModel.SyncPairs.ShouldAllBe(pair => oneDriveFilter.Matches(pair));
    }

    [Fact]
    public async Task when_all_pairs_is_selected_after_a_provider_filter_then_every_pair_is_shown_and_all_pairs_is_highlighted()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateLocalization());
        viewModel.ProviderFilters[0].SelectCommand.Execute(null);

        viewModel.SelectAllPairsCommand.Execute(null);

        viewModel.IsAllPairsSelected.ShouldBeTrue();
        viewModel.ProviderFilters.ShouldAllBe(filter => !filter.IsSelected);
        viewModel.SyncPairs.Count.ShouldBe(7);
    }

    [Fact]
    public async Task when_a_provider_filter_is_selected_then_property_changed_is_raised_for_the_pairs_and_the_all_pairs_highlight()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateLocalization());
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        viewModel.ProviderFilters[0].SelectCommand.Execute(null);

        changed.ShouldContain(nameof(LayoutCViewModel.SyncPairs));
        changed.ShouldContain(nameof(LayoutCViewModel.IsAllPairsSelected));
    }

    [Fact]
    public void when_created_then_the_sort_options_are_needs_attention_first_and_name()
    {
        var viewModel = new LayoutCViewModel(CreateUnloadedOverview(), TestData.CreateLocalization());

        viewModel.SortOptions.Select(option => option.Label).ShouldBe(["needs attention first", "name"]);
        viewModel.SortOptions[0].Order.ShouldBeOfType<LayoutCSortOrder.NeedsAttentionFirst>();
        viewModel.SortOptions[1].Order.ShouldBeOfType<LayoutCSortOrder.ByName>();
    }

    [Fact]
    public void when_created_then_the_sort_is_needs_attention_first()
    {
        var viewModel = new LayoutCViewModel(CreateUnloadedOverview(), TestData.CreateLocalization());

        viewModel.SelectedSortOption.ShouldBeSameAs(viewModel.SortOptions[0]);
        viewModel.SortText.ShouldBe("Sort: needs attention first");
    }

    [Fact]
    public async Task when_the_sort_is_changed_to_name_then_the_pairs_are_in_name_order_and_the_sort_text_follows()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateLocalization());

        viewModel.SelectedSortOption = viewModel.SortOptions[1];

        viewModel.SyncPairs.Select(pair => pair.Name).ShouldBe(["Backups", "Desktop", "Documents", "Holiday photos", "Music", "Projects", "Recipes"]);
        viewModel.SortText.ShouldBe("Sort: name");
    }

    [Fact]
    public async Task when_the_sort_is_changed_then_property_changed_is_raised_for_the_pairs_and_the_sort_text()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateLocalization());
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        viewModel.SelectedSortOption = viewModel.SortOptions[1];

        changed.ShouldContain(nameof(LayoutCViewModel.SyncPairs));
        changed.ShouldContain(nameof(LayoutCViewModel.SortText));
    }

    [Fact]
    public void when_created_then_the_static_text_is_localised()
    {
        var viewModel = new LayoutCViewModel(CreateUnloadedOverview(), TestData.CreateLocalization());

        string[] texts = [viewModel.Title, viewModel.CommandSearchPlaceholder, viewModel.CommandSearchShortcut, viewModel.NewSyncPairText, viewModel.StatusLabel, viewModel.SortText, viewModel.NeedsAttentionText, viewModel.LiveActivityText, viewModel.LiveText, viewModel.AccountsText, viewModel.AddAccountText];

        texts.ShouldAllBe(text => !string.IsNullOrWhiteSpace(text) && !text.StartsWith("LayoutC."));
    }

    [Fact]
    public void when_created_then_the_shared_overview_is_exposed()
    {
        var overview = CreateUnloadedOverview();

        var viewModel = new LayoutCViewModel(overview, TestData.CreateLocalization());

        viewModel.Overview.ShouldBeSameAs(overview);
    }

    private static SyncOverviewViewModel CreateUnloadedOverview() => new(new PlaceholderSyncOverviewSource(TestData.CreateTime()), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
}
