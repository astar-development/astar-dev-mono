using System.Globalization;
using Bunit;
using Fab4Kids.Web.Components.Layout;

namespace Fab4Kids.Web.TestsUnit.Components.Layout;

public class GivenAFooter : Bunit.BunitContext
{
    [Fact]
    public void when_rendered_then_all_five_subject_links_are_shown()
    {
        var cut = Render<Footer>();

        cut.FindAll("nav[aria-label='Subject pages'] a").Count.ShouldBe(5);
    }

    [Fact]
    public void when_rendered_then_the_legal_links_are_shown()
    {
        var cut = Render<Footer>();

        cut.FindAll("a[href='/privacy-policy']").Count.ShouldBe(1);
        cut.FindAll("a[href='/cookie-policy']").Count.ShouldBe(1);
        cut.FindAll("a[href='/terms']").Count.ShouldBe(1);
    }

    [Fact]
    public void when_rendered_then_the_copyright_notice_includes_the_current_year()
    {
        var cut = Render<Footer>();

        cut.Find("p.site-footer__copyright").TextContent.ShouldContain(DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture));
    }
}
