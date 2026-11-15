using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Search;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Search;

public class GivenSearchMatching
{
    [Theory]
    [InlineData("Holiday photos", "photos", true)]
    [InlineData("Holiday photos", "  PHOTOS  ", true)]
    [InlineData("Holiday photos", "", true)]
    [InlineData("Holiday photos", "   ", true)]
    [InlineData("Holiday photos", "documents", false)]
    public void when_text_is_searched_then_it_matches_ignoring_case_and_surrounding_spaces_and_a_blank_query_matches_everything(string text, string query, bool expected) => text.MatchesSearch(query).ShouldBe(expected);

    [Theory]
    [InlineData("work", true)]
    [InlineData("WORK@EXAMPLE", true)]
    [InlineData("onedrive", true)]
    [InlineData("recipes", false)]
    public async Task when_an_account_is_searched_then_its_name_email_or_provider_matches(string query, bool expected)
    {
        var account = (await TestData.CreateLoadedOverviewViewModelAsync()).Accounts.Single(candidate => candidate.Name == "Work");

        account.MatchesSearch(query).ShouldBe(expected);
    }

    [Theory]
    [InlineData("recipes", true)]
    [InlineData(" RECIPES ", true)]
    [InlineData("work", false)]
    public async Task when_a_sync_pair_is_searched_then_its_name_or_paths_match(string query, bool expected)
    {
        var pair = (await TestData.CreateLoadedOverviewViewModelAsync()).SyncPairs.Single(candidate => candidate.Name == "Recipes");

        pair.MatchesSearch(query).ShouldBe(expected);
    }
}
