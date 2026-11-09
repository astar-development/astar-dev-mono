using System.ComponentModel;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using AStarDev.LoggingExtensions;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.ViewModels.Accounts;

/// <summary>The add-account dialog: signs in, shows the plaintext token question when needed, and shows the "already added" notice or a failure message. A new account or a cancel closes it by itself.</summary>
public sealed class AddAccountViewModel : LocalizedObservableObject, IAddAccountDialog, IDisposable
{
    private readonly IProviderCapability<IAccountAdder> accountAdders;
    private readonly ILogger<AddAccountViewModel> logger;
    private TaskCompletionSource closed = CreateClosedSignal(true);
    private CancellationTokenSource attempt = new();
    private CancellationToken callerToken = CancellationToken.None;
    private CancellationTokenRegistration callerRegistration;

    /// <summary>Creates the dialog view model.</summary>
    /// <param name="accountAdders">Finds the adder for the provider being added; OneDrive until the provider picker (#1415).</param>
    /// <param name="consent">The plaintext token question shown inside the dialog.</param>
    /// <param name="localization">Supplies the dialog text.</param>
    /// <param name="logger">Reports an attempt that failed unexpectedly, without account details.</param>
    public AddAccountViewModel(IProviderCapability<IAccountAdder> accountAdders, TokenCacheConsentViewModel consent, ILocalizationService localization, ILogger<AddAccountViewModel> logger) : base(localization)
    {
        this.logger = logger;
        this.accountAdders = accountAdders;
        Consent = consent;
        consent.PropertyChanged += OnConsentChanged;
    }

    /// <summary>The plaintext token question.</summary>
    public TokenCacheConsentViewModel Consent { get; }

    /// <summary>Where the dialog is.</summary>
    public AddAccountStep Step { get; private set; } = AddAccountStepFactory.CreateClosed();

    /// <summary>Whether the dialog is showing.</summary>
    public bool IsOpen => Step is not AddAccountStep.Closed;

    /// <summary>Whether the signing-in panel is showing.</summary>
    public bool IsSigningIn => Step is AddAccountStep.SigningIn && !Consent.IsAsking;

    /// <summary>Whether the plaintext token question is showing.</summary>
    public bool IsAskingConsent => Step is AddAccountStep.SigningIn && Consent.IsAsking;

    /// <summary>Whether a notice or failure message is showing.</summary>
    public bool IsShowingMessage => Step is AddAccountStep.AlreadyAdded or AddAccountStep.Failed;

    /// <summary>Whether the failed attempt can be tried again.</summary>
    public bool CanTryAgain => Step is AddAccountStep.Failed;

    /// <summary>The dialog heading.</summary>
    public string TitleText => Localization.GetLocal("AddAccount.Title");

    /// <summary>The instruction shown while signing in.</summary>
    public string SigningInText => Localization.GetLocal("AddAccount.SigningIn");

    /// <summary>The notice or failure message for the current step.</summary>
    public string MessageText => Step switch
    {
        AddAccountStep.AlreadyAdded => Localization.GetLocal("AddAccount.AlreadyAdded"),
        AddAccountStep.Failed failed => Localization.GetLocal(FailureKey(failed.Failure)),
        _ => string.Empty,
    };

    /// <summary>The label of the button that cancels sign-in.</summary>
    public string CancelText => Localization.GetLocal("AddAccount.Cancel");

    /// <summary>The label of the button that closes the dialog.</summary>
    public string CloseText => Localization.GetLocal("AddAccount.Close");

    /// <summary>The label of the button that tries again.</summary>
    public string TryAgainText => Localization.GetLocal("AddAccount.TryAgain");

    /// <summary>Cancels sign-in, which closes the dialog.</summary>
    public IRelayCommand CancelCommand => field ??= new RelayCommand(() => attempt.Cancel());

    /// <summary>Closes the dialog after a notice or failure.</summary>
    public IRelayCommand CloseCommand => field ??= new RelayCommand(CloseMessage);

