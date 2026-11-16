using AStarDev.CloudSync.Classification;
using AStarDev.CloudSync.TestHelpers.Classification;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Classification;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Classification;

public class GivenTheClassificationSuggestions
{
    private readonly CategoryId colours = ClassificationTestData.NewCategoryId();
    private readonly CategoryId people = ClassificationTestData.NewCategoryId();

    private ClassificationSuggestionsViewModel Create(FakeClassificationSuggestions service) => Create(service, new FakeCategoryStore(ClassificationTestData.Category(colours, "Colour"), ClassificationTestData.Category(people, "Person")));

    private static ClassificationSuggestionsViewModel Create(FakeClassificationSuggestions service, ICategoryStore categories) => new(service, categories, TestData.CreateLocalization());

    private async Task<ClassificationSuggestionsViewModel> LoadedAsync(FakeClassificationSuggestions service)
    {
        var list = Create(service);
        await list.LoadCommand.ExecuteAsync(null);

        return list;
    }

    private static FakeClassificationSuggestions SomeSuggestions() => new(SuggestionTestData.Plain("sunset", 1234), SuggestionTestData.FirstName("alice", 56), SuggestionTestData.Plain("harbour", 7));

    [Fact]
    public void when_created_then_it_is_loading_with_no_rows_and_no_picker()
    {
        var list = Create(SomeSuggestions());

        (list.State, list.IsLoading, list.Rows.Count, list.IsPickerOpen).ShouldBe((SuggestionsLoadStateFactory.CreateLoading(), true, 0, false));
    }

    [Fact]
    public async Task when_loaded_then_rows_keep_the_service_rank_with_count_and_hint()
    {
        var list = await LoadedAsync(SomeSuggestions());

        list.State.ShouldBe(SuggestionsLoadStateFactory.CreateLoaded());
        list.Rows.Select(row => (row.Word, row.FileCount, row.FileCountText, row.IsFirstName, row.HintText)).ShouldBe([("sunset", 1234, "1,234", false, string.Empty), ("alice", 56, "56", true, "Possibly a first name"), ("harbour", 7, "7", false, string.Empty)]);
    }

    [Fact]
    public async Task when_there_are_no_suggestions_then_the_list_is_empty_with_the_empty_text()
    {
        var list = await LoadedAsync(new FakeClassificationSuggestions());

        (list.IsEmpty, list.HasNoMatches, list.IsLoading, list.IsFailed, list.EmptyText).ShouldBe((true, false, false, false, "No new words to suggest"));
    }

    [Fact]
    public async Task when_the_scan_fails_then_the_list_has_failed_with_no_rows()
    {
        var service = SomeSuggestions();
        service.ScanOutcome = Result.Failure<IReadOnlyList<ClassificationSuggestion>, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable());

        var list = await LoadedAsync(service);

