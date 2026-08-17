using Fab4Kids.Web.Catalogue;

namespace Fab4Kids.Web.TestsUnit.Catalogue;

public class GivenAPdfSubcategoryFactory
{
    [Fact]
    public void when_created_with_valid_values_then_they_are_preserved()
    {
        var file = PdfFileFactory.Create(1, "File", "pdfs/file.pdf", 1m);

        var sut = PdfSubcategoryFactory.Create(1, "KS1", [file]);

        sut.Id.ShouldBe(1);
        sut.Name.ShouldBe("KS1");
        sut.Files.ShouldBe([file]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_created_with_a_blank_name_then_it_is_normalized(string? name)
    {
        var sut = PdfSubcategoryFactory.Create(1, name, []);

        sut.Name.ShouldBe("Untitled key stage");
    }

    [Fact]
    public void when_created_with_no_files_then_an_empty_list_is_used()
    {
        var sut = PdfSubcategoryFactory.Create(1, "KS1", null);

        sut.Files.ShouldBeEmpty();
    }
}
