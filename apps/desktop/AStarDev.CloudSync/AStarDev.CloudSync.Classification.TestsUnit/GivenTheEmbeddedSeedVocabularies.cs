using System.Text.RegularExpressions;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed partial class GivenTheEmbeddedSeedVocabularies
{
    private static readonly HashSet<string> AllowedDuplicates = ["tomato", "leaf", "peru"];

    private static readonly string[] ExpectedCategories = ["Colour", "Clothing", "Object", "Car", "Place"];

    [Fact]
    public void when_the_seed_files_are_read_then_one_file_exists_per_vocabulary()
    {
        var categories = EmbeddedSeedFiles.ReadAll().SelectMany(file => file.Categories);

        categories.Order().ShouldBe(ExpectedCategories.Order());
    }

    [Fact]
    public void when_the_seed_files_are_read_then_each_has_a_version_and_terms()
    {
        var files = EmbeddedSeedFiles.ReadAll();

        files.ShouldAllBe(file => file.Version.Length > 0 && file.Terms.Count > 0);
    }

    [Fact]
    public void when_the_seed_files_are_read_then_no_term_or_category_is_blank()
    {
        var files = EmbeddedSeedFiles.ReadAll();

        files.SelectMany(file => file.Terms).ShouldAllBe(term => !string.IsNullOrWhiteSpace(term.Text) && !string.IsNullOrWhiteSpace(term.Category));
        files.SelectMany(file => file.Categories).ShouldAllBe(category => !string.IsNullOrWhiteSpace(category));
    }

    [Fact]
    public void when_the_seed_files_are_read_then_every_term_is_lower_case_and_single_spaced()
    {
        var offenders = EmbeddedSeedFiles.ReadAll().SelectMany(file => file.Terms).Where(term => !string.Equals(term.Text, term.Text.ToLowerInvariant(), StringComparison.Ordinal) || MultiSpace().IsMatch(term.Text) || term.Text != term.Text.Trim()).ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void when_the_seed_files_are_read_then_no_term_repeats_in_a_file()
    {
        foreach(var file in EmbeddedSeedFiles.ReadAll())
        {
            file.Terms.GroupBy(term => term.Text).Where(group => group.Count() > 1).ShouldBeEmpty();
        }
    }

    [Fact]
    public void when_the_seed_files_are_read_then_a_term_is_in_two_categories_only_when_allowed()
    {
        var repeated = EmbeddedSeedFiles.ReadAll().SelectMany(file => file.Terms).GroupBy(term => term.Text).Where(group => group.Select(term => term.Category).Distinct().Count() > 1).Select(group => group.Key);

        repeated.Where(text => !AllowedDuplicates.Contains(text)).ShouldBeEmpty();
    }

    [Fact]
    public void when_the_seed_files_are_read_then_the_american_spelling_is_never_a_category()
    {
        var categories = EmbeddedSeedFiles.ReadAll().SelectMany(file => file.Categories).Concat(EmbeddedSeedFiles.ReadAll().SelectMany(file => file.Terms).Select(term => term.Category));

        categories.ShouldNotContain(category => category.Equals("Color", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void when_the_seed_files_are_read_then_every_term_category_exists_in_its_file()
    {
        foreach(var file in EmbeddedSeedFiles.ReadAll())
        {
            file.Terms.Where(term => !file.Categories.Contains(term.Category)).ShouldBeEmpty();
        }
    }

    [Theory]
    [InlineData("Colour", "red")]
    [InlineData("Colour", "cornflowerblue")]
    [InlineData("Clothing", "jumper")]
    [InlineData("Object", "chair")]
    [InlineData("Car", "ford")]
    [InlineData("Car", "land rover")]
    public void when_the_seed_files_are_read_then_known_terms_are_present(string category, string text)
    {
        var terms = EmbeddedSeedFiles.ReadAll().SelectMany(file => file.Terms);

        terms.ShouldContain(term => term.Category == category && term.Text == text);
    }

    [Fact]
    public void when_the_seed_files_are_read_then_multi_word_entries_exist()
    {
        var terms = EmbeddedSeedFiles.ReadAll().SelectMany(file => file.Terms);

        terms.ShouldContain(term => term.Text.Contains(' '));
    }

    [GeneratedRegex("\\s{2,}|[\\t\\r\\n]")]
    private static partial Regex MultiSpace();
}
