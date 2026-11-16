using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Classification.TestsUnit.ClassificationTestData;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenClassificationSuggestions
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_words_repeat_then_they_are_ranked_by_file_count_then_alphabetically()
    {
        var suggestions = Service(["zoo/lion.jpg", "zoo/tiger.jpg", "zoo/bear.jpg", "farm/lion.jpg", "farm/bear.jpg", "farm/cow.jpg"]);

        var result = await suggestions.GetSuggestionsAsync(Token);

        Words(result).ShouldBe(["farm", "zoo", "bear", "lion", "cow", "tiger"]);
    }

    [Fact]
    public async Task when_counts_tie_then_the_order_is_alphabetical()
    {
        var suggestions = Service(["mango/pear.jpg", "kiwi/plum.jpg"]);

        var result = await suggestions.GetSuggestionsAsync(Token);

        Words(result).ShouldBe(["kiwi", "mango", "pear", "plum"]);
    }

    [Fact]
    public async Task when_a_word_repeats_in_one_path_then_the_file_is_counted_once()
    {
        var suggestions = Service(["lion/lion lion.jpg", "other/lion.jpg"]);

        var result = await suggestions.GetSuggestionsAsync(Token);

        Counts(result)["lion"].ShouldBe(2);
    }

    [Fact]
    public async Task when_casing_varies_then_the_words_are_merged_and_lower_cased()
    {
        var suggestions = Service(["Beach/Sunset.jpg", "BEACH/sunset.png", "beach/SUNSET.gif"]);

        var result = await suggestions.GetSuggestionsAsync(Token);

        Counts(result).ShouldBe(new Dictionary<string, int> { ["beach"] = 3, ["sunset"] = 3 });
    }

    [Fact]
    public async Task when_a_word_is_a_known_term_in_any_inflection_then_it_is_excluded()
    {
        var suggestions = Service(["Holidays/movies.mp4", "other/beach.jpg"], terms: [Term("movie", Colour)]);

        var result = await suggestions.GetSuggestionsAsync(Token);

        Words(result).ShouldBe(["beach", "holidays", "other"]);
    }

    [Fact]
    public async Task when_a_word_is_a_common_english_word_then_it_is_excluded()
    {
        var suggestions = Service(["house/zebra.jpg"], common: new SuggestionWordList("house"));

        var result = await suggestions.GetSuggestionsAsync(Token);

        Words(result).ShouldBe(["zebra"]);
    }

    [Fact]
    public async Task when_words_are_stop_words_one_letter_or_digits_then_they_are_skipped()
    {
        var suggestions = Service(["the/a/2019/x 42/zebra.jpg"]);

        var result = await suggestions.GetSuggestionsAsync(Token);

        Words(result).ShouldBe(["zebra"]);
    }

    [Fact]
    public async Task when_the_extension_is_present_then_it_is_not_a_word()
    {
        var suggestions = Service(["zebra.jpeg"]);

        var result = await suggestions.GetSuggestionsAsync(Token);

        Words(result).ShouldBe(["zebra"]);
    }

    [Fact]
    public async Task when_a_name_is_camel_cased_then_it_is_split_into_words()
    {
        var suggestions = Service(["ZebraCrossing.jpg"]);

        var result = await suggestions.GetSuggestionsAsync(Token);

        Words(result).ShouldBe(["crossing", "zebra"]);
    }

    [Fact]
    public async Task when_a_word_is_a_first_name_then_it_is_flagged_but_still_unassigned()
    {
        var store = new SuggestionTermStore();
        var suggestions = Service(["Alice/zebra.jpg"], store: store, names: new SuggestionWordList("alice"));

        var result = await suggestions.GetSuggestionsAsync(Token);

        Hints(result).ShouldBe(new Dictionary<string, SuggestionHint> { ["alice"] = SuggestionHintFactory.CreateFirstName(), ["zebra"] = SuggestionHintFactory.CreatePlain() });
        store.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_word_is_ignored_then_it_never_returns()
    {
        var store = new SuggestionTermStore();
        var suggestions = Service(["lion/zebra.jpg"], store: store);

        await suggestions.IgnoreAsync("lion", Colour, Token);
        var result = await suggestions.GetSuggestionsAsync(Token);

        Words(result).ShouldBe(["zebra"]);
    }

    [Fact]
    public async Task when_the_term_store_holds_an_ignored_term_then_the_word_is_excluded()
    {
        var suggestions = Service(["lion/zebra.jpg"], terms: [Term("lion", Colour, TermSourceFactory.CreateSuggested(), TermStatusFactory.CreateIgnored())]);

        var result = await suggestions.GetSuggestionsAsync(Token);

        Words(result).ShouldBe(["zebra"]);
    }

    [Fact]
    public async Task when_the_source_throws_then_a_failure_is_returned()
    {
        var suggestions = Service(["one/two.jpg", "three/four.jpg"], failingSource: true);

        var result = await suggestions.GetSuggestionsAsync(Token);

        result.Match(_ => "ok", failure => failure.GetType().Name).ShouldBe(nameof(ClassificationFailure.Unavailable));
    }

    [Fact]
    public async Task when_the_term_store_fails_then_a_failure_is_returned()
    {
        var suggestions = Service(["one/two.jpg"], store: new SuggestionTermStore { Fails = true });

        var result = await suggestions.GetSuggestionsAsync(Token);

        result.Match(_ => "ok", failure => failure.GetType().Name).ShouldBe(nameof(ClassificationFailure.Unavailable));
    }

    [Fact]
    public async Task when_cancelled_then_the_scan_stops()
    {
        var suggestions = Service(["one/two.jpg"]);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var scan = async () => await suggestions.GetSuggestionsAsync(cancellation.Token);

        await scan.ShouldThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task when_a_word_is_promoted_then_a_user_term_is_created_and_the_word_is_no_longer_suggested()
    {
        var store = new SuggestionTermStore();
        var suggestions = Service(["lion/zebra.jpg"], store: store);

        var promoted = await suggestions.PromoteAsync("Lion", Colour, Token);
        var remaining = await suggestions.GetSuggestionsAsync(Token);

        var term = promoted.Match(created => created, _ => throw new InvalidOperationException());
        (term.Text, term.CategoryId, term.Source, term.Status).ShouldBe(("lion", Colour, TermSourceFactory.CreateUser(), TermStatusFactory.CreateActive()));
        Words(remaining).ShouldBe(["zebra"]);
    }

    [Fact]
    public async Task when_a_suggested_term_is_promoted_then_it_becomes_a_user_term_with_the_same_id()
    {
        var existing = Term("lion", Colour, TermSourceFactory.CreateSuggested(), TermStatusFactory.CreateActive());
        var store = new SuggestionTermStore(existing);
        var suggestions = Service([], store: store);

        await suggestions.PromoteAsync("lion", Colour, Token);

        var terms = store.Snapshot();
        terms.Count.ShouldBe(1);
        (terms[0].Id, terms[0].Source).ShouldBe((existing.Id, TermSourceFactory.CreateUser()));
    }

    [Fact]
    public async Task when_a_seed_term_is_promoted_then_it_is_returned_unchanged()
    {
        var existing = Term("lion", Colour);
        var store = new SuggestionTermStore(existing);
        var suggestions = Service([], store: store);

        var promoted = await suggestions.PromoteAsync("lion", Colour, Token);

        promoted.Match(term => term, _ => throw new InvalidOperationException()).ShouldBe(existing);
    }

    [Fact]
    public async Task when_an_ignored_word_is_promoted_then_it_is_active_again()
    {
        var store = new SuggestionTermStore();
        var suggestions = Service([], store: store);
        await suggestions.IgnoreAsync("lion", Colour, Token);

        await suggestions.PromoteAsync("lion", Colour, Token);

        var terms = store.Snapshot();
        (terms.Count, terms[0].Status, terms[0].Source).ShouldBe((1, TermStatusFactory.CreateActive(), TermSourceFactory.CreateUser()));
    }

    [Fact]
    public async Task when_a_word_is_ignored_twice_then_one_ignored_term_exists()
    {
        var store = new SuggestionTermStore();
        var suggestions = Service([], store: store);

        await suggestions.IgnoreAsync("lion", Colour, Token);
        await suggestions.IgnoreAsync("LION", Colour, Token);

        var terms = store.Snapshot();
        (terms.Count, terms[0].Status).ShouldBe((1, TermStatusFactory.CreateIgnored()));
    }

    [Fact]
    public async Task when_a_seed_term_is_ignored_then_it_keeps_its_source_and_is_ignored()
    {
        var existing = Term("lion", Colour);
        var store = new SuggestionTermStore(existing);
        var suggestions = Service([], store: store);

        await suggestions.IgnoreAsync("lion", Colour, Token);

        var terms = store.Snapshot();
        (terms[0].Id, terms[0].Source, terms[0].Status).ShouldBe((existing.Id, TermSourceFactory.CreateSeed(), TermStatusFactory.CreateIgnored()));
    }

    [Fact]
    public async Task when_the_word_is_blank_then_promote_and_ignore_fail_without_throwing()
    {
        var suggestions = Service([]);

        var promoted = await suggestions.PromoteAsync("  ", Colour, Token);
        var ignored = await suggestions.IgnoreAsync("", Colour, Token);

        (promoted.Match(_ => "ok", failure => failure.GetType().Name), ignored.Match(_ => "ok", failure => failure.GetType().Name)).ShouldBe((nameof(ClassificationFailure.NotFound), nameof(ClassificationFailure.NotFound)));
    }

    [Fact]
    public async Task when_the_term_store_fails_then_promote_returns_a_failure()
    {
        var suggestions = Service([], store: new SuggestionTermStore { Fails = true });

        var promoted = await suggestions.PromoteAsync("lion", Colour, Token);

        promoted.Match(_ => "ok", failure => failure.GetType().Name).ShouldBe(nameof(ClassificationFailure.Unavailable));
    }

    [Fact]
    public async Task when_the_same_word_is_promoted_in_parallel_then_one_term_is_created()
    {
        var store = new SuggestionTermStore();
        var suggestions = Service([], store: store);
        var variants = new[] { "lion", "Lion", "LION", " lion " };

        await Task.WhenAll(Enumerable.Range(0, 20).Select(index => Task.Run(() => suggestions.PromoteAsync(variants[index % variants.Length], Colour, Token), Token)));

        var terms = store.Snapshot();
        terms.Count.ShouldBe(1);
        terms[0].Source.ShouldBe(TermSourceFactory.CreateUser());
    }

    [Fact]
    public async Task when_promote_and_ignore_race_on_one_word_then_one_term_exists()
    {
        var store = new SuggestionTermStore();
        var suggestions = Service([], store: store);

        await Task.WhenAll(Enumerable.Range(0, 20).Select(index => Task.Run(() => index % 2 == 0 ? suggestions.PromoteAsync("lion", Colour, Token) : suggestions.IgnoreAsync("lion", Colour, Token), Token)));

        store.Snapshot().Count.ShouldBe(1);
    }

    private static ClassificationSuggestions Service(IReadOnlyList<string> paths, SuggestionTermStore? store = null, ClassificationTerm[]? terms = null, SuggestionWordList? common = null, SuggestionWordList? names = null, bool failingSource = false) =>
        new(new SuggestionFileSource(paths, failingSource), store ?? new SuggestionTermStore(terms ?? []), common ?? new SuggestionWordList(), names ?? new SuggestionWordList());

    private static string[] Words(Result<IReadOnlyList<ClassificationSuggestion>, ClassificationFailure> result) => [.. Unwrap(result).Select(suggestion => suggestion.Word)];

    private static Dictionary<string, int> Counts(Result<IReadOnlyList<ClassificationSuggestion>, ClassificationFailure> result) => Unwrap(result).ToDictionary(suggestion => suggestion.Word, suggestion => suggestion.FileCount);

    private static Dictionary<string, SuggestionHint> Hints(Result<IReadOnlyList<ClassificationSuggestion>, ClassificationFailure> result) => Unwrap(result).ToDictionary(suggestion => suggestion.Word, suggestion => suggestion.Hint);

    private static IReadOnlyList<ClassificationSuggestion> Unwrap(Result<IReadOnlyList<ClassificationSuggestion>, ClassificationFailure> result) => result.Match(list => list, _ => throw new InvalidOperationException());
}
