namespace AStarDev.CloudSync.ViewModels.Accounts;

/// <summary>Where the plaintext token file setting is.</summary>
public abstract record PlaintextCacheConsentSettingsStep
{
    private PlaintextCacheConsentSettingsStep()
    {
    }

    /// <summary>No consent is remembered, so the setting is hidden.</summary>
    public sealed record NotGranted : PlaintextCacheConsentSettingsStep;

    /// <summary>The consent is remembered; the user can withdraw it.</summary>
    public sealed record Granted : PlaintextCacheConsentSettingsStep;

    /// <summary>The user asked to withdraw the consent and must confirm.</summary>
    public sealed record Confirming : PlaintextCacheConsentSettingsStep;

    /// <summary>The consent was withdrawn and the file deleted; the accounts must sign in again.</summary>
    public sealed record Withdrawn : PlaintextCacheConsentSettingsStep;

    /// <summary>The consent could not be withdrawn.</summary>
    public sealed record Failed : PlaintextCacheConsentSettingsStep;
}
