using AStarDev.Web.Components.Layout;
using AStarDev.Web.Theming;
using Blazored.LocalStorage;
using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace AStarDev.Web.TestsUnit.Components.Layout;

public class GivenANavMenu : Bunit.BunitContext
{
    public GivenANavMenu()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton(new ThemeState(Substitute.For<ILocalStorageService>()));
    }

    [Fact]
    public void when_rendered_then_all_primary_links_are_shown()
    {
        var cut = Render<NavMenu>();

        cut.FindAll("ul.nav-links a.nav-link").Count.ShouldBe(5);
    }

    [Fact]
    public void when_rendered_at_the_home_route_then_the_home_link_is_marked_active()
    {
        var cut = Render<NavMenu>();

        cut.Find("a.nav-link[href='/']").ClassList.ShouldContain("nav-link--active");
    }
}
