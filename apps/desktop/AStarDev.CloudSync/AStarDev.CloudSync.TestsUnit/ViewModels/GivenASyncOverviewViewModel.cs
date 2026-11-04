using AStarDev.CloudSync.ViewModels;

namespace AStarDev.CloudSync.TestsUnit.ViewModels;

public class GivenASyncOverviewViewModel
{
    [Fact]
    public async Task when_loaded_then_every_account_and_sync_pair_is_exposed()
    {
        var overview = await TestData.CreateOverviewAsync();

        var viewModel = await TestData.CreateLoadedOverviewViewModelAsync();

        viewModel.Accounts.Count.ShouldBe(overview.Accounts.Count);
        viewModel.SyncPairs.Count.ShouldBe(overview.SyncPairs.Count);
    }

    [Fact]
    public async Task when_loaded_then_the_conflicts_name_their_sync_pair()
    {
        var viewModel = await TestData.CreateLoadedOverviewViewModelAsync();

        viewModel.Conflicts.ShouldNotBeEmpty();
        viewModel.Conflicts.ShouldAllBe(conflict => conflict.Detail.StartsWith("Holiday photos"));
    }

    [Fact]
    public async Task when_loaded_then_the_summary_tiles_count_accounts_providers_folders_and_conflicts()
    {
        var overview = await TestData.CreateOverviewAsync();

        var viewModel = await TestData.CreateLoadedOverviewViewModelAsync();

        viewModel.Stats.Select(tile => tile.Label).ShouldBe(["accounts", "providers", "folders synced", "conflicts", "overall status"]);
        viewModel.Stats[0].Value.ShouldBe(overview.Accounts.Count.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("en-GB")));
        viewModel.Stats[1].Value.ShouldBe("1");
        viewModel.Stats[3].Value.ShouldBe(overview.Conflicts.Count.ToString(System.Globalization.CultureInfo.GetCultureInfo("en-GB")));
    }

    [Fact]
    public async Task when_any_account_is_syncing_then_the_overall_status_is_syncing()
    {
        var viewModel = await TestData.CreateLoadedOverviewViewModelAsync();

        viewModel.Stats[4].Value.ShouldBe("Syncing");
    }

    [Fact]
    public async Task when_loaded_then_the_activity_feed_is_newest_first()
    {
        var viewModel = await TestData.CreateLoadedOverviewViewModelAsync();

        viewModel.Activity.ShouldNotBeEmpty();
        viewModel.Activity[0].WhenText.ShouldBe("Just now");
    }

    [Fact]
    public async Task when_loaded_then_the_throughput_is_exposed()
    {
        var viewModel = await TestData.CreateLoadedOverviewViewModelAsync();

        viewModel.Throughput.Values.ShouldNotBeEmpty();
    }

    [Fact]
    public void when_not_yet_loaded_then_the_collections_are_empty()
    {
        var viewModel = new SyncOverviewViewModel(new AStarDev.CloudSync.Core.Placeholder.PlaceholderSyncOverviewSource(TestData.CreateTime()), TestData.CreateLocalization(), TestData.CreateTime());

        viewModel.Accounts.ShouldBeEmpty();
        viewModel.SyncPairs.ShouldBeEmpty();
        viewModel.Conflicts.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_loaded_then_property_changed_is_raised_for_the_collections()
    {
        var viewModel = new SyncOverviewViewModel(new AStarDev.CloudSync.Core.Placeholder.PlaceholderSyncOverviewSource(TestData.CreateTime()), TestData.CreateLocalization(), TestData.CreateTime());
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        changed.ShouldContain(nameof(SyncOverviewViewModel.Accounts));
        changed.ShouldContain(nameof(SyncOverviewViewModel.SyncPairs));
    }
}
