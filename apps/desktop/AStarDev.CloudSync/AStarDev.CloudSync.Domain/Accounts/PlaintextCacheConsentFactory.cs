using AStarDev.CloudSync.Core.Accounts;

namespace AStarDev.CloudSync.Domain.Accounts;

/// <summary>Creates <see cref="PlaintextCacheConsent"/> answers.</summary>
public static class PlaintextCacheConsentFactory
{
    /// <summary>Creates the answer for a user who agreed to a plaintext token file.</summary>
    public static PlaintextCacheConsent CreateGranted() => new PlaintextCacheConsent.Granted();

    /// <summary>Creates the answer for a user who did not agree to a plaintext token file.</summary>
    public static PlaintextCacheConsent CreateDeclined() => new PlaintextCacheConsent.Declined();
}
