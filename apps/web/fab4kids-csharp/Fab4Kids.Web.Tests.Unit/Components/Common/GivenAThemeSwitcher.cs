using Blazored.LocalStorage;
using Bunit;
using Fab4Kids.Web.Components.Common;
using Fab4Kids.Web.Theming;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Fab4Kids.Web.Tests.Unit.Components.Common;

public class GivenAThemeSwitcher : Bunit.BunitContext
{
    private readonly ILocalStorageService localStorage = Substitute.For<ILocalStorageService>();

    public GivenAThemeSwitcher()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(new ThemeState(localStorage));
    }

    [Fact]
    public void when_rendered_then_three_theme_buttons_are_shown()
    {
        var cut = Render<ThemeSwitcher>();

        cut.FindAll("button").Count.ShouldBe(3);
    }

    [Fact]
    public void when_rendered_then_the_light_button_is_active_by_default()
    {
        var cut = Render<ThemeSwitcher>();

        cut.Find("button[aria-label='Switch to light theme']").GetAttribute("aria-pressed").ShouldBe("true");
    }

    [Fact]
    public void when_the_dark_button_is_clicked_then_the_theme_state_updates_to_dark()
    {
        var themeState = Services.GetRequiredService<ThemeState>();
        var cut = Render<ThemeSwitcher>();

        cut.Find("button[aria-label='Switch to dark theme']").Click();

        themeState.Current.ShouldBe(Theme.Dark);
    }

    [Fact]
    public void when_the_dark_button_is_clicked_then_it_becomes_the_active_button()
    {
        var cut = Render<ThemeSwitcher>();

        cut.Find("button[aria-label='Switch to dark theme']").Click();

        cut.Find("button[aria-label='Switch to dark theme']").GetAttribute("aria-pressed").ShouldBe("true");
        cut.Find("button[aria-label='Switch to light theme']").GetAttribute("aria-pressed").ShouldBe("false");
    }
}
