using Bunit;
using Fab4Kids.Web.Components.Pages;

namespace Fab4Kids.Web.Tests.Unit.Components.Pages;

public class GivenAPrivacyPolicyPage : Bunit.BunitContext
{
    [Fact]
    public void when_rendered_then_the_title_is_shown()
    {
        var cut = Render<PrivacyPolicy>();

        cut.Find("h1").TextContent.ShouldBe("Privacy Policy");
    }
}
