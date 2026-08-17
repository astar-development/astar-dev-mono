using AStar.Dev.FunctionalParadigm;
using AStarDev.Web.Consent;
using Blazored.LocalStorage;
using NSubstitute;

namespace AStarDev.Web.TestsUnit.Consent;

public class GivenACookieConsentState
{
    private readonly ILocalStorageService localStorage = Substitute.For<ILocalStorageService>();

    [Fact]
    public void when_constructed_then_no_preference_is_recorded()
    {
        var sut = new CookieConsentState(localStorage);

        sut.AnalyticsAccepted.TryGetValue(out _).ShouldBeFalse();
    }

    [Fact]
    public async Task when_initialized_and_nothing_is_stored_then_no_preference_is_recorded()
    {
        localStorage.GetItemAsStringAsync("cookie-consent-analytics", Arg.Any<CancellationToken>()).Returns((string?)null);
        var sut = new CookieConsentState(localStorage);

        await sut.InitializeAsync();

        sut.AnalyticsAccepted.TryGetValue(out _).ShouldBeFalse();
    }

    [Fact]
    public async Task when_initialized_and_true_is_stored_then_analytics_is_accepted()
    {
        localStorage.GetItemAsStringAsync("cookie-consent-analytics", Arg.Any<CancellationToken>()).Returns("true");
        var sut = new CookieConsentState(localStorage);

        await sut.InitializeAsync();

        sut.AnalyticsAccepted.TryGetValue(out bool accepted).ShouldBeTrue();
        accepted.ShouldBeTrue();
    }

    [Fact]
    public async Task when_initialized_and_false_is_stored_then_analytics_is_declined()
    {
        localStorage.GetItemAsStringAsync("cookie-consent-analytics", Arg.Any<CancellationToken>()).Returns("false");
        var sut = new CookieConsentState(localStorage);

        await sut.InitializeAsync();

        sut.AnalyticsAccepted.TryGetValue(out bool accepted).ShouldBeTrue();
        accepted.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_preference_is_set_then_it_is_persisted_to_local_storage()
    {
        var sut = new CookieConsentState(localStorage);

        await sut.SetPreferenceAsync(analyticsAccepted: true);

        sut.AnalyticsAccepted.TryGetValue(out bool accepted).ShouldBeTrue();
        accepted.ShouldBeTrue();
        await localStorage.Received(1).SetItemAsStringAsync("cookie-consent-analytics", "true", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_preference_is_set_then_on_change_is_raised()
    {
        var sut = new CookieConsentState(localStorage);
        bool raised = false;
        sut.OnChange += () => raised = true;

        await sut.SetPreferenceAsync(analyticsAccepted: false);

        raised.ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_preference_is_cleared_then_it_is_removed_from_local_storage_and_no_preference_is_recorded()
    {
        var sut = new CookieConsentState(localStorage);
        await sut.SetPreferenceAsync(analyticsAccepted: true);

        await sut.ClearPreferenceAsync();

        sut.AnalyticsAccepted.TryGetValue(out _).ShouldBeFalse();
        await localStorage.Received(1).RemoveItemAsync("cookie-consent-analytics", Arg.Any<CancellationToken>());
    }
}
