namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenAFileClassificationKeywordEntity
{
    [Fact]
    public void when_instantiated_then_keyword_defaults_to_empty_string() =>
        new FileClassificationKeywordEntity().Keyword.ShouldBe(string.Empty);

    [Fact]
    public void when_instantiated_then_category_id_defaults_to_zero() =>
        new FileClassificationKeywordEntity().CategoryId.ShouldBe(0);

    [Fact]
    public void when_instantiated_then_category_navigation_is_null() =>
        new FileClassificationKeywordEntity().Category.ShouldBeNull();

    [Fact]
    public void when_category_id_is_set_then_it_reflects_in_the_property()
    {
        var entity = new FileClassificationKeywordEntity
        {
            CategoryId = 7
        };

        entity.CategoryId.ShouldBe(7);
    }

    [Fact]
    public void when_keyword_is_set_then_it_reflects_in_the_property()
    {
        var entity = new FileClassificationKeywordEntity
        {
            Keyword = "holiday"
        };

        entity.Keyword.ShouldBe("holiday");
    }
}
