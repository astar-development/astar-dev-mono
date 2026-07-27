using Bunit;
using Fab4Kids.Web.Catalogue;
using Fab4Kids.Web.Components.Common;

namespace Fab4Kids.Web.Tests.Unit.Components.Common;

public class GivenAPdfCard : Bunit.BunitContext
{
    [Fact]
    public void when_rendered_then_the_name_and_price_are_shown()
    {
        var file = PdfFileFactory.Create(1, "Times Tables Pack", "pdfs/times-tables.pdf", 2.50m);

        var cut = Render<PdfCard>(parameters => parameters.Add(p => p.File, file));

        cut.Find("h3.pdf-card__name").TextContent.ShouldBe("Times Tables Pack");
        cut.Find("p.pdf-card__price").TextContent.ShouldBe("£2.50");
    }

    [Fact]
    public void when_rendered_then_the_view_link_points_at_the_file_url()
    {
        var file = PdfFileFactory.Create(1, "Times Tables Pack", "pdfs/times-tables.pdf", 2.50m);

        var cut = Render<PdfCard>(parameters => parameters.Add(p => p.File, file));

        var link = cut.Find("a.btn--secondary");
        link.GetAttribute("href").ShouldBe("pdfs/times-tables.pdf");
        link.GetAttribute("target").ShouldBe("_blank");
    }
}
