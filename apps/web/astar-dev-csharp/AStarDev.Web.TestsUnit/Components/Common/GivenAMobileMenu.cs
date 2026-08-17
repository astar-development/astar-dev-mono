using AStarDev.Web.Components.Common;
using AStarDev.Web.Theming;
using Blazored.LocalStorage;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace AStarDev.Web.TestsUnit.Components.Common;

public class GivenAMobileMenu : Bunit.BunitContext
{
    public GivenAMobileMenu()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(new ThemeState(Substitute.For<ILocalStorageService>()));
    }

    private IRenderedComponent<MobileMenu> RenderMenu() => Render<MobileMenu>(parameters => parameters
        .Add(p => p.GithubUrl, "https://github.com/example")
        .Add(p => p.NugetUrl, "https://www.nuget.org/profiles/example"));

    [Fact]
    public void when_rendered_then_the_drawer_is_closed()
    {
        var cut = RenderMenu();

        cut.Find("button.hamburger").GetAttribute("aria-expanded").ShouldBe("false");
        cut.Find("div.drawer").ClassList.ShouldNotContain("drawer--open");
    }

    [Fact]
    public void when_the_hamburger_is_clicked_then_the_drawer_opens()
    {
        var cut = RenderMenu();

        cut.Find("button.hamburger").Click();

        cut.Find("button.hamburger").GetAttribute("aria-expanded").ShouldBe("true");
        cut.Find("div.drawer").ClassList.ShouldContain("drawer--open");
    }

    [Fact]
    public void when_the_close_button_is_clicked_then_the_drawer_closes()
    {
        var cut = RenderMenu();
        cut.Find("button.hamburger").Click();

        cut.Find("button.drawer-close").Click();

        cut.Find("button.hamburger").GetAttribute("aria-expanded").ShouldBe("false");
    }
}
