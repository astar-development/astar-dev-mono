using Bunit;
using Fab4Kids.Web.Components.Common;

namespace Fab4Kids.Web.TestsUnit.Components.Common;

public class GivenALegalArticle : Bunit.BunitContext
{
    [Fact]
    public void when_rendered_then_the_title_and_last_updated_date_are_shown()
    {
        var cut = Render<LegalArticle>(parameters => parameters
            .Add(p => p.Title, "Terms and Conditions")
            .Add(p => p.LastUpdated, "April 2026")
            .AddChildContent("<p>Body text.</p>"));

        cut.Find("h1").TextContent.ShouldBe("Terms and Conditions");
        cut.Find("p.legal-meta").TextContent.ShouldContain("April 2026");
        cut.Markup.ShouldContain("Body text.");
    }
}
