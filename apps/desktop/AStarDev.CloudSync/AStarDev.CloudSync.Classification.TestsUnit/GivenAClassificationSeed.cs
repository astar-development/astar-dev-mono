using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAClassificationSeed
{
    [Fact]
    public void when_a_term_names_an_unknown_category_then_it_is_dropped()
    {
        var seed = ClassificationSeedFactory.Create("1", [SeedCategoryFactory.Create("Colour")], [Term("red", "Colour"), Term("bmw", "Car")]);

        seed.Terms.Select(term => term.Text).ShouldBe(["red"]);
    }

    [Fact]
    public void when_a_term_repeats_ignoring_case_then_one_is_kept()
    {
        var seed = ClassificationSeedFactory.Create("1", [SeedCategoryFactory.Create("Colour")], [Term("Red", "Colour"), Term("red", "COLOUR")]);

        seed.Terms.Count.ShouldBe(1);
    }

    [Fact]
    public void when_the_category_name_differs_only_in_case_then_the_term_is_kept()
    {
        var seed = ClassificationSeedFactory.Create("1", [SeedCategoryFactory.Create("Colour")], [Term("red", "colour")]);

        seed.Terms.Count.ShouldBe(1);
    }

    [Fact]
    public void when_a_term_text_is_shared_by_two_kinds_then_both_are_kept()
    {
        var word = SeedTermFactory.Create("music", "Audio", SeedTermKindFactory.CreateWord()).Match(term => term, () => throw new InvalidOperationException());
        var folder = SeedTermFactory.Create("music", "Audio", SeedTermKindFactory.CreateFolder()).Match(term => term, () => throw new InvalidOperationException());

        var seed = ClassificationSeedFactory.Create("1", [SeedCategoryFactory.Create("Audio")], [word, folder]);

        seed.Terms.Count.ShouldBe(2);
    }

    [Fact]
    public void when_a_seed_term_has_no_kind_then_it_is_a_word()
    {
        Term("red", "Colour").Kind.ShouldBeOfType<SeedTermKind.Word>();
    }

    [Fact]
    public void when_a_category_repeats_ignoring_case_then_one_is_kept()
    {
        var seed = ClassificationSeedFactory.Create("1", [SeedCategoryFactory.Create("Colour"), SeedCategoryFactory.Create("colour")], []);

        seed.Categories.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void when_the_version_is_blank_then_it_becomes_zero(string version)
    {
        var seed = ClassificationSeedFactory.Create(version, [], []);

        seed.Version.ShouldBe("0");
    }

    [Fact]
    public void when_the_version_is_padded_then_it_is_trimmed()
    {
        var seed = ClassificationSeedFactory.Create(" 2 ", [], []);

        seed.Version.ShouldBe("2");
    }

    [Fact]
    public void when_a_seed_term_has_padding_and_case_then_it_is_normalised()
    {
        var term = SeedTermFactory.Create("  Red   Car ", " Car ");

        var created = term.ShouldBeOfType<Option<SeedTerm>.Some>().Value;
        created.Text.ShouldBe("red car");
        created.CategoryName.ShouldBe("Car");
    }

    [Fact]
    public void when_a_seed_term_text_is_blank_then_there_is_no_term()
    {
        SeedTermFactory.Create("  ", "Colour").ShouldBeOfType<Option<SeedTerm>.None>();
    }

    [Fact]
    public void when_a_seed_category_name_is_blank_then_it_becomes_unclassified()
    {
        SeedCategoryFactory.Create(" ").Name.ShouldBe(ClassificationCategoryFactory.UnclassifiedName);
    }

    private static SeedTerm Term(string text, string categoryName) =>
        SeedTermFactory.Create(text, categoryName).Match(term => term, () => throw new InvalidOperationException());
}
