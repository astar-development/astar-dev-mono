using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.CloudSync.ViewModels.Search;
using AStarDev.CloudSync.ViewModels.SyncPairs;
using AStarDev.CloudSync.ViewModels.TestsUnit.Overview;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Search;

public class GivenSearchResults
{
    [Fact]
    public async Task when_the_search_is_blank_then_every_account_and_folder_is_listed()
    {
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();

        var results = new SearchResultsViewModel(overview, _ => { }, _ => { }, TestData.CreateLocalization());

        (results.Accounts.Count, results.Folders.Count, results.IsEmpty).ShouldBe((4, 7, false));
    }

    [Fact]
    public async Task when_a_folder_name_is_searched_then_only_the_matching_folders_are_listed_and_no_accounts()
    {
        var results = new SearchResultsViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), _ => { }, _ => { }, TestData.CreateLocalization());

        results.SearchText = "  recipes ";

        results.Accounts.ShouldBeEmpty();
        results.Folders.Select(folder => folder.Name).ShouldBe(["Recipes"]);
    }

    [Fact]
    public async Task when_an_account_email_is_searched_then_only_that_account_is_listed()
    {
        var results = new SearchResultsViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), _ => { }, _ => { }, TestData.CreateLocalization());

        results.SearchText = "ARCHIVE@";

        results.Accounts.Select(account => account.Name).ShouldBe(["Archive"]);
        results.Folders.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_nothing_matches_then_the_results_are_empty_with_the_no_results_text()
    {
        var results = new SearchResultsViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), _ => { }, _ => { }, TestData.CreateLocalization());

        results.SearchText = "zzz-no-such-thing";

        (results.IsEmpty, results.NoResultsText).ShouldBe((true, "Nothing matches your search"));
    }

    [Fact]
    public async Task when_the_search_text_changes_then_property_changed_is_raised_for_the_results()
    {
        var results = new SearchResultsViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), _ => { }, _ => { }, TestData.CreateLocalization());
        var changed = new List<string?>();
        results.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        results.SearchText = "recipes";

        changed.ShouldContain(nameof(SearchResultsViewModel.Accounts));
        changed.ShouldContain(nameof(SearchResultsViewModel.Folders));
        changed.ShouldContain(nameof(SearchResultsViewModel.IsEmpty));
    }

    [Fact]
    public async Task when_the_overview_loads_again_then_the_results_follow_the_search_text()
    {
        var overview = new SyncOverviewViewModel(new CountingSyncOverviewSource(await TestData.CreateOverviewAsync()), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        var results = new SearchResultsViewModel(overview, _ => { }, _ => { }, TestData.CreateLocalization());
        results.SearchText = "recipes";

        await overview.LoadAsync(TestContext.Current.CancellationToken);

        results.Folders.Select(folder => folder.Name).ShouldBe(["Recipes"]);
    }

    [Fact]
    public async Task when_the_search_is_given_a_null_then_it_is_treated_as_blank()
    {
        var results = new SearchResultsViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), _ => { }, _ => { }, TestData.CreateLocalization());

        results.SearchText = null!;

        (results.SearchText, results.Folders.Count).ShouldBe((string.Empty, 7));
    }

    [Fact]
    public async Task when_an_account_result_is_opened_then_that_account_is_passed_to_the_open_callback()
    {
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();
        AccountViewModel? opened = null;
        var results = new SearchResultsViewModel(overview, account => opened = account, _ => { }, TestData.CreateLocalization());

        results.OpenAccountCommand.Execute(overview.Accounts[1]);

        opened.ShouldBeSameAs(overview.Accounts[1]);
    }

    [Fact]
    public async Task when_a_folder_result_is_opened_then_that_folder_is_passed_to_the_open_callback()
    {
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();
        SyncPairViewModel? opened = null;
        var results = new SearchResultsViewModel(overview, _ => { }, pair => opened = pair, TestData.CreateLocalization());

        results.OpenFolderCommand.Execute(overview.SyncPairs[2]);

        opened.ShouldBeSameAs(overview.SyncPairs[2]);
    }
}
