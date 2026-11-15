using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Accounts;

/// <summary>The modal dialog for removing an account and for signing one in again (#1474). It is the app's <see cref="IAccountCommands"/>.</summary>
/// <param name="actions">Removes and re-authenticates accounts, and changes sync folders.</param>
/// <param name="syncRoots">Reads an account's current sync folder.</param>
/// <param name="accounts">Names the account a rejected folder clashes with.</param>
/// <param name="folderPicker">Lets the user choose a folder.</param>
/// <param name="concurrencyReader">Reads how many transfers an account runs at once.</param>
/// <param name="concurrencyWriter">Saves how many transfers an account runs at once.</param>
/// <param name="localization">Supplies the text.</param>
public sealed class AccountDialogViewModel(IAccountActions actions, ISyncRootReader syncRoots, IAccountStore accounts, IFolderPicker folderPicker, ITransferConcurrencyReader concurrencyReader, ITransferConcurrencyWriter concurrencyWriter, ILocalizationService localization) : LocalizedObservableObject(localization), IAccountCommands
{
    private TaskCompletionSource closed = new();
    private Option<AccountViewModel> pendingRemoval = Option.None<AccountViewModel>();
    private int transferConcurrency = TransferConcurrencyFactory.Default;
    private string transferConcurrencyMessage = string.Empty;

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

    /// <summary>Whether the sync folder panel shows.</summary>
    public bool IsShowingSyncFolder => Step is AccountDialogStep.SyncFolder;

    /// <summary>Whether the sync folder change confirmation shows.</summary>
    public bool IsConfirmingSyncFolderChange => Step is AccountDialogStep.ConfirmingSyncFolderChange;

    /// <summary>The line above the account's current sync folder.</summary>
    public string SyncFolderText => Step is AccountDialogStep.SyncFolder folder ? Localization.GetLocal("AccountDialog.SyncFolder.Current", folder.Account.Name) : string.Empty;

    /// <summary>The account's current sync folder path, or text saying there is none.</summary>
    public string SyncFolderPathText => Step switch
    {
        AccountDialogStep.SyncFolder folder => folder.CurrentFolderText,
        AccountDialogStep.ConfirmingSyncFolderChange confirming => confirming.CurrentFolderText,
        _ => string.Empty,
    };

    /// <summary>The last change failure, or empty.</summary>
    public string SyncFolderMessageText => Step is AccountDialogStep.SyncFolder folder ? folder.MessageText : string.Empty;

    /// <summary>The line that shows how many transfers the account runs at once.</summary>
    public string TransferConcurrencyText => Localization.GetLocal("AccountDialog.Transfers.Current", transferConcurrency.ToString("N0", Localization.CurrentCulture));

    /// <summary>The help text that explains the trade-off of a higher or lower number.</summary>
    public string TransferConcurrencyHelpText => Localization.GetLocal("AccountDialog.Transfers.Help");

    /// <summary>Why the setting could not be saved, or empty.</summary>
    public string TransferConcurrencyMessageText => transferConcurrencyMessage;

    /// <summary>Whether a save failure shows.</summary>
    public bool HasTransferConcurrencyMessage => transferConcurrencyMessage.Length > 0;

    /// <summary>The label of the button that lowers the number.</summary>
    public string FewerTransfersText => Localization.GetLocal("AccountDialog.Transfers.Fewer");

    /// <summary>The label of the button that raises the number.</summary>
    public string MoreTransfersText => Localization.GetLocal("AccountDialog.Transfers.More");

    /// <summary>Raises the number of transfers at once by one, up to the maximum, and saves it. It applies from the account's next sync.</summary>
    public IAsyncRelayCommand IncreaseTransferConcurrencyCommand => field ??= new AsyncRelayCommand(cancellationToken => ChangeTransferConcurrencyAsync(1, cancellationToken), () => CanChangeTransferConcurrency(1));

    /// <summary>Lowers the number of transfers at once by one, down to the minimum, and saves it. It applies from the account's next sync.</summary>
    public IAsyncRelayCommand DecreaseTransferConcurrencyCommand => field ??= new AsyncRelayCommand(cancellationToken => ChangeTransferConcurrencyAsync(-1, cancellationToken), () => CanChangeTransferConcurrency(-1));

