namespace AStarDev.CloudSync.Core.Accounts;

/// <summary>The user's answer when the secure token store is unavailable and the tokens could be kept in a plaintext file instead.</summary>
public abstract record PlaintextCacheConsent
{
    private PlaintextCacheConsent()
    {
    }

    /// <summary>The user agreed to keep the tokens in a plaintext file.</summary>
    public sealed record Granted : PlaintextCacheConsent;

    /// <summary>The user did not agree; the account is not added.</summary>
    public sealed record Declined : PlaintextCacheConsent;
}
