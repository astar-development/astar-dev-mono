using AStarDev.Web.Components.Layout;
using AStarDev.Web.Consent;
using Blazored.LocalStorage;
using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace AStarDev.Web.TestsUnit.Components.Layout;

public class GivenAFooter : Bunit.BunitContext
{
    private readonly ILocalStorageService localStorage = Substitute.For<ILocalStorageService>();

    public GivenAFooter()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton(new CookieConsentState(localStorage));
    }

    [Fact]
    public void when_rendered_then_the_privacy_and_cookie_preference_links_are_shown()
    {
        var cut = Render<Footer>();

        cut.FindAll("a.footer-link[href='/privacy']").Count.ShouldBe(1);
        cut.FindAll("button.cookie-pref-btn").Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_cookie_preferences_is_clicked_then_the_stored_preference_is_cleared()
    {
        var cut = Render<Footer>();

        cut.Find("button.cookie-pref-btn").Click();

        await localStorage.Received(1).RemoveItemAsync("cookie-consent-analytics", Arg.Any<CancellationToken>());
    }
}
