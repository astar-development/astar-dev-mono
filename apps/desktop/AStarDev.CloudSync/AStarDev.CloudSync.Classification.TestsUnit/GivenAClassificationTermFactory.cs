using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAClassificationTermFactory
{
    [Fact]
    public void when_text_has_padding_case_and_repeated_spaces_then_it_is_trimmed_lowered_and_single_spaced()
    {
        var term = Create("  Red   CAR ");

        term.ShouldBeOfType<Option<ClassificationTerm>.Some>().Value.Text.ShouldBe("red car");
    }

    [Fact]
    public void when_text_is_decomposed_unicode_then_it_is_composed()
    {
        var term = Create("café");

        term.ShouldBeOfType<Option<ClassificationTerm>.Some>().Value.Text.ShouldBe("café");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void when_text_is_blank_then_there_is_no_term(string? text)
    {
        var term = Create(text!);

        term.ShouldBeOfType<Option<ClassificationTerm>.None>();
    }

    [Fact]
    public void when_the_term_is_created_then_the_other_values_are_kept()
    {
        var id = TermIdFactory.Create(Guid.NewGuid());
        var categoryId = CategoryIdFactory.Create(Guid.NewGuid());

        var term = ClassificationTermFactory.Create(id, "red", categoryId, TermSourceFactory.CreateUser(), TermStatusFactory.CreateIgnored());

        var created = term.ShouldBeOfType<Option<ClassificationTerm>.Some>().Value;
        created.Id.ShouldBe(id);
        created.CategoryId.ShouldBe(categoryId);
        created.Source.ShouldBeOfType<TermSource.User>();
        created.Status.ShouldBeOfType<TermStatus.Ignored>();
    }

    [Fact]
    public void when_the_term_is_written_as_text_then_the_words_are_redacted()
    {
        var created = Create("holiday in paris").ShouldBeOfType<Option<ClassificationTerm>.Some>().Value;

        created.ToString().ShouldNotContain("paris");
        SeedTermFactory.Create("holiday in paris", "Place").ShouldBeOfType<Option<SeedTerm>.Some>().Value.ToString().ShouldNotContain("paris");
    }

    private static Option<ClassificationTerm> Create(string text) =>
        ClassificationTermFactory.Create(TermIdFactory.Create(Guid.NewGuid()), text, CategoryIdFactory.Create(Guid.NewGuid()), TermSourceFactory.CreateSeed(), TermStatusFactory.CreateActive());
}
