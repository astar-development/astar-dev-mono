namespace AStarDev.CloudSync.ViewModels.Accounts;

/// <summary>Creates <see cref="PlaintextCacheConsentSettingsStep"/> cases.</summary>
public static class PlaintextCacheConsentSettingsStepFactory
{
    /// <summary>Creates the hidden step for no remembered consent.</summary>
    public static PlaintextCacheConsentSettingsStep CreateNotGranted() => new PlaintextCacheConsentSettingsStep.NotGranted();

    /// <summary>Creates the step for a remembered consent.</summary>
    public static PlaintextCacheConsentSettingsStep CreateGranted() => new PlaintextCacheConsentSettingsStep.Granted();

    /// <summary>Creates the confirmation step.</summary>
    public static PlaintextCacheConsentSettingsStep CreateConfirming() => new PlaintextCacheConsentSettingsStep.Confirming();

    /// <summary>Creates the step for a withdrawn consent.</summary>
    public static PlaintextCacheConsentSettingsStep CreateWithdrawn() => new PlaintextCacheConsentSettingsStep.Withdrawn();

    /// <summary>Creates the step for a withdrawal that failed.</summary>
    public static PlaintextCacheConsentSettingsStep CreateFailed() => new PlaintextCacheConsentSettingsStep.Failed();
}
