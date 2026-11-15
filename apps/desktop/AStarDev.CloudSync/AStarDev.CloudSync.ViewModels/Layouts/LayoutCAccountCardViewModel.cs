using System.Windows.Input;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Layouts;

/// <summary>An account card in layout C's accounts strip. Selecting it shows only that account's sync pairs.</summary>
/// <param name="account">The account.</param>
/// <param name="select">Called with this card when the user selects it.</param>
/// <param name="syncActions">Pauses or resumes the account when the user picks the card action.</param>
/// <param name="localization">Supplies the text and culture.</param>
public sealed class LayoutCAccountCardViewModel(AccountViewModel account, Action<LayoutCAccountCardViewModel> select, ISyncActions syncActions, ILocalizationService localization) : LocalizedObservableObject(localization)
{
    private bool isSelected;

    /// <summary>The account.</summary>
    public AccountViewModel Account => account;

    /// <summary>Whether this is the selected account.</summary>
    public bool IsSelected
    {
        get => isSelected;
        internal set => SetProperty(ref isSelected, value);
    }

    /// <summary>Selects this account, or clears the selection when it is already selected.</summary>
    public ICommand SelectCommand => field ??= new RelayCommand(() => select(this));

    /// <summary>The card action: "Cancel" while syncing, otherwise "Sync now".</summary>
    public string ActionText => Localization.GetLocal(account.Status.IsSyncing ? "LayoutA.Card.Cancel" : "LayoutA.Card.SyncNow");

    /// <summary>The card action: cancels the account's sync while syncing, otherwise syncs it now. Cannot run while the account needs sign-in.</summary>
    public IAsyncRelayCommand ActionCommand => field ??= new AsyncRelayCommand(RunActionAsync, () => account.CanPause);

    /// <summary>Pauses the account's scheduled syncing, or resumes it when paused. Cannot run while the account needs sign-in.</summary>
    public IAsyncRelayCommand PauseCommand => field ??= new AsyncRelayCommand(cancellationToken => syncActions.PauseAsync(account.Id, cancellationToken), () => account.CanPause);

    private Task RunActionAsync(CancellationToken cancellationToken) => account.Status.IsSyncing ? syncActions.CancelSyncAsync(account.Id, cancellationToken) : syncActions.SyncNowAsync(account.Id, cancellationToken);
}