    /// <summary>Starts a new attempt after a failure.</summary>
    public IAsyncRelayCommand TryAgainCommand => field ??= new AsyncRelayCommand(() => CanTryAgain ? RunAttemptAsync() : Task.CompletedTask);

    /// <inheritdoc />
    public Task ShowAsync(CancellationToken cancellationToken)
    {
        if (IsOpen)
            return closed.Task;

        closed = CreateClosedSignal(false);
        callerToken = cancellationToken;
        callerRegistration = cancellationToken.Register(Close);
        _ = RunAttemptAsync();

        return closed.Task;
    }

    /// <summary>Releases the sign-in cancellation source, stops following the consent question and cancels a pending <see cref="ShowAsync"/>.</summary>
    public void Dispose()
    {
        Consent.PropertyChanged -= OnConsentChanged;
        _ = callerRegistration.Unregister();
        attempt.Dispose();
        _ = closed.TrySetCanceled(CancellationToken.None);
    }

    private static TaskCompletionSource CreateClosedSignal(bool alreadyClosed)
    {
        var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        if (alreadyClosed)
            signal.SetResult();

        return signal;
    }

    private static string FailureKey(AccountAddFailure failure) => failure switch
    {
        AccountAddFailure.BrowserUnavailable => "AddAccount.Failure.BrowserUnavailable",
        AccountAddFailure.AuthRejected => "AddAccount.Failure.AuthRejected",
        AccountAddFailure.Network => "AddAccount.Failure.Network",
        AccountAddFailure.TokenCacheUnavailable => "AddAccount.Failure.TokenCacheUnavailable",
        AccountAddFailure.DriveUnavailable => "AddAccount.Failure.DriveUnavailable",
        _ => "AddAccount.Failure.PersistFailed",
    };

    private async Task RunAttemptAsync()
    {
        attempt.Dispose();
        attempt = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
        SetStep(AddAccountStepFactory.CreateSigningIn());

        var outcome = await accountAdders.ForProvider(CloudProviderFactory.CreateOneDrive())
            .Match(adder => Try.RunAsync(() => adder.AddAsync(attempt.Token), CancellationToken.None).MatchAsync(added => added, Unexpected), AdderUnavailable);

        switch (outcome)
        {
            case AccountAddOutcome.ReAuthenticated:
                SetStep(AddAccountStepFactory.CreateAlreadyAdded());
                break;
            case AccountAddOutcome.Failed failed:
                SetStep(AddAccountStepFactory.CreateFailed(failed.Failure));
                break;
            default:
                Close();
                break;
        }
    }

    private Task<AccountAddOutcome> AdderUnavailable(ProviderError error)
    {
        AddAccountDialogLog.AttemptFailedUnexpectedly(logger, error.GetType().FullName ?? error.GetType().Name);

        return Task.FromResult(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateAuthRejected()));
    }

    private AccountAddOutcome Unexpected(Exception exception)
    {
        AddAccountDialogLog.AttemptFailedUnexpectedly(logger, exception.ErrorType());

        return AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateAuthRejected());
    }

    private void CloseMessage()
    {
        if (IsShowingMessage)
            Close();
    }

    private void Close()
    {
        _ = callerRegistration.Unregister();
        SetStep(AddAccountStepFactory.CreateClosed());
        _ = closed.TrySetResult();
    }

    private void SetStep(AddAccountStep step)
    {
        Step = step;
        OnPropertyChanged(nameof(Step));
        OnPropertyChanged(nameof(IsOpen));
        OnPropertyChanged(nameof(IsSigningIn));
        OnPropertyChanged(nameof(IsAskingConsent));
        OnPropertyChanged(nameof(IsShowingMessage));
        OnPropertyChanged(nameof(CanTryAgain));
        OnPropertyChanged(nameof(MessageText));
    }

    private void OnConsentChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TokenCacheConsentViewModel.IsAsking))
            return;

        OnPropertyChanged(nameof(IsSigningIn));
        OnPropertyChanged(nameof(IsAskingConsent));
    }
}
