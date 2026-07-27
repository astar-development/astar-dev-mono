using Fab4Kids.Web.Catalogue;

namespace Fab4Kids.Web.Tests.Unit.Catalogue;

public class GivenAPdfCategoryFactory
{
    [Fact]
    public void when_created_with_valid_values_then_they_are_preserved()
    {
        var subcategory = PdfSubcategoryFactory.Create(1, "KS1", []);

        var sut = PdfCategoryFactory.Create(1, "Maths", [subcategory]);

        sut.Id.ShouldBe(1);
        sut.Name.ShouldBe("Maths");
        sut.Subcategories.ShouldBe([subcategory]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_created_with_a_blank_name_then_it_is_normalized(string? name)
    {
        var sut = PdfCategoryFactory.Create(1, name, []);

        sut.Name.ShouldBe("Untitled subject");
    }

    [Fact]
    public void when_created_with_no_subcategories_then_an_empty_list_is_used()
    {
        var sut = PdfCategoryFactory.Create(1, "Maths", null);

        sut.Subcategories.ShouldBeEmpty();
    }
}
