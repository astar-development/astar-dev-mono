using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAnEmbeddedClassificationSeedSource
{
    private const string Prefix = "AStarDev.CloudSync.Classification.TestsUnit.TestSeeds.";

    [Fact]
    public async Task when_two_files_are_embedded_then_they_are_merged_into_one_seed()
    {
        var seed = await LoadSeedAsync("Merge.");

        seed.Categories.Select(category => category.Name).ShouldBe(["Photo", "Audio"], ignoreOrder: true);
        seed.Terms.Select(term => term.Text).ShouldBe(["jpg", "pictures", "holiday", "mp3"], ignoreOrder: true);
    }

    [Fact]
    public async Task when_the_same_extension_is_in_two_files_then_one_term_is_kept()
    {
        var seed = await LoadSeedAsync("Merge.");

        seed.Terms.Count(term => term.Text == "jpg").ShouldBe(1);
    }

    [Fact]
    public async Task when_terms_carry_a_kind_then_the_kind_is_read()
    {
        var seed = await LoadSeedAsync("Merge.");

        seed.Terms.Single(term => term.Text == "jpg").Kind.ShouldBeOfType<SeedTermKind.Extension>();
        seed.Terms.Single(term => term.Text == "pictures").Kind.ShouldBeOfType<SeedTermKind.Folder>();
        seed.Terms.Single(term => term.Text == "holiday").Kind.ShouldBeOfType<SeedTermKind.Word>();
    }

    [Fact]
    public async Task when_files_have_different_versions_then_the_versions_are_joined()
    {
        var seed = await LoadSeedAsync("Merge.");

        seed.Version.ShouldBe("1+2");
    }

    [Fact]
    public async Task when_the_json_is_bad_then_the_seed_is_invalid()
    {
        var failure = await FailureAsync("Bad.");

        failure.ShouldBeOfType<ClassificationFailure.InvalidSeed>();
    }

    [Fact]
    public async Task when_a_kind_is_unknown_then_the_seed_is_invalid()
    {
        var failure = await FailureAsync("UnknownKind.");

        failure.ShouldBeOfType<ClassificationFailure.InvalidSeed>();
    }

    [Fact]
    public async Task when_no_file_matches_the_prefix_then_the_seed_is_invalid()
    {
        var failure = await FailureAsync("Missing.");

        failure.ShouldBeOfType<ClassificationFailure.InvalidSeed>();
    }

    [Fact]
    public async Task when_a_file_has_no_categories_or_terms_then_the_seed_is_empty()
    {
        var seed = await LoadSeedAsync("Empty.");

        seed.Terms.ShouldBeEmpty();
    }

    private static async Task<ClassificationSeed> LoadSeedAsync(string folder) =>
        await Source(folder).LoadAsync(TestContext.Current.CancellationToken).MatchAsync(seed => seed, _ => throw new InvalidOperationException());

    private static async Task<ClassificationFailure> FailureAsync(string folder) =>
        await Source(folder).LoadAsync(TestContext.Current.CancellationToken).MatchAsync(_ => throw new InvalidOperationException(), failure => failure);

    private static EmbeddedClassificationSeedSource Source(string folder) => new(typeof(GivenAnEmbeddedClassificationSeedSource).Assembly, Prefix + folder);
}
