using AStar.Dev.OneDrive.Client.Features.Authentication;
using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Features.Authentication;

public sealed class GivenAConsentStore
{
    private readonly ConsentStore _consentStore = new();

    [Fact]
    public async Task WhenGetConsentDecisionAsync_ForUnknownAccount_ThenReturnsNone()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act
        var result = await _consentStore.GetConsentDecisionAsync(accountId);

        // Assert
        result.Match(
            onSome: _ => throw new Xunit.Sdk.XunitException("Should be None"),
            onNone: () => { });
    }

    [Fact]
    public async Task WhenSetConsentDecisionAsync_ThenCanRetrieveIt()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act
        var setResult = await _consentStore.SetConsentDecisionAsync(accountId, true);
        var getResult = await _consentStore.GetConsentDecisionAsync(accountId);

        // Assert
        setResult.Match(
            onSuccess: _ => { },
            onFailure: e => throw new Xunit.Sdk.XunitException($"Set failed: {e}"));

        getResult.Match(
            onSome: consented => consented.ShouldBeTrue(),
            onNone: () => throw new Xunit.Sdk.XunitException("Should be Some(true)"));
    }

    [Fact]
    public async Task WhenSetConsentDecisionAsync_WithFalse_ThenCanRetrieveIt()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act
        var setResult = await _consentStore.SetConsentDecisionAsync(accountId, false);
        var getResult = await _consentStore.GetConsentDecisionAsync(accountId);

        // Assert
        setResult.Match(
            onSuccess: _ => { },
            onFailure: e => throw new Xunit.Sdk.XunitException($"Set failed: {e}"));

        getResult.Match(
            onSome: consented => consented.ShouldBeFalse(),
            onNone: () => throw new Xunit.Sdk.XunitException("Should be Some(false)"));
    }

    [Fact]
    public async Task WhenSetConsentDecisionAsync_MultipleAccounts_ThenAreIndependent()
    {
        // Arrange
        var account1 = Guid.NewGuid();
        var account2 = Guid.NewGuid();

        // Act
        await _consentStore.SetConsentDecisionAsync(account1, true);
        await _consentStore.SetConsentDecisionAsync(account2, false);

        var result1 = await _consentStore.GetConsentDecisionAsync(account1);
        var result2 = await _consentStore.GetConsentDecisionAsync(account2);

        // Assert
        result1.Match(
            onSome: c => c.ShouldBeTrue(),
            onNone: () => throw new Xunit.Sdk.XunitException("Account1 missing"));

        result2.Match(
            onSome: c => c.ShouldBeFalse(),
            onNone: () => throw new Xunit.Sdk.XunitException("Account2 missing"));
    }
}
