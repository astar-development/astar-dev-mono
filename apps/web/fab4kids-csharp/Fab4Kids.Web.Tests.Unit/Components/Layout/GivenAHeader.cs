using Blazored.LocalStorage;
using Bunit;
using Fab4Kids.Web.Cart;
using Fab4Kids.Web.Checkout;
using Fab4Kids.Web.Components.Layout;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Fab4Kids.Web.Tests.Unit.Components.Layout;

public class GivenAHeader : Bunit.BunitContext
{
    public GivenAHeader()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(new CartState(Substitute.For<ILocalStorageService>()));
        Services.AddSingleton(Substitute.For<ICheckoutSessionService>());
    }

    [Fact]
    public void when_rendered_then_all_five_subject_nav_links_are_shown()
    {
        var cut = Render<Header>();

        cut.FindAll("nav#primary-nav a").Count.ShouldBe(5);
    }

    [Fact]
    public void when_rendered_then_the_search_form_is_shown()
    {
        var cut = Render<Header>();

        cut.FindAll("form.site-header__search[action='/search']").Count.ShouldBe(1);
    }

    [Fact]
    public void when_rendered_then_the_mobile_nav_is_closed()
    {
        var cut = Render<Header>();

        cut.Find("button#hamburger-btn").GetAttribute("aria-expanded").ShouldBe("false");
        cut.Find("nav#primary-nav").ClassList.ShouldNotContain("site-header__nav--open");
    }

    [Fact]
    public void when_the_hamburger_button_is_clicked_then_the_mobile_nav_opens()
    {
        var cut = Render<Header>();

        cut.Find("button#hamburger-btn").Click();

        cut.Find("button#hamburger-btn").GetAttribute("aria-expanded").ShouldBe("true");
        cut.Find("nav#primary-nav").ClassList.ShouldContain("site-header__nav--open");
    }

    [Fact]
    public void when_the_hamburger_button_is_clicked_twice_then_the_mobile_nav_closes_again()
    {
        var cut = Render<Header>();

        cut.Find("button#hamburger-btn").Click();
        cut.Find("button#hamburger-btn").Click();

        cut.Find("button#hamburger-btn").GetAttribute("aria-expanded").ShouldBe("false");
        cut.Find("nav#primary-nav").ClassList.ShouldNotContain("site-header__nav--open");
    }
}
