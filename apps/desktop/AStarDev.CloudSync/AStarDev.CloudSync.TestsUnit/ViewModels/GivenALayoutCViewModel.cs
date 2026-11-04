using AStarDev.CloudSync.Core.Placeholder;
using AStarDev.CloudSync.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.TestsUnit.ViewModels;

public class GivenALayoutCViewModel
{
    [Fact]
    public async Task when_the_overview_is_loaded_then_the_headline_counts_pairs_in_sync_syncing_and_conflicts()
    {
        var viewModel = new LayoutCViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateLocalization());

        viewModel.Headline.ShouldBe("4 in sync, 1 syncing, 2 conflicts");
    }

    [Fact]
    public void when_the_overview_is_not_loaded_then_the_headline_counts_nothing()
    {
        var viewModel = new LayoutCViewModel(CreateUnloadedOverview(), TestData.CreateLocalization());

        viewModel.Headline.ShouldBe("0 in sync, 0 syncing, 0 conflicts");
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
        changed.ShouldContain(nameof(LayoutCViewModel.ProviderFilters));
        changed.ShouldContain(nameof(LayoutCViewModel.SyncPairs));
        viewModel.Headline.ShouldBe("4 in sync, 1 syncing, 2 conflicts");
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
