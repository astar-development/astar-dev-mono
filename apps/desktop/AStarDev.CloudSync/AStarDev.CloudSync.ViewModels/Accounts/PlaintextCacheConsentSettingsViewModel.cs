using AStarDev.CloudSync.Core.Accounts;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Accounts;

/// <summary>The setting that shows a remembered consent to the plaintext token file and lets the user withdraw it.</summary>
/// <param name="settings">Reads and withdraws the consent.</param>
/// <param name="localization">Supplies the text.</param>
public sealed class PlaintextCacheConsentSettingsViewModel(IPlaintextCacheConsentSettings settings, ILocalizationService localization) : LocalizedObservableObject(localization)
{
    /// <summary>Where the setting is.</summary>
    public PlaintextCacheConsentSettingsStep Step => throw new NotImplementedException();

    /// <summary>Whether the setting shows at all.</summary>
    public bool IsVisible => throw new NotImplementedException();

    /// <summary>Whether the withdraw button shows.</summary>
    public bool IsGranted => throw new NotImplementedException();

    /// <summary>Whether the confirmation shows.</summary>
    public bool IsConfirming => throw new NotImplementedException();

    /// <summary>Whether the result of a withdrawal shows.</summary>
    public bool IsShowingResult => throw new NotImplementedException();

    /// <summary>The setting's heading.</summary>
    public string HeadingText => throw new NotImplementedException();

    /// <summary>Explains that the tokens are in a plaintext file.</summary>
    public string GrantedText => throw new NotImplementedException();

    /// <summary>The label of the withdraw button.</summary>
    public string WithdrawText => throw new NotImplementedException();

    /// <summary>The confirmation question.</summary>
    public string ConfirmMessageText => throw new NotImplementedException();

    /// <summary>The label of the button that confirms the withdrawal.</summary>
    public string ConfirmText => throw new NotImplementedException();

    /// <summary>The label of the button that keeps the consent.</summary>
    public string CancelText => throw new NotImplementedException();

    /// <summary>The result of the withdrawal.</summary>
    public string ResultText => throw new NotImplementedException();

    /// <summary>Asks the user to confirm the withdrawal.</summary>
    public IRelayCommand WithdrawCommand => throw new NotImplementedException();

    /// <summary>Keeps the consent.</summary>
    public IRelayCommand CancelCommand => throw new NotImplementedException();

    /// <summary>Withdraws the consent.</summary>
    public IAsyncRelayCommand ConfirmCommand => throw new NotImplementedException();

    /// <summary>Reads the consent again, e.g. when the settings open.</summary>
    public void Refresh() => throw new NotImplementedException(settings.GetType().Name);
}
