using AStarDev.Web.Components.Pages;
using Bunit;

namespace AStarDev.Web.TestsUnit.Components.Pages;

public class GivenTheBlogPage : Bunit.BunitContext
{
    [Fact]
    public void when_rendered_then_the_heading_and_intro_are_shown()
    {
        var cut = Render<Blog>();

        cut.Find("h1").TextContent.ShouldBe("Blog");
        cut.Find(".page-intro").TextContent.ShouldContain(".NET engineering");
    }

    [Fact]
    public void when_rendered_then_a_coming_soon_message_is_shown()
    {
        var cut = Render<Blog>();

        cut.Find(".coming-soon").TextContent.ShouldBe("Coming soon.");
    }
}
