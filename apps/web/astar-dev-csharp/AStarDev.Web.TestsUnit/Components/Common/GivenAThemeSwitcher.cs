using AStarDev.Web.Components.Common;
using AStarDev.Web.Theming;
using Blazored.LocalStorage;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace AStarDev.Web.TestsUnit.Components.Common;

public class GivenAThemeSwitcher : Bunit.BunitContext
{
    private readonly ILocalStorageService localStorage = Substitute.For<ILocalStorageService>();

    public GivenAThemeSwitcher()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(new ThemeState(localStorage));
    }

    [Fact]
    public void when_rendered_then_four_theme_buttons_are_shown()
    {
        var cut = Render<ThemeSwitcher>();

        cut.FindAll("button").Count.ShouldBe(4);
    }

    [Fact]
    public void when_rendered_then_the_dark_button_is_active_by_default()
    {
        var cut = Render<ThemeSwitcher>();

        cut.Find("button[aria-label='Switch to dark theme']").GetAttribute("aria-pressed").ShouldBe("true");
    }

    [Fact]
    public void when_the_metal_button_is_clicked_then_the_theme_state_updates_to_metal()
    {
        var themeState = Services.GetRequiredService<ThemeState>();
        var cut = Render<ThemeSwitcher>();

        cut.Find("button[aria-label='Switch to metal theme']").Click();

        themeState.Current.ShouldBe(Theme.Metal);
    }

    [Fact]
    public void when_the_metal_button_is_clicked_then_it_becomes_the_active_button()
    {
        var cut = Render<ThemeSwitcher>();

        cut.Find("button[aria-label='Switch to metal theme']").Click();

        cut.Find("button[aria-label='Switch to metal theme']").GetAttribute("aria-pressed").ShouldBe("true");
        cut.Find("button[aria-label='Switch to dark theme']").GetAttribute("aria-pressed").ShouldBe("false");
    }
}