    /// <summary>Whether a change failure shows.</summary>
    public bool HasSyncFolderMessage => SyncFolderMessageText.Length > 0;

    /// <summary>Why the folder cannot be changed right now, or empty.</summary>
    public string SyncFolderSyncingText => Step is AccountDialogStep.SyncFolder { Account.Status.IsSyncing: true } ? Localization.GetLocal("AccountDialog.SyncFolder.Syncing") : string.Empty;

    /// <summary>The confirmation text for moving an account that has synced.</summary>
    public string SyncFolderConfirmText => Step is AccountDialogStep.ConfirmingSyncFolderChange confirming ? Localization.GetLocal("AccountDialog.SyncFolder.Confirm.Prompt", confirming.Account.Name) : string.Empty;

    /// <summary>The Change button text.</summary>
    public string ChangeText => Localization.GetLocal("AccountDialog.SyncFolder.Change");

    /// <summary>The confirm button text for moving the sync folder.</summary>
    public string ChangeSyncFolderConfirmText => Localization.GetLocal("AccountDialog.SyncFolder.Confirm.Yes");

    /// <summary>Asks the user for a folder and changes the account's sync folder to it.</summary>
    public IAsyncRelayCommand ChooseSyncFolderCommand => field ??= new AsyncRelayCommand(ChooseSyncFolderAsync, CanChooseSyncFolder);

    /// <summary>Changes the sync folder after the user confirmed.</summary>
    public IAsyncRelayCommand ConfirmSyncFolderChangeCommand => field ??= new AsyncRelayCommand(ConfirmSyncFolderChangeAsync);

    /// <summary>Goes back to the sync folder panel without changing anything.</summary>
    public IRelayCommand CancelSyncFolderChangeCommand => field ??= new RelayCommand(CancelSyncFolderChange);

    /// <summary>The dialog title.</summary>
    public string TitleText => Localization.GetLocal(TitleKey());

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
        if (IsOpen)
            return;

