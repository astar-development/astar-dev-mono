using AStarDev.CloudSync.Classification;
using AStarDev.CloudSync.TestHelpers.Classification;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Classification;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Classification;

public class GivenTheClassificationTermList
{
    private readonly CategoryId colours = ClassificationTestData.NewCategoryId();
    private readonly CategoryId cars = ClassificationTestData.NewCategoryId();
    private readonly FakeFileClassificationStore files = new();

    private ClassificationTermListViewModel Create(FakeTermStore terms) => new(terms, new FakeCategoryStore(ClassificationTestData.Category(colours, "Colour"), ClassificationTestData.Category(cars, "Car")), files, TestData.CreateLocalization());

    private async Task<ClassificationTermListViewModel> LoadedAsync(FakeTermStore terms)
    {
        var list = Create(terms);
        await list.LoadCommand.ExecuteAsync(null);

        return list;
    }

    [Fact]
    public void when_created_then_it_is_loading_and_has_no_rows()
    {
        var list = Create(new FakeTermStore());

        (list.State, list.IsLoading, list.Rows.Count).ShouldBe((TermListLoadStateFactory.CreateLoading(), true, 0));
    }

    [Fact]
    public async Task when_loaded_then_rows_show_term_category_source_and_count_sorted_by_term()
    {
        var red = ClassificationTestData.Term("red", colours);
        var audi = ClassificationTestData.Term("audi", cars, TermSourceFactory.CreateUser(), TermStatusFactory.CreateIgnored());
        files.Counts[red.Id] = 1234;

        var list = await LoadedAsync(new FakeTermStore(red, audi));

        list.State.ShouldBe(TermListLoadStateFactory.CreateLoaded());
        list.Rows.Select(row => (row.Text, row.CategoryName, row.SourceText, row.FileCount, row.FileCountText, row.IsIgnored)).ShouldBe([("audi", "Car", "Added by you", 0, "0", true), ("red", "Colour", "Built in", 1234, "1,234", false)]);
    }

    [Fact]
    public async Task when_there_are_no_terms_then_the_list_is_empty_with_the_empty_text()
    {
        var list = await LoadedAsync(new FakeTermStore());

        (list.IsEmpty, list.HasNoMatches, list.IsLoading, list.IsFailed, list.EmptyText).ShouldBe((true, false, false, false, "No terms yet"));
    }

    [Fact]
    public async Task when_the_terms_cannot_be_read_then_the_list_has_failed_with_no_rows()
    {
        var terms = new FakeTermStore(ClassificationTestData.Term("red", colours)) { GetAllOutcome = Result.Failure<IReadOnlyList<ClassificationTerm>, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable()) };

        var list = await LoadedAsync(terms);

