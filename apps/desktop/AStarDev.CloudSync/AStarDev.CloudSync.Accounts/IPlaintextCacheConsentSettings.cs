using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Accounts;

/// <summary>Lets the user see and withdraw an earlier consent to keep the sign-in tokens in a plaintext file.</summary>
public interface IPlaintextCacheConsentSettings
{
    /// <summary>Reads the remembered consent.</summary>
    /// <returns><see cref="PlaintextCacheConsent.Granted"/> while the consent is remembered; otherwise <see cref="PlaintextCacheConsent.Declined"/>.</returns>
    PlaintextCacheConsent Current();

    /// <summary>Withdraws the consent: stops using the plaintext token file, deletes it and forgets the consent. The accounts must then sign in again.</summary>
    /// <returns><see cref="Unit"/>, or an error if the file or the consent could not be removed.</returns>
    Task<Result<Unit, ErrorResponse>> WithdrawAsync();
}
