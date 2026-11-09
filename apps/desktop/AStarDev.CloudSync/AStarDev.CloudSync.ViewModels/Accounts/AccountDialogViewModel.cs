using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Accounts;

/// <summary>The modal dialog for removing an account and for signing one in again (#1474). It is the app's <see cref="IAccountCommands"/>.</summary>
/// <param name="actions">Removes and re-authenticates accounts.</param>
/// <param name="localization">Supplies the text.</param>
public sealed class AccountDialogViewModel(IAccountActions actions, ILocalizationService localization) : LocalizedObservableObject(localization), IAccountCommands
{
    private TaskCompletionSource closed = new();
    private Option<AccountViewModel> pendingRemoval = Option.None<AccountViewModel>();

    /// <summary>What the dialog shows.</summary>
    public AccountDialogStep Step { get; private set; } = AccountDialogStepFactory.CreateClosed();

    /// <summary>Whether the dialog is open.</summary>
    public bool IsOpen => Step is not AccountDialogStep.Closed;

    /// <summary>Whether the removal confirmation shows.</summary>
    public bool IsConfirming => Step is AccountDialogStep.ConfirmingRemoval;

    /// <summary>Whether a removal or sign-in is running.</summary>
    public bool IsWorking => Step is AccountDialogStep.Working;

    /// <summary>Whether a failure message shows.</summary>
    public bool IsShowingMessage => Step is AccountDialogStep.Message;

    /// <summary>The dialog title.</summary>
    public string TitleText => Localization.GetLocal(Step is AccountDialogStep.ConfirmingRemoval || pendingRemoval.Match(_ => true, () => false) ? "AccountDialog.Remove.Title" : "AccountDialog.SignIn.Title");

    /// <summary>The removal question, naming the account and saying local files are kept.</summary>
    public string ConfirmText => Step is AccountDialogStep.ConfirmingRemoval confirming ? Localization.GetLocal("AccountDialog.Remove.Prompt", confirming.Account.Name, confirming.Account.Email) : string.Empty;

    /// <summary>The progress text.</summary>
    public string WorkingText => Step is AccountDialogStep.Working working ? working.Text : string.Empty;

    /// <summary>The failure message.</summary>
    public string MessageText => Step is AccountDialogStep.Message message ? message.Text : string.Empty;

    /// <summary>The confirm button text.</summary>
    public string RemoveText => Localization.GetLocal("AccountDialog.Remove.Confirm");

    /// <summary>The cancel button text.</summary>
    public string CancelText => Localization.GetLocal("AccountDialog.Cancel");

    /// <summary>The close button text.</summary>
    public string CloseText => Localization.GetLocal("AccountDialog.Close");

    /// <summary>Removes the account being confirmed.</summary>
    public IAsyncRelayCommand ConfirmRemoveCommand => field ??= new AsyncRelayCommand(ConfirmRemoveAsync);

    /// <summary>Cancels the confirmation or closes a message.</summary>
    public IRelayCommand CloseCommand => field ??= new RelayCommand(CloseUnlessWorking);

    /// <inheritdoc />
    public async Task SignInAsync(AccountViewModel account, CancellationToken cancellationToken)
    {
        await Task.FromException(new NotImplementedException($"{actions}"));
    }

    /// <inheritdoc />
    public async Task RemoveAsync(AccountViewModel account, CancellationToken cancellationToken)
    {
        await Task.FromException(new NotImplementedException($"{actions}"));
    }

    private async Task ConfirmRemoveAsync(CancellationToken cancellationToken)
    {
        await Task.FromException(new NotImplementedException($"{actions}"));
    }

    private Unit ShowRemovalFailure(AccountRemovalFailure failure)
    {
        SetStep(AccountDialogStepFactory.CreateMessage(Localization.GetLocal(RemovalFailureKey(failure))));

        return Unit.Instance;
    }

    private Unit ShowSignInFailure(ReauthFailure failure, AccountViewModel account)
    {
        if (failure is ReauthFailure.Cancelled)
            return Close();

        SetStep(AccountDialogStepFactory.CreateMessage(Localization.GetLocal(SignInFailureKey(failure), account.Email)));

        return Unit.Instance;
    }

    private static string RemovalFailureKey(AccountRemovalFailure failure) => failure switch
    {
        AccountRemovalFailure.NotFound => "AccountDialog.Remove.Failure.NotFound",
        AccountRemovalFailure.SyncStillRunning => "AccountDialog.Failure.SyncStillRunning",
        AccountRemovalFailure.Provider => "AccountDialog.Remove.Failure.Provider",
        _ => "AccountDialog.Remove.Failure.Store",
    };

    private static string SignInFailureKey(ReauthFailure failure) => failure switch
    {
        ReauthFailure.DifferentAccount => "AccountDialog.SignIn.Failure.DifferentAccount",
        ReauthFailure.SyncStillRunning => "AccountDialog.Failure.SyncStillRunning",
        _ => "AccountDialog.SignIn.Failure.Provider",
    };

    private void CloseUnlessWorking()
    {
        if (!IsWorking)
            _ = Close();
    }

    private Unit Close()
    {
        pendingRemoval = Option.None<AccountViewModel>();
        SetStep(AccountDialogStepFactory.CreateClosed());
        _ = closed.TrySetResult();

        return Unit.Instance;
    }

    private void SetStep(AccountDialogStep step)
    {
        Step = step;
        OnPropertyChanged(nameof(Step));
        OnPropertyChanged(nameof(IsOpen));
        OnPropertyChanged(nameof(IsConfirming));
        OnPropertyChanged(nameof(IsWorking));
        OnPropertyChanged(nameof(IsShowingMessage));
        OnPropertyChanged(nameof(TitleText));
        OnPropertyChanged(nameof(ConfirmText));
        OnPropertyChanged(nameof(WorkingText));
        OnPropertyChanged(nameof(MessageText));
    }
}
