using AStarDev.CloudSync.Core.Accounts;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Accounts;

/// <summary>The add-account dialog: signs in, shows the plaintext token question when needed, and shows the "already added" notice or a failure message.</summary>
public sealed class AddAccountViewModel : LocalizedObservableObject, IAddAccountDialog
{
    private readonly IAccountAdder accountAdder;

    /// <summary>Creates the dialog view model.</summary>
    /// <param name="accountAdder">Runs the add-account attempt.</param>
    /// <param name="consent">The plaintext token question shown inside the dialog.</param>
    /// <param name="localization">Supplies the dialog text.</param>
    public AddAccountViewModel(IAccountAdder accountAdder, TokenCacheConsentViewModel consent, ILocalizationService localization) : base(localization)
    {
        this.accountAdder = accountAdder;
        Consent = consent;
    }

    /// <summary>The plaintext token question.</summary>
    public TokenCacheConsentViewModel Consent { get; }

    /// <summary>Where the dialog is.</summary>
    public AddAccountStep Step => throw new NotImplementedException($"{accountAdder}");

    /// <summary>Whether the dialog is showing.</summary>
    public bool IsOpen => throw new NotImplementedException();

    /// <summary>Whether the signing-in panel is showing.</summary>
    public bool IsSigningIn => throw new NotImplementedException();

    /// <summary>Whether the plaintext token question is showing.</summary>
    public bool IsAskingConsent => throw new NotImplementedException();

    /// <summary>Whether a notice or failure message is showing.</summary>
    public bool IsShowingMessage => throw new NotImplementedException();

    /// <summary>Whether the failed attempt can be tried again.</summary>
    public bool CanTryAgain => throw new NotImplementedException();

    /// <summary>The dialog heading.</summary>
    public string TitleText => Localization.GetLocal("AddAccount.Title");

    /// <summary>The instruction shown while signing in.</summary>
    public string SigningInText => Localization.GetLocal("AddAccount.SigningIn");

    /// <summary>The notice or failure message for the current step.</summary>
    public string MessageText => throw new NotImplementedException();

    /// <summary>The label of the button that cancels sign-in.</summary>
    public string CancelText => Localization.GetLocal("AddAccount.Cancel");

    /// <summary>The label of the button that closes the dialog.</summary>
    public string CloseText => Localization.GetLocal("AddAccount.Close");

    /// <summary>The label of the button that tries again.</summary>
    public string TryAgainText => Localization.GetLocal("AddAccount.TryAgain");

    /// <summary>Cancels sign-in, which closes the dialog.</summary>
    public IRelayCommand CancelCommand => field ??= new RelayCommand(() => throw new NotImplementedException());

    /// <summary>Closes the dialog after a notice or failure.</summary>
    public IRelayCommand CloseCommand => field ??= new RelayCommand(() => throw new NotImplementedException());

    /// <summary>Starts a new attempt after a failure.</summary>
    public IAsyncRelayCommand TryAgainCommand => field ??= new AsyncRelayCommand(() => throw new NotImplementedException());

    /// <inheritdoc />
    public Task ShowAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}
