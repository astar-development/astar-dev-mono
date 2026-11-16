using System.Reflection;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenTheSeedTermKinds
{
    private const string DefaultPrefix = "AStarDev.CloudSync.Classification.Seed.";

    [Fact]
    public async Task when_every_shipped_seed_file_is_read_then_no_text_and_category_is_seeded_under_two_kinds()
    {
        var terms = await ReadTermsPerFileAsync();

        var collisions = SeedKindCollisions.Find(terms);

        terms.ShouldNotBeEmpty();
        collisions.Count.ShouldBe(0, $"{collisions.Count} text and category pairs are seeded under more than one kind: {string.Join(", ", collisions)}");
    }

    [Fact]
    public void when_a_text_is_seeded_under_two_kinds_in_one_category_then_it_is_reported_once()
    {
        var terms = new[]
        {
            Term("log", "Document", SeedTermKindFactory.CreateExtension()),
            Term("log", "Document", SeedTermKindFactory.CreateFolder()),
            Term("LOG", "Document", SeedTermKindFactory.CreateWord()),
        };

        var collisions = SeedKindCollisions.Find(terms);

        collisions.ShouldBe(["log|Document"]);
    }

    [Fact]
    public void when_a_text_is_seeded_under_two_kinds_in_different_categories_then_nothing_is_reported()
    {
        var terms = new[]
        {
            Term("log", "Document", SeedTermKindFactory.CreateExtension()),
            Term("log", "Code", SeedTermKindFactory.CreateFolder()),
        };

        SeedKindCollisions.Find(terms).ShouldBeEmpty();
    }

    [Fact]
    public void when_a_text_is_seeded_twice_under_the_same_kind_then_nothing_is_reported()
    {
        var terms = new[]
        {
            Term("log", "Document", SeedTermKindFactory.CreateExtension()),
            Term("log", "Document", SeedTermKindFactory.CreateExtension()),
        };

        SeedKindCollisions.Find(terms).ShouldBeEmpty();
    }

    private static SeedTerm Term(string text, string category, SeedTermKind kind) =>
        SeedTermFactory.Create(text, category, kind).Match(term => term, () => throw new InvalidOperationException());

    private static async Task<IReadOnlyList<SeedTerm>> ReadTermsPerFileAsync()
    {
        var assembly = typeof(EmbeddedClassificationSeedSourceFactory).Assembly;
        var names = assembly.GetManifestResourceNames().Where(name => name.StartsWith(DefaultPrefix, StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)).Order(StringComparer.Ordinal);
        var terms = new List<SeedTerm>();
        foreach (var name in names)
        {
            var seed = await LoadFileAsync(assembly, name);
            terms.AddRange(seed.Terms);
        }

        return terms;
    }

    private static async Task<ClassificationSeed> LoadFileAsync(Assembly assembly, string resourceName) =>
        await new EmbeddedClassificationSeedSource(assembly, resourceName).LoadAsync(TestContext.Current.CancellationToken).MatchAsync(seed => seed, _ => throw new InvalidOperationException());
}
