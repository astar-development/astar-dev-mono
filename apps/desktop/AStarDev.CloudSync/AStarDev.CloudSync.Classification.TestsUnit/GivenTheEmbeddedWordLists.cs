using System.Reflection;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenTheEmbeddedWordLists
{
    private const string WordsResource = "AStarDev.CloudSync.Classification.Lists.common-words.txt";
    private const string NamesResource = "AStarDev.CloudSync.Classification.Lists.first-names.txt";

    private static readonly Assembly ClassificationAssembly = typeof(ClassificationSeed).Assembly;

    public static TheoryData<string> ListResources => new() { WordsResource, NamesResource };

    [Theory]
    [MemberData(nameof(ListResources))]
    public void when_a_list_is_read_then_entries_are_trimmed_lower_case_and_not_blank(string resource)
    {
        var entries = ReadEntries(resource);

        entries.ShouldAllBe(entry => entry.Length > 0 && entry == entry.Trim() && !entry.Any(char.IsUpper) && !entry.StartsWith('#'));
    }

    [Theory]
    [MemberData(nameof(ListResources))]
    public void when_a_list_is_read_then_it_has_no_duplicates(string resource)
    {
        var entries = ReadEntries(resource);

        entries.Distinct(StringComparer.Ordinal).Count().ShouldBe(entries.Count);
    }

    [Theory]
    [MemberData(nameof(ListResources))]
    public void when_a_list_is_read_then_the_header_names_a_source_and_a_licence(string resource)
    {
        var header = string.Join('\n', ReadLines(resource).Where(line => line.StartsWith('#')));

        header.ShouldContain("Source:");
        header.ShouldContain("Licence:");
    }

    [Fact]
    public void when_the_common_word_list_is_read_then_its_size_is_in_range()
    {
        var count = ReadEntries(WordsResource).Count;

        count.ShouldBeInRange(10_000, 50_000);
    }

    [Fact]
    public void when_the_first_name_list_is_read_then_its_size_is_in_range()
    {
        var count = ReadEntries(NamesResource).Count;

        count.ShouldBeInRange(1_000, 50_000);
    }

    [Fact]
    public void when_a_common_word_is_looked_up_in_any_case_then_it_is_known()
    {
        IKnownWordList lists = EmbeddedWordListsFactory.CreateDefault();

        lists.Contains("house").ShouldBeTrue();
        lists.Contains("HOUSE").ShouldBeTrue();
    }

    [Fact]
    public void when_a_plural_of_a_common_word_is_looked_up_then_it_is_known()
    {
        IKnownWordList lists = EmbeddedWordListsFactory.CreateDefault();

        lists.Contains("houses").ShouldBeTrue();
        lists.Contains("movies").ShouldBeTrue();
    }

    [Fact]
    public void when_a_made_up_word_is_looked_up_then_it_is_not_known()
    {
        IKnownWordList lists = EmbeddedWordListsFactory.CreateDefault();

        lists.Contains("xqzvtk").ShouldBeFalse();
    }

    [Fact]
    public void when_a_first_name_is_looked_up_in_any_case_then_it_is_known()
    {
        IFirstNameList lists = EmbeddedWordListsFactory.CreateDefault();

        lists.Contains("james").ShouldBeTrue();
        lists.Contains("JAMES").ShouldBeTrue();
        lists.Contains("Mary").ShouldBeTrue();
    }

    [Fact]
    public void when_a_plural_of_a_first_name_is_looked_up_then_it_is_known()
    {
        IFirstNameList lists = EmbeddedWordListsFactory.CreateDefault();

        lists.Contains("johns").ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    [InlineData("two words")]
    public void when_the_word_is_null_blank_or_not_one_word_then_both_lists_answer_false(string? word)
    {
        var lists = EmbeddedWordListsFactory.CreateDefault();

        ((IKnownWordList)lists).Contains(word).ShouldBeFalse();
        ((IFirstNameList)lists).Contains(word).ShouldBeFalse();
    }

    [Fact]
    public async Task when_about_twenty_callers_look_up_at_once_then_every_answer_is_correct()
    {
        var lists = EmbeddedWordListsFactory.CreateDefault();

        var answers = await Task.WhenAll(Enumerable.Range(0, 20).Select(index => Task.Run(() => (IsWord: ((IKnownWordList)lists).Contains("house"), IsName: ((IFirstNameList)lists).Contains("mary"), IsMissing: ((IKnownWordList)lists).Contains("xqzvtk")), TestContext.Current.CancellationToken)));

        answers.ShouldAllBe(answer => answer.IsWord && answer.IsName && !answer.IsMissing);
    }

    private static List<string> ReadLines(string resource)
    {
        using var stream = ClassificationAssembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);

        List<string> lines = [];
        while (reader.ReadLine() is { } line)
            lines.Add(line);

        return lines;
    }

    private static List<string> ReadEntries(string resource) => [.. ReadLines(resource).Where(line => !line.StartsWith('#'))];
}
