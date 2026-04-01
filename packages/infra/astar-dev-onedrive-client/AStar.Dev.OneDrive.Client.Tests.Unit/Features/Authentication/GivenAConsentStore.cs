using AStar.Dev.OneDrive.Client.Features.Authentication;
using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Features.Authentication;

public sealed class GivenAConsentStore
{
    private readonly ConsentStore _consentStore = new();

    [Fact]
    public async Task when_get_consent_decision_async_for_unknown_account_then_returns_none()
    {
        var accountId = Guid.NewGuid();

        var result = await _consentStore.GetConsentDecisionAsync(accountId);

        result.Match(
            onSome: _ => throw new Xunit.Sdk.XunitException("Should be None"),
            onNone: () => { });
    }

    [Fact]
    public async Task when_set_consent_decision_async_then_can_retrieve_it()
    {
        var accountId = Guid.NewGuid();

        var setResult = await _consentStore.SetConsentDecisionAsync(accountId, true);
        var getResult = await _consentStore.GetConsentDecisionAsync(accountId);

        setResult.Match(
            onSuccess: _ => { },
            onFailure: e => throw new Xunit.Sdk.XunitException($"Set failed: {e}"));
        getResult.Match(
            onSome: consented => consented.ShouldBeTrue(),
            onNone: () => throw new Xunit.Sdk.XunitException("Should be Some(true)"));
    }

    [Fact]
    public async Task when_set_consent_decision_async_with_false_then_can_retrieve_it()
    {
        var accountId = Guid.NewGuid();

        var setResult = await _consentStore.SetConsentDecisionAsync(accountId, false);
        var getResult = await _consentStore.GetConsentDecisionAsync(accountId);

        setResult.Match(
            onSuccess: _ => { },
            onFailure: e => throw new Xunit.Sdk.XunitException($"Set failed: {e}"));
        getResult.Match(
            onSome: consented => consented.ShouldBeFalse(),
            onNone: () => throw new Xunit.Sdk.XunitException("Should be Some(false)"));
    }

    [Fact]
    public async Task when_set_consent_decision_async_multiple_accounts_then_are_independent()
    {
        var account1 = Guid.NewGuid();
        var account2 = Guid.NewGuid();
        await _consentStore.SetConsentDecisionAsync(account1, true);
        await _consentStore.SetConsentDecisionAsync(account2, false);

        var result1 = await _consentStore.GetConsentDecisionAsync(account1);
        var result2 = await _consentStore.GetConsentDecisionAsync(account2);

        result1.Match(
            onSome: c => c.ShouldBeTrue(),
            onNone: () => throw new Xunit.Sdk.XunitException("Account1 missing"));
        result2.Match(
            onSome: c => c.ShouldBeFalse(),
            onNone: () => throw new Xunit.Sdk.XunitException("Account2 missing"));
    }
}
