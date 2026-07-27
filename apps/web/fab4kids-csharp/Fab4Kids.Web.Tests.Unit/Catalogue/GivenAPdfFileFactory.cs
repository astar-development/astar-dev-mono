using Fab4Kids.Web.Catalogue;

namespace Fab4Kids.Web.Tests.Unit.Catalogue;

public class GivenAPdfFileFactory
{
    [Fact]
    public void when_created_with_valid_values_then_they_are_preserved()
    {
        var sut = PdfFileFactory.Create(1, "Times Tables Pack", "pdfs/times-tables.pdf", 2.50m);

        sut.Id.ShouldBe(1);
        sut.Name.ShouldBe("Times Tables Pack");
        sut.Url.ShouldBe("pdfs/times-tables.pdf");
        sut.Price.ShouldBe(2.50m);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_created_with_a_blank_name_then_it_is_normalized(string? name)
    {
        var sut = PdfFileFactory.Create(1, name, "pdfs/file.pdf", 1m);

        sut.Name.ShouldBe("Untitled resource");
    }

    [Fact]
    public void when_created_with_a_negative_price_then_it_is_clamped_to_zero()
    {
        var sut = PdfFileFactory.Create(1, "File", "pdfs/file.pdf", -5m);

        sut.Price.ShouldBe(0m);
    }

    [Fact]
    public void when_created_with_untrimmed_name_and_url_then_they_are_trimmed()
    {
        var sut = PdfFileFactory.Create(1, "  File  ", "  pdfs/file.pdf  ", 1m);

        sut.Name.ShouldBe("File");
        sut.Url.ShouldBe("pdfs/file.pdf");
    }
}
