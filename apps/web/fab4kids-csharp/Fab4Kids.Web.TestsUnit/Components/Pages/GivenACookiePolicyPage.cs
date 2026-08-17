using Bunit;
using Fab4Kids.Web.Components.Pages;

namespace Fab4Kids.Web.TestsUnit.Components.Pages;

public class GivenACookiePolicyPage : Bunit.BunitContext
{
    [Fact]
    public void when_rendered_then_the_title_is_shown()
    {
        var cut = Render<CookiePolicy>();

        cut.Find("h1").TextContent.ShouldBe("Cookie Policy");
    }

    [Fact]
    public void when_rendered_then_all_five_tracked_cookies_are_listed()
    {
        var cut = Render<CookiePolicy>();

        cut.FindAll("table.legal-table tbody tr").Count.ShouldBe(5);
    }
}
