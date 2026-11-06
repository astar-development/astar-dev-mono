using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakePlaintextCacheConsentSettings : IPlaintextCacheConsentSettings
{
    public PlaintextCacheConsent Consent { get; set; } = PlaintextCacheConsentFactory.CreateDeclined();

    public bool WithdrawFails { get; init; }

    public int Withdrawals { get; private set; }

    public PlaintextCacheConsent Current() => Consent;

    public Task<Result<Unit, ErrorResponse>> WithdrawAsync()
    {
        Withdrawals++;

        if (WithdrawFails)
            return Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("The plaintext token file could not be deleted.")));

        Consent = PlaintextCacheConsentFactory.CreateDeclined();

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }
}