        (list.State, list.IsFailed, list.IsEmpty, list.Rows.Count, list.LoadFailedText).ShouldBe((TermListLoadStateFactory.CreateFailed(), true, false, 0, "The terms could not be read."));
    }

    [Fact]
    public async Task when_the_file_counts_cannot_be_read_then_the_list_has_failed()
    {
        files.FailGetFileCounts = true;

        var list = await LoadedAsync(new FakeTermStore(ClassificationTestData.Term("red", colours)));

        (list.IsFailed, list.Rows.Count).ShouldBe((true, 0));
    }

    [Fact]
    public async Task when_a_term_has_no_known_category_then_the_unknown_category_text_is_shown()
    {
        var list = await LoadedAsync(new FakeTermStore(ClassificationTestData.Term("red", ClassificationTestData.NewCategoryId())));

        list.Rows.Single().CategoryName.ShouldBe("Unknown category");
    }

    [Fact]
    public async Task when_the_filter_is_typed_then_rows_whose_term_contains_it_ignoring_case_are_listed()
    {
        var list = await LoadedAsync(new FakeTermStore(ClassificationTestData.Term("dark red", colours), ClassificationTestData.Term("red", colours), ClassificationTestData.Term("blue", colours)));

        list.FilterText = "RED";

        list.Rows.Select(row => row.Text).ShouldBe(["dark red", "red"]);
    }

    [Fact]
    public async Task when_the_filter_names_a_category_then_its_terms_are_listed()
    {
        var list = await LoadedAsync(new FakeTermStore(ClassificationTestData.Term("red", colours), ClassificationTestData.Term("audi", cars)));

        list.FilterText = "car";

        list.Rows.Select(row => row.Text).ShouldBe(["audi"]);
    }

    [Fact]
    public async Task when_the_filter_matches_nothing_then_no_matches_shows_and_empty_does_not()
    {
        var list = await LoadedAsync(new FakeTermStore(ClassificationTestData.Term("red", colours)));

        list.FilterText = "zzz";

        (list.Rows.Count, list.HasNoMatches, list.IsEmpty, list.NoMatchesText).ShouldBe((0, true, false, "No terms match your filter"));
    }

    [Fact]
    public async Task when_the_filter_is_cleared_then_every_row_is_listed_again()
    {
        var list = await LoadedAsync(new FakeTermStore(ClassificationTestData.Term("red", colours), ClassificationTestData.Term("blue", colours)));
        list.FilterText = "red";

        list.FilterText = "  ";

        (list.Rows.Count, list.HasNoMatches).ShouldBe((2, false));
    }

    [Fact]
    public async Task when_eight_thousand_terms_are_filtered_then_only_the_matches_are_listed()
    {
        var terms = Enumerable.Range(0, 8000).Select(index => ClassificationTestData.Term($"term {index}", colours)).ToArray();
        var list = await LoadedAsync(new FakeTermStore(terms));

        list.FilterText = "term 79";

        (list.Rows.Count, list.Rows.All(row => row.Text.Contains("term 79", StringComparison.Ordinal))).ShouldBe((111, true));
    }

    [Fact]
    public async Task when_sorted_by_file_count_then_the_smallest_is_first_and_the_same_column_again_flips_it()
    {
        var one = ClassificationTestData.Term("one", colours);
        var two = ClassificationTestData.Term("two", colours);
        var three = ClassificationTestData.Term("three", colours);
        files.Counts[one.Id] = 5;
        files.Counts[two.Id] = 50;
        files.Counts[three.Id] = 1;
        var list = await LoadedAsync(new FakeTermStore(one, two, three));

        list.SortCommand.Execute(TermSortColumnFactory.CreateFileCount());
        var ascending = list.Rows.Select(row => row.Text).ToList();
        list.SortCommand.Execute(TermSortColumnFactory.CreateFileCount());

        ascending.ShouldBe(["three", "one", "two"]);
        list.Rows.Select(row => row.Text).ShouldBe(["two", "one", "three"]);
        list.SortDirection.ShouldBe(TermSortDirectionFactory.CreateDescending());
    }

    [Fact]
    public async Task when_sorted_by_category_then_rows_follow_the_category_name_then_the_term()
    {
        var list = await LoadedAsync(new FakeTermStore(ClassificationTestData.Term("red", colours), ClassificationTestData.Term("audi", cars), ClassificationTestData.Term("blue", colours)));

        list.SortCommand.Execute(TermSortColumnFactory.CreateCategory());

        (list.SortColumn, list.SortDirection).ShouldBe((TermSortColumnFactory.CreateCategory(), TermSortDirectionFactory.CreateAscending()));
        list.Rows.Select(row => row.Text).ShouldBe(["audi", "blue", "red"]);
    }

    [Fact]
    public async Task when_sorted_by_ignored_then_active_terms_come_before_ignored_ones()
    {
        var list = await LoadedAsync(new FakeTermStore(ClassificationTestData.Term("a", colours, TermSourceFactory.CreateSeed(), TermStatusFactory.CreateIgnored()), ClassificationTestData.Term("b", colours)));

        list.SortCommand.Execute(TermSortColumnFactory.CreateIgnored());

        list.Rows.Select(row => row.Text).ShouldBe(["b", "a"]);
    }

    [Fact]
    public async Task when_sorted_by_source_then_rows_follow_the_source_text()
    {
        var list = await LoadedAsync(new FakeTermStore(ClassificationTestData.Term("a", colours, TermSourceFactory.CreateUser(), TermStatusFactory.CreateActive()), ClassificationTestData.Term("b", colours)));

        list.SortCommand.Execute(TermSortColumnFactory.CreateSource());

        list.Rows.Select(row => row.Text).ShouldBe(["a", "b"]);
    }

    [Fact]
    public async Task when_an_active_term_is_toggled_then_the_store_is_asked_to_ignore_it_and_the_row_shows_ignored()
    {
        var red = ClassificationTestData.Term("red", colours);
        var terms = new FakeTermStore(red);
        var list = await LoadedAsync(terms);
        var row = list.Rows.Single();

        await list.ToggleIgnoredCommand.ExecuteAsync(row);

        terms.StatusCalls.ShouldBe([(red.Id, TermStatusFactory.CreateIgnored())]);
        (row.IsIgnored, row.HasError, row.StatusText, row.ToggleText, list.Rows.Count).ShouldBe((true, false, "Ignored", "Un-ignore", 1));
    }

    [Fact]
    public async Task when_an_ignored_term_is_toggled_then_the_store_is_asked_to_make_it_active()
    {
        var red = ClassificationTestData.Term("red", colours, TermSourceFactory.CreateSeed(), TermStatusFactory.CreateIgnored());
        var terms = new FakeTermStore(red);
        var list = await LoadedAsync(terms);
        var row = list.Rows.Single();

        await list.ToggleIgnoredCommand.ExecuteAsync(row);

        terms.StatusCalls.ShouldBe([(red.Id, TermStatusFactory.CreateActive())]);
        (row.IsIgnored, row.ToggleText).ShouldBe((false, "Ignore"));
    }

    [Fact]
    public async Task when_the_store_fails_to_toggle_then_the_row_is_unchanged_and_shows_a_localised_error()
    {
        var red = ClassificationTestData.Term("red", colours);
        var terms = new FakeTermStore(red) { FailSetStatus = true };
        var list = await LoadedAsync(terms);
        var row = list.Rows.Single();

        await list.ToggleIgnoredCommand.ExecuteAsync(row);

        (row.IsIgnored, row.HasError, row.ErrorText, list.Rows.Count).ShouldBe((false, true, "The term could not be changed.", 1));
    }

    [Fact]
    public async Task when_a_toggle_works_after_a_failure_then_the_error_is_cleared()
    {
        var terms = new FakeTermStore(ClassificationTestData.Term("red", colours)) { FailSetStatus = true };
        var list = await LoadedAsync(terms);
        var row = list.Rows.Single();
        await list.ToggleIgnoredCommand.ExecuteAsync(row);
        terms.FailSetStatus = false;

        await list.ToggleIgnoredCommand.ExecuteAsync(row);

        (row.HasError, row.ErrorText, row.IsIgnored).ShouldBe((false, string.Empty, true));
    }

    [Fact]
    public async Task when_the_list_is_sorted_by_ignored_and_a_term_is_toggled_then_the_rows_are_sorted_again()
    {
        var list = await LoadedAsync(new FakeTermStore(ClassificationTestData.Term("a", colours), ClassificationTestData.Term("b", colours)));
        list.SortCommand.Execute(TermSortColumnFactory.CreateIgnored());

        await list.ToggleIgnoredCommand.ExecuteAsync(list.Rows[0]);

        list.Rows.Select(row => row.Text).ShouldBe(["b", "a"]);
    }

    [Fact]
    public async Task when_a_toggle_succeeds_then_the_term_is_never_removed_from_the_store()
    {
        var terms = new FakeTermStore(ClassificationTestData.Term("red", colours));
        var list = await LoadedAsync(terms);

        await list.ToggleIgnoredCommand.ExecuteAsync(list.Rows.Single());

        terms.Stored.Count.ShouldBe(1);
    }
}
