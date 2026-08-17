using Bunit;
using Fab4Kids.Web.Components.Pages;

namespace Fab4Kids.Web.TestsUnit.Components.Pages;

public class GivenATermsPage : Bunit.BunitContext
{
    [Fact]
    public void when_rendered_then_the_title_is_shown()
    {
        var cut = Render<Terms>();

        cut.Find("h1").TextContent.ShouldBe("Terms and Conditions");
    }
}
