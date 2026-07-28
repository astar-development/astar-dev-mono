using AStarDev.Web.Components.Common;
using AStarDev.Web.Consent;
using Blazored.LocalStorage;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace AStarDev.Web.Tests.Unit.Components.Common;

public class GivenACookieConsent : Bunit.BunitContext
{
    private readonly ILocalStorageService localStorage = Substitute.For<ILocalStorageService>();

    public GivenACookieConsent()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(new CookieConsentState(localStorage));
    }

    [Fact]
    public void when_no_preference_is_stored_then_the_consent_banner_is_shown()
    {
        localStorage.GetItemAsStringAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);

        var cut = Render<CookieConsent>();

        cut.FindAll("div.consent-bar").Count.ShouldBe(1);
    }

    [Fact]
    public void when_a_preference_is_already_stored_then_the_consent_banner_is_not_shown()
    {
        localStorage.GetItemAsStringAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("true");

        var cut = Render<CookieConsent>();

        cut.FindAll("div.consent-bar").ShouldBeEmpty();
    }

    [Fact]
    public void when_accept_is_clicked_then_the_banner_is_hidden()
    {
        localStorage.GetItemAsStringAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var cut = Render<CookieConsent>();

        cut.Find("button.btn-accept").Click();

        cut.FindAll("div.consent-bar").ShouldBeEmpty();
    }

    [Fact]
    public void when_decline_is_clicked_then_the_banner_is_hidden()
    {
        localStorage.GetItemAsStringAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var cut = Render<CookieConsent>();

        cut.Find("button.btn-decline").Click();

        cut.FindAll("div.consent-bar").ShouldBeEmpty();
    }
}
