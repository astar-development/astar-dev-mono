using AStarDev.CloudSync.Core;
using AStarDev.CloudSync.ViewModels;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.TestsUnit.ViewModels;

public class GivenALayoutBViewModel
{
    [Fact]
    public async Task when_the_connect_provider_command_runs_then_an_account_is_added()
    {
        var syncActions = new RecordingSyncActions();
        var layout = new LayoutBViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateLocalPathFormatter(), syncActions, TestData.CreateLocalization());

        await layout.ConnectProviderCommand.ExecuteAsync(null);

        syncActions.Calls.ShouldBe(["AddAccountAsync"]);
    }

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
    public async Task when_an_account_is_selected_then_its_synced_folders_show_local_paths_relative_to_the_home_folder()
    {
        var layout = await CreateLoadedLayoutAsync();

        var detail = layout.Detail.ShouldBeOfType<AccountDetailViewModel>();

        detail.SyncedFolders.Select(folder => folder.LocalPathText).ShouldBe(["~/CloudSync/OneDrive/Work/Documents", "~/CloudSync/OneDrive/Work/Projects", "~/CloudSync/OneDrive/Work/Desktop"]);
    }

    [Fact]
    public async Task when_an_account_is_selected_then_its_activity_comes_only_from_that_account_newest_first()
    {
        var layout = await CreateLoadedLayoutAsync();

        var detail = layout.Detail.ShouldBeOfType<AccountDetailViewModel>();

        detail.RecentActivity.Select(item => item.Text).ShouldBe(["Detecting local changes…", "Uploaded 12 files (84.0 MB)", "Authenticated"]);
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
        source.GetOverviewAsync(Arg.Any<CancellationToken>()).Returns(SyncOverviewFactory.Create([], [], [], [], [], 0, Option.None<DateTimeOffset>()));
        var overview = new SyncOverviewViewModel(source, new RecordingSyncActions(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        var layout = new LayoutBViewModel(overview, TestData.CreateLocalPathFormatter(), new RecordingSyncActions(), TestData.CreateLocalization());

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

    [Fact]
    public async Task when_the_search_is_blank_then_every_account_and_folder_is_shown()
    {
        var layout = await CreateLoadedLayoutAsync();

        layout.SearchText = "   ";

        layout.ProviderGroups.ShouldHaveSingleItem().Accounts.Select(account => account.Name).ShouldBe(["Work", "Personal", "Photos", "Archive"]);
        layout.Detail.ShouldBeOfType<AccountDetailViewModel>().SyncPairs.Select(pair => pair.Name).ShouldBe(["Documents", "Projects", "Desktop"]);
    }

    [Fact]
    public async Task when_the_search_matches_an_account_name_ignoring_case_and_spaces_then_only_that_account_is_shown()
    {
        var layout = await CreateLoadedLayoutAsync();

        layout.SearchText = "  PHOTOS ";

        var group = layout.ProviderGroups.ShouldHaveSingleItem();
        group.Accounts.Select(account => account.Name).ShouldBe(["Photos"]);
        group.CountText.ShouldBe("1");
    }

    [Fact]
    public async Task when_the_search_matches_a_folder_then_its_account_is_shown_with_only_the_matching_folders()
    {
        var layout = await CreateLoadedLayoutAsync();

        layout.SearchText = "projects";

        layout.ProviderGroups.ShouldHaveSingleItem().Accounts.Select(account => account.Name).ShouldBe(["Work"]);
        layout.Detail.ShouldBeOfType<AccountDetailViewModel>().SyncPairs.Select(pair => pair.Name).ShouldBe(["Projects"]);
    }

    [Fact]
    public async Task when_the_search_matches_the_account_then_all_of_its_folders_are_shown()
    {
        var layout = await CreateLoadedLayoutAsync();

        layout.SearchText = "work@example.com";

        layout.Detail.ShouldBeOfType<AccountDetailViewModel>().SyncPairs.Select(pair => pair.Name).ShouldBe(["Documents", "Projects", "Desktop"]);
    }

    [Fact]
    public async Task when_the_search_matches_the_provider_name_then_every_account_is_shown()
    {
        var layout = await CreateLoadedLayoutAsync();

        layout.SearchText = "onedrive";

        layout.ProviderGroups.ShouldHaveSingleItem().Accounts.Count.ShouldBe(4);
    }

    [Fact]
    public async Task when_the_selected_account_is_filtered_out_then_the_first_matching_account_is_selected()
    {
        var layout = await CreateLoadedLayoutAsync();

        layout.SearchText = "recipes";

        var detail = layout.Detail.ShouldBeOfType<AccountDetailViewModel>();
        detail.Account.Name.ShouldBe("Personal");
        detail.SyncPairs.Select(pair => pair.Name).ShouldBe(["Recipes"]);
        layout.SelectedItem.ShouldBeSameAs(detail.Account);
    }

    [Fact]
    public async Task when_the_selected_account_still_matches_then_it_stays_selected()
    {
        var layout = await CreateLoadedLayoutAsync();
        var photos = layout.ProviderGroups[0].Accounts[2];
        layout.SelectedItem = photos;

        layout.SearchText = "example.com";

        layout.Detail.ShouldBeOfType<AccountDetailViewModel>().Account.ShouldBeSameAs(photos);
    }

    [Fact]
    public async Task when_the_search_matches_nothing_then_a_no_matches_message_is_shown()
    {
        var layout = await CreateLoadedLayoutAsync();

        layout.SearchText = "no such thing";

        layout.ProviderGroups.ShouldBeEmpty();
        layout.Detail.ShouldBeOfType<NoMatchingAccountsDetailViewModel>().Text.ShouldBe("No accounts or folders match your search");
    }

    [Fact]
    public async Task when_the_search_is_cleared_then_everything_is_shown_again_and_the_selection_is_kept()
    {
        var layout = await CreateLoadedLayoutAsync();
        layout.SearchText = "recipes";

        layout.SearchText = string.Empty;

        layout.ProviderGroups.ShouldHaveSingleItem().Accounts.Count.ShouldBe(4);
        var detail = layout.Detail.ShouldBeOfType<AccountDetailViewModel>();
        detail.Account.Name.ShouldBe("Personal");
        detail.SyncPairs.Select(pair => pair.Name).ShouldBe(["Recipes", "Music"]);
    }

    [Fact]
    public async Task when_the_search_changes_then_the_search_and_the_tree_raise_change()
    {
        var layout = await CreateLoadedLayoutAsync();
        var changed = new List<string?>();
        layout.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        layout.SearchText = "work";

        changed.ShouldContain(nameof(LayoutBViewModel.SearchText));
        changed.ShouldContain(nameof(LayoutBViewModel.ProviderGroups));
    }

    private static async Task<LayoutBViewModel> CreateLoadedLayoutAsync()
    {
        var overview = new SyncOverviewViewModel(new AStarDev.CloudSync.Core.Placeholder.PlaceholderSyncOverviewSource(TestData.CreateTime()), new RecordingSyncActions(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        var layout = new LayoutBViewModel(overview, TestData.CreateLocalPathFormatter(), new RecordingSyncActions(), TestData.CreateLocalization());
        await overview.LoadAsync(TestContext.Current.CancellationToken);

        return layout;
    }
}