        (list.IsFailed, list.IsCancelled, list.Rows.Count, list.LoadFailedText).ShouldBe((true, false, 0, "The suggestions could not be read."));
    }

    [Fact]
    public async Task when_the_scan_is_running_then_it_is_loading_and_can_be_cancelled()
    {
        var service = SomeSuggestions();
        service.ScanGate = new TaskCompletionSource();
        var list = Create(service);

        var running = list.LoadCommand.ExecuteAsync(null);

        (list.IsLoading, list.CancelCommand.CanExecute(null), list.LoadingText).ShouldBe((true, true, "Scanning your files for new words"));
        service.ScanGate.SetResult();
        await running;
        list.CancelCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_scan_is_cancelled_then_it_is_a_cancelled_state_and_not_a_failure()
    {
        var service = SomeSuggestions();
        service.ScanGate = new TaskCompletionSource();
        var list = Create(service);
        var running = list.LoadCommand.ExecuteAsync(null);

        list.CancelCommand.Execute(null);
        await running;

        (list.State, list.IsCancelled, list.IsFailed, list.IsLoading, list.Rows.Count, list.CancelledText).ShouldBe((SuggestionsLoadStateFactory.CreateCancelled(), true, false, false, 0, "The scan was cancelled."));
    }

    [Fact]
    public async Task when_refreshed_after_a_cancel_then_the_scan_runs_again()
    {
        var service = SomeSuggestions();
        service.ScanGate = new TaskCompletionSource();
        var list = Create(service);
        var cancelled = list.LoadCommand.ExecuteAsync(null);
        list.CancelCommand.Execute(null);
        await cancelled;
        service.ScanGate = null;

        await list.RefreshCommand.ExecuteAsync(null);

        (service.ScanCount, list.State, list.Rows.Count).ShouldBe((2, SuggestionsLoadStateFactory.CreateLoaded(), 3));
    }

    [Fact]
    public async Task when_the_filter_is_typed_then_only_matching_words_are_listed_ignoring_case_and_spaces()
    {
        var list = await LoadedAsync(SomeSuggestions());

        list.FilterText = "  HAR ";

        string.Join(',', list.Rows.Select(row => row.Word)).ShouldBe("harbour");
    }

    [Fact]
    public async Task when_the_filter_matches_nothing_then_the_no_matches_state_shows_and_clearing_it_restores_the_rows()
    {
        var list = await LoadedAsync(SomeSuggestions());

        list.FilterText = "zzz";
        var hidden = (list.HasNoMatches, list.IsEmpty, list.Rows.Count, list.NoMatchesText);
        list.FilterText = string.Empty;

        (hidden, list.HasNoMatches, list.Rows.Count).ShouldBe(((true, false, 0, "No suggestions match your filter"), false, 3));
    }

    [Fact]
    public async Task when_promote_is_chosen_then_the_picker_opens_with_every_category_and_nothing_is_assigned()
    {
        var service = SomeSuggestions();
        var list = await LoadedAsync(service);

        await list.PromoteCommand.ExecuteAsync(list.Rows[1]);

        (list.IsPickerOpen, list.PickerAction, list.PickerTitleText, string.Join(',', list.Categories.Select(category => category.Name)), service.Promoted.Count, service.Ignored.Count).ShouldBe((true, SuggestionActionFactory.CreatePromote(), "Promote \"alice\" to a category", "Colour,Person", 0, 0));
    }

    [Fact]
    public async Task when_the_category_filter_is_typed_then_only_matching_categories_are_listed()
    {
        var list = await LoadedAsync(SomeSuggestions());
        await list.PromoteCommand.ExecuteAsync(list.Rows[0]);

        list.CategoryFilterText = " per";

        (string.Join(',', list.Categories.Select(category => category.Name)), list.HasNoCategoryMatches).ShouldBe(("Person", false));
    }

    [Fact]
    public async Task when_the_category_filter_matches_nothing_then_the_no_category_matches_state_shows()
    {
        var list = await LoadedAsync(SomeSuggestions());
        await list.PromoteCommand.ExecuteAsync(list.Rows[0]);

        list.CategoryFilterText = "zzz";

        (list.Categories.Count, list.HasNoCategoryMatches, list.NoCategoryMatchesText).ShouldBe((0, true, "No categories match your filter"));
    }

    [Fact]
    public async Task when_the_picker_is_reopened_then_the_category_filter_is_cleared()
    {
        var list = await LoadedAsync(SomeSuggestions());
        await list.PromoteCommand.ExecuteAsync(list.Rows[0]);
        list.CategoryFilterText = "per";
        list.ClosePickerCommand.Execute(null);

        await list.PromoteCommand.ExecuteAsync(list.Rows[0]);

        (list.CategoryFilterText, list.Categories.Count).ShouldBe((string.Empty, 2));
    }

    [Fact]
    public async Task when_there_are_no_categories_then_the_picker_says_so()
    {
        var list = Create(SomeSuggestions(), new FakeCategoryStore());
        await list.LoadCommand.ExecuteAsync(null);

        await list.PromoteCommand.ExecuteAsync(list.Rows[0]);

        (list.HasNoCategories, list.NoCategoriesText).ShouldBe((true, "No categories yet"));
    }

    [Fact]
    public async Task when_the_categories_cannot_be_read_then_the_picker_shows_the_failure_and_no_categories()
    {
        var list = Create(SomeSuggestions(), new FailingCategoryStore());
        await list.LoadCommand.ExecuteAsync(null);

        await list.PromoteCommand.ExecuteAsync(list.Rows[0]);

        (list.IsPickerOpen, list.IsCategoriesFailed, list.Categories.Count, list.CategoriesFailedText).ShouldBe((true, true, 0, "The categories could not be read."));
    }

    [Fact]
    public async Task when_a_category_is_chosen_then_the_word_is_promoted_removed_locally_and_not_rescanned()
    {
        var service = SomeSuggestions();
        var list = await LoadedAsync(service);
        await list.PromoteCommand.ExecuteAsync(list.Rows[1]);

        await list.ChooseCategoryCommand.ExecuteAsync(list.Categories[1]);

        (service.Promoted.Single() == ("alice", people), service.ScanCount, string.Join(',', list.Rows.Select(row => row.Word)), list.IsPickerOpen).ShouldBe((true, 1, "sunset,harbour", false));
    }

    [Fact]
    public async Task when_a_word_is_promoted_under_a_filter_then_the_filter_stays_and_the_other_rows_remain_held()
    {
        var list = await LoadedAsync(SomeSuggestions());
        list.FilterText = "a";
        await list.PromoteCommand.ExecuteAsync(list.Rows.Single(row => row.Word == "alice"));
        await list.ChooseCategoryCommand.ExecuteAsync(list.Categories[1]);

        var filtered = string.Join(',', list.Rows.Select(row => row.Word));
        list.FilterText = string.Empty;

        (filtered, string.Join(',', list.Rows.Select(row => row.Word))).ShouldBe(("harbour", "sunset,harbour"));
    }

    [Fact]
    public async Task when_the_last_word_is_promoted_then_the_list_is_empty()
    {
        var list = await LoadedAsync(new FakeClassificationSuggestions(SuggestionTestData.Plain("sunset", 3)));
        await list.PromoteCommand.ExecuteAsync(list.Rows[0]);

        await list.ChooseCategoryCommand.ExecuteAsync(list.Categories[0]);

        (list.IsEmpty, list.Rows.Count).ShouldBe((true, 0));
    }

    [Fact]
    public async Task when_a_word_is_promoted_then_terms_changed_is_raised_once()
    {
        var list = await LoadedAsync(SomeSuggestions());
        var raised = 0;
        list.TermsChanged += (_, _) => raised++;
        await list.PromoteCommand.ExecuteAsync(list.Rows[0]);

        await list.ChooseCategoryCommand.ExecuteAsync(list.Categories[0]);

        raised.ShouldBe(1);
    }

    [Fact]
    public async Task when_promote_fails_then_the_row_stays_with_an_error_and_terms_changed_is_not_raised()
    {
        var service = SomeSuggestions();
        service.FailPromote = true;
        var list = await LoadedAsync(service);
        var raised = 0;
        list.TermsChanged += (_, _) => raised++;
        var row = list.Rows[0];
        await list.PromoteCommand.ExecuteAsync(row);

        await list.ChooseCategoryCommand.ExecuteAsync(list.Categories[0]);

        (list.Rows.Count, row.HasError, row.ErrorText, list.IsPickerOpen, raised).ShouldBe((3, true, "The word could not be changed.", false, 0));
    }

    [Fact]
    public async Task when_a_failed_word_is_tried_again_then_its_error_is_cleared()
    {
        var service = SomeSuggestions();
        service.FailPromote = true;
        var list = await LoadedAsync(service);
        var row = list.Rows[0];
        await list.PromoteCommand.ExecuteAsync(row);
        await list.ChooseCategoryCommand.ExecuteAsync(list.Categories[0]);

        await list.PromoteCommand.ExecuteAsync(row);

        row.HasError.ShouldBeFalse();
    }

    [Fact]
    public async Task when_ignore_is_chosen_then_the_picker_opens_for_ignore_and_the_choice_ignores_the_word()
    {
        var service = SomeSuggestions();
        var list = await LoadedAsync(service);
        var raised = 0;
        list.TermsChanged += (_, _) => raised++;

        await list.IgnoreCommand.ExecuteAsync(list.Rows[0]);
        var title = (list.PickerAction, list.PickerTitleText);
        await list.ChooseCategoryCommand.ExecuteAsync(list.Categories[0]);

        (title, service.Ignored.Single() == ("sunset", colours), service.Promoted.Count, service.ScanCount, string.Join(',', list.Rows.Select(row => row.Word)), raised).ShouldBe(((SuggestionActionFactory.CreateIgnore(), "Choose a category for the ignored word \"sunset\""), true, 0, 1, "alice,harbour", 1));
    }

    [Fact]
    public async Task when_ignore_fails_then_the_row_stays_with_an_error()
    {
        var service = SomeSuggestions();
        service.FailIgnore = true;
        var list = await LoadedAsync(service);
        await list.IgnoreCommand.ExecuteAsync(list.Rows[0]);

        await list.ChooseCategoryCommand.ExecuteAsync(list.Categories[0]);

        (list.Rows.Count, list.Rows[0].HasError).ShouldBe((3, true));
    }

    [Fact]
    public async Task when_the_picker_is_closed_then_nothing_changes()
    {
        var service = SomeSuggestions();
        var list = await LoadedAsync(service);
        await list.PromoteCommand.ExecuteAsync(list.Rows[0]);

        list.ClosePickerCommand.Execute(null);

        (list.IsPickerOpen, list.Rows.Count, service.Promoted.Count, service.Ignored.Count).ShouldBe((false, 3, 0, 0));
    }

    [Fact]
    public async Task when_a_category_is_chosen_with_no_picker_open_then_nothing_is_called()
    {
        var service = SomeSuggestions();
        var list = await LoadedAsync(service);

        await list.ChooseCategoryCommand.ExecuteAsync(ClassificationTestData.Category(colours, "Colour"));

        (service.Promoted.Count, service.Ignored.Count, list.Rows.Count).ShouldBe((0, 0, 3));
    }

    [Fact]
    public async Task when_refreshed_then_the_scan_runs_again_and_replaces_the_held_rows()
    {
        var service = SomeSuggestions();
        var list = await LoadedAsync(service);
        service.ScanOutcome = Result.Success<IReadOnlyList<ClassificationSuggestion>, ClassificationFailure>([SuggestionTestData.Plain("meadow", 2)]);

        await list.RefreshCommand.ExecuteAsync(null);

        (service.ScanCount, string.Join(',', list.Rows.Select(row => row.Word))).ShouldBe((2, "meadow"));
    }
}