        SetStep(AccountDialogStepFactory.CreateWorking(Localization.GetLocal("AccountDialog.SigningIn", account.Name)));
        _ = await UiResumingTry.RunAsync(() => actions.ReauthenticateAsync(account.Id, cancellationToken), signedIn => signedIn.Match(_ => Close(), failure => ShowSignInFailure(failure, account)), _ => ShowSignInFailure(ReauthFailureFactory.CreateProvider(ProviderErrorFactory.CreateUnexpected()), account), cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(AccountViewModel account, CancellationToken cancellationToken)
    {
        if (IsOpen)
            return;

        pendingRemoval = account;
        SetStep(AccountDialogStepFactory.CreateConfirmingRemoval(account));
        closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await closed.Task.WaitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ChangeSyncFolderAsync(AccountViewModel account, CancellationToken cancellationToken)
    {
        if (IsOpen)
            return;

        closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        SetStep(AccountDialogStepFactory.CreateSyncFolder(account, string.Empty, string.Empty));
        var currentText = await CurrentFolderTextAsync(account.Id, cancellationToken);
        transferConcurrency = await concurrencyReader.GetAsync(account.Id, cancellationToken).MatchAsync(found => found.Value, _ => TransferConcurrencyFactory.Default);
        transferConcurrencyMessage = string.Empty;
        NotifyTransferConcurrency();
        _ = ShowSyncFolder(account, currentText, string.Empty);
        await closed.Task.WaitAsync(cancellationToken);
    }

    private bool CanChangeTransferConcurrency(int delta) => Step is AccountDialogStep.SyncFolder && TransferConcurrencyFactory.Create(transferConcurrency + delta).Value != transferConcurrency;

    private async Task ChangeTransferConcurrencyAsync(int delta, CancellationToken cancellationToken)
    {
        if (Step is not AccountDialogStep.SyncFolder folder)
            return;

        var next = TransferConcurrencyFactory.Create(transferConcurrency + delta);
        _ = await concurrencyWriter.SetAsync(folder.Account.Id, next, cancellationToken)
            .MatchAsync(_ => SetTransferConcurrency(next.Value, string.Empty), _ => SetTransferConcurrency(transferConcurrency, Localization.GetLocal("AccountDialog.Transfers.SaveFailed")));
    }

    private Unit SetTransferConcurrency(int value, string message)
    {
        transferConcurrency = value;
        transferConcurrencyMessage = message;
        NotifyTransferConcurrency();

        return Unit.Instance;
    }

    private void NotifyTransferConcurrency()
    {
        OnPropertyChanged(nameof(TransferConcurrencyText));
        OnPropertyChanged(nameof(TransferConcurrencyMessageText));
        OnPropertyChanged(nameof(HasTransferConcurrencyMessage));
        IncreaseTransferConcurrencyCommand.NotifyCanExecuteChanged();
        DecreaseTransferConcurrencyCommand.NotifyCanExecuteChanged();
    }

    private bool CanChooseSyncFolder() => Step is AccountDialogStep.SyncFolder { Account.Status.IsSyncing: false };

    private async Task ChooseSyncFolderAsync(CancellationToken cancellationToken)
    {
        if (Step is not AccountDialogStep.SyncFolder folder)
            return;

        var chosen = await folderPicker.PickAsync(Option.None<IDirectoryInfo>(), cancellationToken);
        _ = await chosen.Match(
            directory => folder.Account.HasSynced ? Task.FromResult(AskToConfirm(folder, directory)) : ApplySyncFolderAsync(folder.Account, directory, folder.CurrentFolderText, cancellationToken),
            () => Task.FromResult(Unit.Instance));
    }

    private async Task ConfirmSyncFolderChangeAsync(CancellationToken cancellationToken)
    {
        if (Step is AccountDialogStep.ConfirmingSyncFolderChange confirming)
            _ = await ApplySyncFolderAsync(confirming.Account, confirming.Candidate, confirming.CurrentFolderText, cancellationToken);
    }

    private void CancelSyncFolderChange()
    {
        if (Step is AccountDialogStep.ConfirmingSyncFolderChange confirming)
            _ = ShowSyncFolder(confirming.Account, confirming.CurrentFolderText, string.Empty);
    }

    private Unit AskToConfirm(AccountDialogStep.SyncFolder folder, IDirectoryInfo directory)
    {
        SetStep(AccountDialogStepFactory.CreateConfirmingSyncFolderChange(folder.Account, directory, folder.CurrentFolderText));

        return Unit.Instance;
    }

    private Unit ShowSyncFolder(AccountViewModel account, string currentText, string message)
    {
        SetStep(AccountDialogStepFactory.CreateSyncFolder(account, currentText, message));

        return Unit.Instance;
    }

    private Task<string> CurrentFolderTextAsync(AccountId accountId, CancellationToken cancellationToken) =>
        syncRoots.GetAsync(accountId, cancellationToken)
            .MatchAsync(
                root => root.Match(found => found.Directory.FullName, () => Localization.GetLocal("AccountDialog.SyncFolder.None")),
                _ => Localization.GetLocal("AccountDialog.SyncFolder.ReadFailed"));

    private async Task<Unit> ApplySyncFolderAsync(AccountViewModel account, IDirectoryInfo directory, string currentText, CancellationToken cancellationToken)
    {
        SetStep(AccountDialogStepFactory.CreateWorking(Localization.GetLocal("AccountDialog.SyncFolder.Working", account.Name)));

        return await UiResumingTry.RunAsync(
            () => actions.ChangeSyncRootAsync(account.Id, directory, cancellationToken),
            changed => changed.Match(root => Task.FromResult(ShowSyncFolder(account, root.Directory.FullName, string.Empty)), rejection => ShowRejectionAsync(account, currentText, rejection, cancellationToken)),
            _ => Task.FromResult(ShowSyncFolder(account, currentText, Localization.GetLocal("AccountDialog.SyncFolder.Failure.Store"))),
            cancellationToken).Unwrap();
    }

    private async Task<Unit> ShowRejectionAsync(AccountViewModel account, string currentText, SyncRootRejection rejection, CancellationToken cancellationToken) =>
        ShowSyncFolder(account, currentText, await RejectionTextAsync(rejection, cancellationToken));

    private async Task<string> RejectionTextAsync(SyncRootRejection rejection, CancellationToken cancellationToken) => rejection switch
    {
        SyncRootRejection.SameAs clash => Localization.GetLocal("AccountDialog.SyncFolder.Failure.SameAs", await AccountNameAsync(clash.Other, cancellationToken)),
        SyncRootRejection.InsideOf clash => Localization.GetLocal("AccountDialog.SyncFolder.Failure.InsideOf", await AccountNameAsync(clash.Other, cancellationToken)),
        SyncRootRejection.Contains clash => Localization.GetLocal("AccountDialog.SyncFolder.Failure.Contains", await AccountNameAsync(clash.Other, cancellationToken)),
        SyncRootRejection.NotAbsolute => Localization.GetLocal("AccountDialog.SyncFolder.Failure.NotAbsolute"),
        SyncRootRejection.NotWritable => Localization.GetLocal("AccountDialog.SyncFolder.Failure.NotWritable"),
        SyncRootRejection.Unavailable => Localization.GetLocal("AccountDialog.SyncFolder.Failure.Unavailable"),
        SyncRootRejection.Reserved => Localization.GetLocal("AccountDialog.SyncFolder.Failure.Reserved"),
        SyncRootRejection.SyncStillRunning => Localization.GetLocal("AccountDialog.Failure.SyncStillRunning"),
        _ => Localization.GetLocal("AccountDialog.SyncFolder.Failure.Store"),
    };

    private Task<string> AccountNameAsync(AccountId accountId, CancellationToken cancellationToken) =>
        accounts.GetAllAsync(cancellationToken)
            .MatchAsync(
                stored => stored.FirstOrNone(account => account.Id == accountId).Match(account => account.Registration.DisplayName, () => Localization.GetLocal("AccountDialog.SyncFolder.OtherAccount")),
                _ => Localization.GetLocal("AccountDialog.SyncFolder.OtherAccount"));

    private async Task ConfirmRemoveAsync(CancellationToken cancellationToken)
    {
        if (Step is not AccountDialogStep.ConfirmingRemoval confirming)
            return;

        SetStep(AccountDialogStepFactory.CreateWorking(Localization.GetLocal("AccountDialog.Removing", confirming.Account.Name)));
        _ = await UiResumingTry.RunAsync(() => actions.RemoveAsync(confirming.Account.Id, cancellationToken), removed => removed.Match(_ => Close(), ShowRemovalFailure), _ => ShowRemovalFailure(AccountRemovalFailureFactory.CreateProvider(ProviderErrorFactory.CreateUnexpected())), cancellationToken);
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

    private string TitleKey()
    {
        if (Step is AccountDialogStep.SyncFolder or AccountDialogStep.ConfirmingSyncFolderChange)
            return "AccountDialog.SyncFolder.Title";

        return Step is AccountDialogStep.ConfirmingRemoval || pendingRemoval.Match(_ => true, () => false) ? "AccountDialog.Remove.Title" : "AccountDialog.SignIn.Title";
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
        OnPropertyChanged(nameof(IsShowingSyncFolder));
        OnPropertyChanged(nameof(IsConfirmingSyncFolderChange));
        OnPropertyChanged(nameof(SyncFolderText));
        OnPropertyChanged(nameof(SyncFolderPathText));
        OnPropertyChanged(nameof(SyncFolderMessageText));
        OnPropertyChanged(nameof(HasSyncFolderMessage));
        OnPropertyChanged(nameof(SyncFolderSyncingText));
        OnPropertyChanged(nameof(SyncFolderConfirmText));
        OnPropertyChanged(nameof(TitleText));
        ChooseSyncFolderCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(ConfirmText));
        OnPropertyChanged(nameof(WorkingText));
        OnPropertyChanged(nameof(MessageText));
    }
}
