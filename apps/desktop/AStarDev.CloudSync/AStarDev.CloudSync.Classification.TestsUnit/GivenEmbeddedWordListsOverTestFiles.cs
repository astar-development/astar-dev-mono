namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenEmbeddedWordListsOverTestFiles
{
    private const string Prefix = "AStarDev.CloudSync.Classification.TestsUnit.TestLists.";

    private static EmbeddedWordLists Create() => new(typeof(GivenEmbeddedWordListsOverTestFiles).Assembly, Prefix);

    [Fact]
    public void when_the_file_has_headers_and_blank_lines_then_only_entries_are_known()
    {
        IKnownWordList lists = Create();

        lists.Contains("apple").ShouldBeTrue();
        lists.Contains("cherry").ShouldBeTrue();
        lists.Contains("header").ShouldBeFalse();
        lists.Contains("trailing").ShouldBeFalse();
    }

    [Fact]
    public void when_an_entry_has_spaces_or_capitals_then_it_is_matched_trimmed_and_lower_case()
    {
        IKnownWordList words = Create();
        IFirstNameList names = Create();

        words.Contains("Banana").ShouldBeTrue();
        names.Contains("bob").ShouldBeTrue();
        names.Contains("ALICE").ShouldBeTrue();
    }

    [Fact]
    public void when_the_entry_is_singular_and_the_query_plural_then_the_word_is_known()
    {
        IKnownWordList lists = Create();

        lists.Contains("movie").ShouldBeTrue();
        lists.Contains("movies").ShouldBeTrue();
        lists.Contains("apples").ShouldBeTrue();
    }

    [Fact]
    public void when_the_lists_are_separate_then_a_name_is_not_a_common_word()
    {
        IKnownWordList words = Create();
        IFirstNameList names = Create();

        words.Contains("alice").ShouldBeFalse();
        names.Contains("apple").ShouldBeFalse();
    }

    [Fact]
    public void when_the_resource_files_are_missing_then_lookups_answer_false_without_throwing()
    {
        var lists = new EmbeddedWordLists(typeof(GivenEmbeddedWordListsOverTestFiles).Assembly, "Nothing.Here.");

        ((IKnownWordList)lists).Contains("apple").ShouldBeFalse();
        ((IFirstNameList)lists).Contains("alice").ShouldBeFalse();
    }
}
