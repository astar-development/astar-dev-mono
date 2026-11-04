using AStarDev.CloudSync.Core;
using AStarDev.CloudSync.ViewModels;

namespace AStarDev.CloudSync.TestsUnit.ViewModels;

public class GivenALayoutBViewModel
{
    [Fact]
    public async Task when_the_overview_loads_then_the_accounts_are_grouped_by_their_provider()
    {
        var layout = await CreateLoadedLayoutAsync();

        var group = layout.ProviderGroups.ShouldHaveSingleItem();
        group.Provider.Name.ShouldBe("OneDrive");
        group.Accounts.Select(account => account.Name).ShouldBe(["Work", "Personal", "Photos", "Archive"]);
        group.CountText.ShouldBe("4");
    }

    [Fact]
    public async Task when_the_overview_loads_then_the_first_account_is_selected()
    {
        var layout = await CreateLoadedLayoutAsync();

        var detail = layout.Detail.ShouldBeOfType<AccountDetailViewModel>();
        detail.Account.Name.ShouldBe("Work");
        layout.SelectedItem.ShouldBeSameAs(detail.Account);
    }

    [Fact]
    public async Task when_an_account_is_selected_then_only_its_sync_pairs_are_shown()
    {
        var layout = await CreateLoadedLayoutAsync();

        var detail = layout.Detail.ShouldBeOfType<AccountDetailViewModel>();

        detail.SyncPairs.Select(pair => pair.LocalPath).ShouldBe(["/home/user/CloudSync/OneDrive/Work/Documents", "/home/user/CloudSync/OneDrive/Work/Projects", "/home/user/CloudSync/OneDrive/Work/Desktop"]);
    }

    [Fact]
    public async Task when_another_account_is_selected_then_the_detail_follows_and_change_is_raised()
    {
        var layout = await CreateLoadedLayoutAsync();
        var changed = new List<string?>();
        layout.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        var photos = layout.ProviderGroups[0].Accounts[2];

        layout.SelectedItem = photos;

        var detail = layout.Detail.ShouldBeOfType<AccountDetailViewModel>();
        detail.Account.ShouldBeSameAs(photos);
        detail.SyncPairs.Select(pair => pair.Name).ShouldBe(["Holiday photos"]);
        changed.ShouldContain(nameof(LayoutBViewModel.Detail));
    }

    [Fact]
    public async Task when_a_provider_group_is_selected_then_the_selected_account_is_kept()
    {
        var layout = await CreateLoadedLayoutAsync();

        layout.SelectedItem = layout.ProviderGroups[0];

        layout.Detail.ShouldBeOfType<AccountDetailViewModel>().Account.Name.ShouldBe("Work");
    }

    [Fact]
    public async Task when_the_overview_has_no_accounts_then_there_is_no_account_detail()
    {
        var source = Substitute.For<ISyncOverviewSource>();
        source.GetOverviewAsync(Arg.Any<CancellationToken>()).Returns(SyncOverviewFactory.Create([], [], [], [], []));
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime());
        var layout = new LayoutBViewModel(overview, TestData.CreateLocalization());

        await overview.LoadAsync(TestContext.Current.CancellationToken);

        layout.ProviderGroups.ShouldBeEmpty();
        layout.Detail.ShouldBeOfType<NoAccountDetailViewModel>().Text.ShouldBe("No accounts connected yet");
    }

    [Fact]
    public async Task when_the_overview_loads_then_the_sidebar_footer_counts_the_syncing_accounts()
    {
        var layout = await CreateLoadedLayoutAsync();

        layout.SyncingSummaryText.ShouldBe("1 account syncing");
    }

    [Fact]
    public async Task when_an_account_is_selected_then_its_summary_figures_are_shown()
    {
        var layout = await CreateLoadedLayoutAsync();

        var detail = layout.Detail.ShouldBeOfType<AccountDetailViewModel>();

        detail.Stats.Select(tile => tile.Label).ShouldBe(["folders", "files", "conflicts", "last sync"]);
        detail.Stats.Select(tile => tile.Value).ShouldBe(["30", "240,686", "0", "4 min ago"]);
    }

    [Fact]
    public async Task when_an_account_is_selected_then_the_header_and_storage_ring_describe_it()
    {
        var layout = await CreateLoadedLayoutAsync();

        var detail = layout.Detail.ShouldBeOfType<AccountDetailViewModel>();

        detail.BreadcrumbText.ShouldBe("OneDrive / Work");
        detail.SubtitleText.ShouldBe("work@example.com · Syncing 25%");
        detail.Progress.ShouldBe(0.25);
        detail.StoragePercentText.ShouldBe("71%");
        detail.StorageSweepAngle.ShouldBe(256.25, 0.01);
    }

    [Fact]
    public async Task when_created_then_the_tab_headers_are_localised()
    {
        var layout = await CreateLoadedLayoutAsync();

        var detail = layout.Detail.ShouldBeOfType<AccountDetailViewModel>();

        new[] { detail.OverviewTabText, detail.FoldersTabText, detail.ActivityTabText, detail.ConflictsTabText }.ShouldBe(["Overview", "Folders", "Activity", "Conflicts"]);
    }

    private static async Task<LayoutBViewModel> CreateLoadedLayoutAsync()
    {
        var overview = new SyncOverviewViewModel(new AStarDev.CloudSync.Core.Placeholder.PlaceholderSyncOverviewSource(TestData.CreateTime()), TestData.CreateLocalization(), TestData.CreateTime());
        var layout = new LayoutBViewModel(overview, TestData.CreateLocalization());
        await overview.LoadAsync(TestContext.Current.CancellationToken);

        return layout;
    }
}
