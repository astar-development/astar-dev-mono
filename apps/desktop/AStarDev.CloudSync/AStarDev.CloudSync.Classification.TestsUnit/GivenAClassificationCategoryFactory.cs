namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAClassificationCategoryFactory
{
    [Fact]
    public void when_name_has_padding_and_repeated_spaces_then_it_is_trimmed_and_keeps_its_case()
    {
        var category = ClassificationCategoryFactory.Create(CategoryIdFactory.Create(Guid.NewGuid()), "  Photo   Album ");

        category.Name.ShouldBe("Photo Album");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void when_name_is_blank_then_it_becomes_unclassified(string? name)
    {
        var category = ClassificationCategoryFactory.Create(CategoryIdFactory.Create(Guid.NewGuid()), name!);

        category.Name.ShouldBe(ClassificationCategoryFactory.UnclassifiedName);
    }
}
