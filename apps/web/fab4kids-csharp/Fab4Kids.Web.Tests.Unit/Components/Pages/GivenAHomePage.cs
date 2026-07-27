using Bunit;
using Fab4Kids.Web.Components.Pages;
using Fab4Kids.Web.Newsletter;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Fab4Kids.Web.Tests.Unit.Components.Pages;

public class GivenAHomePage : Bunit.BunitContext
{
    public GivenAHomePage() => Services.AddSingleton(Substitute.For<INewsletterSubscriptionService>());

    [Fact]
    public void when_rendered_then_the_hero_headline_is_shown()
    {
        var cut = Render<Home>();

        cut.Find("h1.hero__headline").TextContent.ShouldBe("Quality resources for every child");
    }

    [Fact]
    public void when_rendered_then_all_five_subject_cards_are_shown()
    {
        var cut = Render<Home>();

        cut.FindAll("a.subject-card").Count.ShouldBe(5);
    }
}
