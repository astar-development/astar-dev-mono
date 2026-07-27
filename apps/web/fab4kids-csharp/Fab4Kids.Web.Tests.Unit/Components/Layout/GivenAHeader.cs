using Blazored.LocalStorage;
using Bunit;
using Fab4Kids.Web.Cart;
using Fab4Kids.Web.Components.Layout;
using Fab4Kids.Web.Theming;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Fab4Kids.Web.Tests.Unit.Components.Layout;

public class GivenAHeader : Bunit.BunitContext
{
    public GivenAHeader()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(new ThemeState(Substitute.For<ILocalStorageService>()));
        Services.AddSingleton(new CartState(Substitute.For<ILocalStorageService>()));
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
    public void when_rendered_then_the_theme_switcher_is_shown()
    {
        var cut = Render<Header>();

        cut.FindAll("div.theme-switcher").Count.ShouldBe(1);
    }
}
