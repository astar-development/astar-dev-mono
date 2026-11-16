using AStarDev.FunctionalParadigm;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed partial class GivenTheEmbeddedPlaceVocabulary
{
    private const int CompressedBudgetBytes = 2 * 1024 * 1024;

    [Fact]
    public void when_the_place_file_is_read_then_it_declares_the_place_category_and_terms()
    {
        var file = ReadPlaceFile();

        file.Categories.ShouldBe(["Place"]);
        file.Terms.ShouldNotBeEmpty();
        file.Terms.ShouldAllBe(term => term.Category == "Place");
    }

    [Fact]
    public void when_the_place_file_is_read_then_every_term_is_lower_case_letters_with_single_spaces()
    {
        var offenders = ReadPlaceFile().Terms.Where(term => !PlaceName().IsMatch(term.Text)).Select(term => term.Text).ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void when_the_place_file_is_read_then_no_term_repeats()
    {
        var repeated = ReadPlaceFile().Terms.GroupBy(term => term.Text).Where(group => group.Count() > 1).Select(group => group.Key);

        repeated.ShouldBeEmpty();
    }

    [Fact]
    public void when_the_place_file_is_read_then_no_ambiguous_common_word_is_seeded()
    {
        var texts = ReadPlaceFile().Terms.Select(term => term.Text).ToHashSet();

        PlaceSkipList.Terms.Where(texts.Contains).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("france")]
    [InlineData("london")]
    [InlineData("new york city")]
    [InlineData("paris")]
    [InlineData("tokyo")]
    [InlineData("united kingdom")]
    public void when_the_place_file_is_read_then_known_places_are_present(string text)
    {
        ReadPlaceFile().Terms.ShouldContain(term => term.Text == text);
    }

    [Theory]
    [InlineData("reading")]
    [InlineData("bath")]
    [InlineData("nice")]
    [InlineData("mobile")]
    [InlineData("orange")]
    [InlineData("turkey")]
    [InlineData("jordan")]
    [InlineData("chelsea")]
    [InlineData("cork")]
    public void when_the_place_file_is_read_then_the_ambiguous_word_is_absent(string text)
    {
        ReadPlaceFile().Terms.ShouldNotContain(term => term.Text == text);
    }

    [Fact]
    public async Task when_the_default_seed_is_loaded_then_place_terms_are_merged_without_duplicates()
    {
        var seed = await EmbeddedClassificationSeedSourceFactory.CreateDefault().LoadAsync(TestContext.Current.CancellationToken).MatchAsync(loaded => loaded, _ => throw new InvalidOperationException());

        seed.Categories.ShouldContain(category => category.Name == "Place");
        seed.Terms.ShouldContain(term => term.CategoryName == "Place" && term.Text == "london");
        seed.Terms.GroupBy(term => (term.Text, term.Kind, term.CategoryName)).Where(group => group.Count() > 1).ShouldBeEmpty();
    }

    [Fact]
    public void when_the_place_file_is_compressed_then_it_is_under_the_budget()
    {
        using var stream = typeof(ClassificationSeed).Assembly.GetManifestResourceStream(PlaceResourceName())!;
        using var compressed = new MemoryStream();
        using(var gzip = new GZipStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            stream.CopyTo(gzip);
        }

        compressed.Length.ShouldBeLessThan(CompressedBudgetBytes);
    }

    private static string PlaceResourceName() => typeof(ClassificationSeed).Assembly.GetManifestResourceNames().Single(name => name.EndsWith(".Seed.place.json", StringComparison.Ordinal));

    private static SeedFileDto ReadPlaceFile() => EmbeddedSeedFiles.ReadAll().Single(file => file.Name.EndsWith(".Seed.place.json", StringComparison.Ordinal));

    [GeneratedRegex("^[a-z]+( [a-z]+)*$")]
    private static partial Regex PlaceName();
}
