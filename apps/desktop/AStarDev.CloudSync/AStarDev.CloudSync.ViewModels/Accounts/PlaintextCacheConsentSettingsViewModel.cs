using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Accounts;

/// <summary>The setting that shows a remembered consent to the plaintext token file and lets the user withdraw it.</summary>
/// <param name="settings">Reads and withdraws the consent.</param>
/// <param name="localization">Supplies the text.</param>
public sealed class PlaintextCacheConsentSettingsViewModel(IPlaintextCacheConsentSettings settings, ILocalizationService localization) : LocalizedObservableObject(localization)
{
    /// <summary>Where the setting is.</summary>
    public PlaintextCacheConsentSettingsStep Step { get; private set; } = FromConsent(settings.Current());

    /// <summary>Whether the setting shows at all.</summary>
    public bool IsVisible => Step is not PlaintextCacheConsentSettingsStep.NotGranted;

    /// <summary>Whether the withdraw button shows.</summary>
    public bool IsGranted => Step is PlaintextCacheConsentSettingsStep.Granted or PlaintextCacheConsentSettingsStep.Failed;

    /// <summary>Whether the confirmation shows.</summary>
    public bool IsConfirming => Step is PlaintextCacheConsentSettingsStep.Confirming;

    /// <summary>Whether the result of a withdrawal shows.</summary>
    public bool IsShowingResult => Step is PlaintextCacheConsentSettingsStep.Withdrawn or PlaintextCacheConsentSettingsStep.Failed;

    /// <summary>The setting's heading.</summary>
    public string HeadingText => Localization.GetLocal("Settings.PlaintextTokens");

    /// <summary>Explains that the tokens are in a plaintext file.</summary>
    public string GrantedText => Localization.GetLocal("Settings.PlaintextTokens.Granted");

    /// <summary>The label of the withdraw button.</summary>
    public string WithdrawText => Localization.GetLocal("Settings.PlaintextTokens.Withdraw");

    /// <summary>The confirmation question.</summary>
    public string ConfirmMessageText => Localization.GetLocal("Settings.PlaintextTokens.Confirm");

    /// <summary>The label of the button that confirms the withdrawal.</summary>
    public string ConfirmText => Localization.GetLocal("Settings.PlaintextTokens.ConfirmButton");

    /// <summary>The label of the button that keeps the consent.</summary>
    public string CancelText => Localization.GetLocal("Settings.PlaintextTokens.Cancel");

    /// <summary>The result of the withdrawal, or empty before one.</summary>
    public string ResultText => Step switch
    {
        PlaintextCacheConsentSettingsStep.Withdrawn => Localization.GetLocal("Settings.PlaintextTokens.Withdrawn"),
        PlaintextCacheConsentSettingsStep.Failed => Localization.GetLocal("Settings.PlaintextTokens.Failed"),
        _ => string.Empty
    };

    /// <summary>Asks the user to confirm the withdrawal.</summary>
    public IRelayCommand WithdrawCommand => field ??= new RelayCommand(() => MoveTo(PlaintextCacheConsentSettingsStepFactory.CreateConfirming()));

    /// <summary>Keeps the consent.</summary>
    public IRelayCommand CancelCommand => field ??= new RelayCommand(() => MoveTo(PlaintextCacheConsentSettingsStepFactory.CreateGranted()));

    /// <summary>Withdraws the consent.</summary>
    public IAsyncRelayCommand ConfirmCommand => field ??= new AsyncRelayCommand(ConfirmAsync);

    /// <summary>Reads the consent again, e.g. when the settings open.</summary>
    public void Refresh() => MoveTo(FromConsent(settings.Current()));

    private static PlaintextCacheConsentSettingsStep FromConsent(PlaintextCacheConsent consent) =>
        consent is PlaintextCacheConsent.Granted ? PlaintextCacheConsentSettingsStepFactory.CreateGranted() : PlaintextCacheConsentSettingsStepFactory.CreateNotGranted();

    private async Task ConfirmAsync() =>
        MoveTo(await settings.WithdrawAsync().MatchAsync(_ => PlaintextCacheConsentSettingsStepFactory.CreateWithdrawn(), _ => PlaintextCacheConsentSettingsStepFactory.CreateFailed()));

    private void MoveTo(PlaintextCacheConsentSettingsStep step)
    {
        Step = step;
        OnPropertyChanged(nameof(Step));
        OnPropertyChanged(nameof(IsVisible));
        OnPropertyChanged(nameof(IsGranted));
        OnPropertyChanged(nameof(IsConfirming));
        OnPropertyChanged(nameof(IsShowingResult));
        OnPropertyChanged(nameof(ResultText));
    }
}
