using AStarDev.CloudSync.Core.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Remembers between runs that the user agreed to a plaintext token file.</summary>
public interface IPlaintextCacheConsentStore
{
    /// <summary>Reads the remembered answer.</summary>
    /// <returns><see cref="PlaintextCacheConsent.Granted"/> if the user agreed on an earlier run; otherwise none.</returns>
    Option<PlaintextCacheConsent> Load();

    /// <summary>Remembers <see cref="PlaintextCacheConsent.Granted"/>, or forgets an earlier consent for <see cref="PlaintextCacheConsent.Declined"/>.</summary>
    /// <param name="consent">The user's answer.</param>
    /// <returns><see cref="Unit"/>, or an error if the answer could not be saved.</returns>
    Result<Unit, ErrorResponse> Save(PlaintextCacheConsent consent);
}
