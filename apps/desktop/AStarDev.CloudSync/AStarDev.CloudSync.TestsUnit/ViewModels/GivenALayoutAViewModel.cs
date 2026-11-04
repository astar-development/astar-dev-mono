using AStarDev.CloudSync.Core;
using AStarDev.CloudSync.ViewModels;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.TestsUnit.ViewModels;

public class GivenALayoutAViewModel
{
    [Fact]
    public async Task when_the_overview_loads_then_the_provider_chips_are_all_plus_each_provider_present()
    {
        var layout = await CreateLoadedLayoutAsync();

        layout.ProviderChips.Select(chip => chip.Label).ShouldBe(["All", "OneDrive"]);
    }

    [Fact]
    public void when_the_overview_has_no_accounts_then_only_the_all_chip_is_shown()
    {
        var layout = new LayoutAViewModel(new SyncOverviewViewModel(Substitute.For<ISyncOverviewSource>(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance), TestData.CreateLocalization());

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
        source.GetOverviewAsync(Arg.Any<CancellationToken>()).Returns(SyncOverviewFactory.Create([TestData.CreateAccount(SyncStatusFactory.CreateSyncing(50), Option.None<DateTimeOffset>())], [], [], [], [], 0));
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        var layout = new LayoutAViewModel(overview, TestData.CreateLocalization());

        await overview.LoadAsync(TestContext.Current.CancellationToken);

        layout.StatusSummaryText.ShouldBe("Syncing 1 of 1 account");
    }

    [Fact]
    public async Task when_no_account_is_syncing_then_the_status_summary_says_so()
    {
        var source = Substitute.For<ISyncOverviewSource>();
        source.GetOverviewAsync(Arg.Any<CancellationToken>()).Returns(SyncOverviewFactory.Create([TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>())], [], [], [], [], 0));
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        var layout = new LayoutAViewModel(overview, TestData.CreateLocalization());

        await overview.LoadAsync(TestContext.Current.CancellationToken);

        layout.StatusSummaryText.ShouldBe("No accounts syncing");
    }

    [Fact]
    public void when_created_then_the_pane_and_rail_text_is_localised()
    {
        var layout = new LayoutAViewModel(new SyncOverviewViewModel(Substitute.For<ISyncOverviewSource>(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance), TestData.CreateLocalization());

        layout.AccountsTitle.ShouldBe("Accounts");
        layout.AddAccountText.ShouldBe("+ Add account");
        layout.DashboardText.ShouldBe("Dashboard");
        layout.FoldersText.ShouldBe("Folders");
        layout.ActivityText.ShouldBe("Activity");
        layout.ConflictsText.ShouldBe("Conflicts");
        layout.SearchText.ShouldBe("Search");
        layout.SettingsText.ShouldBe("Settings");
    }

    private static async Task<LayoutAViewModel> CreateLoadedLayoutAsync()
    {
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();

        return new LayoutAViewModel(overview, TestData.CreateLocalization());
    }
}
