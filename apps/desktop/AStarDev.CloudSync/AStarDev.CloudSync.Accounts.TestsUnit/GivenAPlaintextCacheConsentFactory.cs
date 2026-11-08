using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Accounts.TestsUnit;

public class GivenAPlaintextCacheConsentFactory
{
    [Fact]
    public void when_consent_is_granted_then_the_answer_is_granted() => PlaintextCacheConsentFactory.CreateGranted().ShouldBeOfType<PlaintextCacheConsent.Granted>();

    [Fact]
    public void when_consent_is_declined_then_the_answer_is_declined() => PlaintextCacheConsentFactory.CreateDeclined().ShouldBeOfType<PlaintextCacheConsent.Declined>();
}
