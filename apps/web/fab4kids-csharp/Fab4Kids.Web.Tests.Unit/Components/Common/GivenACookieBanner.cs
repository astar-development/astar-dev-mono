using Blazored.LocalStorage;
using Bunit;
using Fab4Kids.Web.Components.Common;
using Fab4Kids.Web.Consent;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Fab4Kids.Web.Tests.Unit.Components.Common;

public class GivenACookieBanner : Bunit.BunitContext
{
    private readonly ILocalStorageService localStorage = Substitute.For<ILocalStorageService>();

    public GivenACookieBanner()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(new CookieConsentState(localStorage));
    }

    [Fact]
    public void when_no_preference_is_stored_then_the_banner_is_shown()
    {
        localStorage.GetItemAsStringAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);

        var cut = Render<CookieBanner>();

        cut.FindAll("div.cookie-banner").Count.ShouldBe(1);
    }

    [Fact]
    public void when_a_preference_is_already_stored_then_the_banner_is_not_shown()
    {
        localStorage.GetItemAsStringAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("accepted");

        var cut = Render<CookieBanner>();

        cut.FindAll("div.cookie-banner").ShouldBeEmpty();
    }

    [Fact]
    public void when_accept_all_is_clicked_then_the_banner_is_hidden()
    {
        localStorage.GetItemAsStringAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var cut = Render<CookieBanner>();

        cut.Find("button.btn-primary").Click();

        cut.FindAll("div.cookie-banner").ShouldBeEmpty();
    }

    [Fact]
    public void when_essential_only_is_clicked_then_the_banner_is_hidden()
    {
        localStorage.GetItemAsStringAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var cut = Render<CookieBanner>();

        cut.Find("button.btn-secondary").Click();

        cut.FindAll("div.cookie-banner").ShouldBeEmpty();
    }
}
