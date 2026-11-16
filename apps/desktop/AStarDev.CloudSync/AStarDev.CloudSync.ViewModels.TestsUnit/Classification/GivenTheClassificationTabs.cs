using AStarDev.CloudSync.Classification;
using AStarDev.CloudSync.TestHelpers.Classification;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Classification;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Classification;

public class GivenTheClassificationTabs
{
    private readonly CategoryId colours = ClassificationTestData.NewCategoryId();
    private readonly GetAllCountingTermStore terms = new();
    private readonly FakeClassificationSuggestions service = new(SuggestionTestData.Plain("sunset", 12), SuggestionTestData.Plain("harbour", 3));

    private ClassificationTabsViewModel Create()
    {
        var localization = TestData.CreateLocalization();
        var categories = new FakeCategoryStore(ClassificationTestData.Category(colours, "Colour"));
        var termList = new ClassificationTermListViewModel(terms, categories, new FakeFileClassificationStore(), localization);

        return new ClassificationTabsViewModel(termList, new ClassificationSuggestionsViewModel(service, categories, localization), localization);
    }

    [Fact]
    public void when_created_then_the_terms_tab_is_selected_with_both_headers_and_nothing_is_scanned()
    {
        var tabs = Create();

        (tabs.Selected, tabs.IsTermsSelected, tabs.IsSuggestionsSelected, tabs.TermsTabText, tabs.SuggestionsTabText, service.ScanCount).ShouldBe((ClassificationTabFactory.CreateTerms(), true, false, "Terms", "Suggestions", 0));
    }

    [Fact]
    public async Task when_loaded_then_the_term_list_is_read_and_the_suggestions_are_not_scanned()
    {
        var tabs = Create();

        await tabs.LoadCommand.ExecuteAsync(null);

        (terms.GetAllCount, service.ScanCount).ShouldBe((1, 0));
    }

    [Fact]
    public async Task when_the_suggestions_tab_is_shown_then_it_scans_once_even_when_shown_again()
    {
        var tabs = Create();

        await tabs.ShowSuggestionsCommand.ExecuteAsync(null);
        await tabs.ShowTermsCommand.ExecuteAsync(null);
        await tabs.ShowSuggestionsCommand.ExecuteAsync(null);

        (tabs.Selected, service.ScanCount, tabs.Suggestions.Rows.Count).ShouldBe((ClassificationTabFactory.CreateSuggestions(), 1, 2));
    }

    [Fact]
    public async Task when_the_terms_tab_is_shown_without_a_change_then_the_term_list_is_not_reloaded()
    {
        var tabs = Create();
        await tabs.LoadCommand.ExecuteAsync(null);
        await tabs.ShowSuggestionsCommand.ExecuteAsync(null);

        await tabs.ShowTermsCommand.ExecuteAsync(null);

        terms.GetAllCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_word_is_promoted_then_the_term_list_reloads_once_when_the_terms_tab_is_shown_and_lists_the_new_term()
    {
        var tabs = Create();
        await tabs.LoadCommand.ExecuteAsync(null);
        await tabs.ShowSuggestionsCommand.ExecuteAsync(null);
        await tabs.Suggestions.PromoteCommand.ExecuteAsync(tabs.Suggestions.Rows[0]);
        await tabs.Suggestions.ChooseCategoryCommand.ExecuteAsync(tabs.Suggestions.Categories[0]);
        terms.Terms.Add(ClassificationTestData.Term("sunset", colours));
        var reloadedBeforeShowing = terms.GetAllCount;

        await tabs.ShowTermsCommand.ExecuteAsync(null);
        await tabs.ShowTermsCommand.ExecuteAsync(null);

        (reloadedBeforeShowing, terms.GetAllCount, string.Join(',', tabs.Terms.Rows.Select(row => row.Text))).ShouldBe((1, 2, "sunset"));
    }

    [Fact]
    public async Task when_a_word_is_ignored_then_the_term_list_reloads_when_the_terms_tab_is_shown()
    {
        var tabs = Create();
        await tabs.LoadCommand.ExecuteAsync(null);
        await tabs.ShowSuggestionsCommand.ExecuteAsync(null);
        await tabs.Suggestions.IgnoreCommand.ExecuteAsync(tabs.Suggestions.Rows[0]);
        await tabs.Suggestions.ChooseCategoryCommand.ExecuteAsync(tabs.Suggestions.Categories[0]);

        await tabs.ShowTermsCommand.ExecuteAsync(null);

        terms.GetAllCount.ShouldBe(2);
    }
}
