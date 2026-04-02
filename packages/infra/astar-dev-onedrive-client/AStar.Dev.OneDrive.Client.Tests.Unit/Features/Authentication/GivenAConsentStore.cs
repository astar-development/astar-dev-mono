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

        var result = await _consentStore.GetConsentDecisionAsync(accountId, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Option<bool>.None>();
    }

    [Fact]
    public async Task when_set_consent_decision_async_then_can_retrieve_it()
    {
        var accountId = Guid.NewGuid();

        var setResult = await _consentStore.SetConsentDecisionAsync(accountId, true, TestContext.Current.CancellationToken);
        var getResult = await _consentStore.GetConsentDecisionAsync(accountId, TestContext.Current.CancellationToken);

        setResult.ShouldBeOfType<Result<bool, string>.Ok>();
        getResult.ShouldBeOfType<Option<bool>.Some>().Value.ShouldBeTrue();
    }

    [Fact]
    public async Task when_set_consent_decision_async_with_false_then_can_retrieve_it()
    {
        var accountId = Guid.NewGuid();

        var setResult = await _consentStore.SetConsentDecisionAsync(accountId, false, TestContext.Current.CancellationToken);
        var getResult = await _consentStore.GetConsentDecisionAsync(accountId, TestContext.Current.CancellationToken);

        setResult.ShouldBeOfType<Result<bool, string>.Ok>();
        getResult.ShouldBeOfType<Option<bool>.Some>().Value.ShouldBeFalse();
    }

    [Fact]
    public async Task when_set_consent_decision_async_multiple_accounts_then_are_independent()
    {
        var account1 = Guid.NewGuid();
        var account2 = Guid.NewGuid();
        await _consentStore.SetConsentDecisionAsync(account1, true, TestContext.Current.CancellationToken);
        await _consentStore.SetConsentDecisionAsync(account2, false, TestContext.Current.CancellationToken);

        var result1 = await _consentStore.GetConsentDecisionAsync(account1, TestContext.Current.CancellationToken);
        var result2 = await _consentStore.GetConsentDecisionAsync(account2, TestContext.Current.CancellationToken);

        result1.ShouldBeOfType<Option<bool>.Some>().Value.ShouldBeTrue();
        result2.ShouldBeOfType<Option<bool>.Some>().Value.ShouldBeFalse();
    }
}
