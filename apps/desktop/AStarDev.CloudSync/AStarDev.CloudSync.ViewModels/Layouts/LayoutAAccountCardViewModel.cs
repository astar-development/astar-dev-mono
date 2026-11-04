using System.Windows.Input;
using AStarDev.CloudSync.Core.Common;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Layouts;

/// <summary>An account as shown in layout A's account list and account cards.</summary>
/// <param name="account">The account.</param>
/// <param name="select">Called with this card when the user selects it.</param>
/// <param name="syncActions">Syncs or cancels the account when the user picks the card action.</param>
/// <param name="localization">Supplies the card text.</param>
public sealed class LayoutAAccountCardViewModel(AccountViewModel account, Action<LayoutAAccountCardViewModel> select, ISyncActions syncActions, ILocalizationService localization) : LocalizedObservableObject(localization)
{
    private bool isSelected;

    /// <summary>The account.</summary>
    public AccountViewModel Account => account;

    /// <summary>Whether this is the selected account in the account list.</summary>
    public bool IsSelected
    {
        get => isSelected;
        internal set => SetProperty(ref isSelected, value);
    }

    /// <summary>Makes this the selected account.</summary>
    public ICommand SelectCommand => field ??= new RelayCommand(() => select(this));

    /// <summary>The email and provider, e.g. "work@example.com · OneDrive".</summary>
    public string EmailAndProviderText => Localization.GetLocal("LayoutA.Card.EmailAndProvider", account.Email, account.Provider.Name);

    /// <summary>The card action: "Cancel" while syncing, otherwise "Sync now".</summary>
    public string ActionText => Localization.GetLocal(account.Status.IsSyncing ? "LayoutA.Card.Cancel" : "LayoutA.Card.SyncNow");

    /// <summary>The card action: cancels the account's sync while syncing, otherwise syncs it now.</summary>
    public IAsyncRelayCommand ActionCommand => field ??= new AsyncRelayCommand(cancellationToken => account.Status.IsSyncing ? syncActions.CancelSyncAsync(account.Id, cancellationToken) : syncActions.SyncNowAsync(account.Id, cancellationToken));

    /// <summary>The storage label.</summary>
    public string StorageLabel => Localization.GetLocal("LayoutA.Card.Storage");

    /// <summary>The folder count label.</summary>
    public string FoldersLabel => Localization.GetLocal(CountKey.For("LayoutA.Card.Folders", account.FolderCount));

    /// <summary>The file count label.</summary>
    public string FilesLabel => Localization.GetLocal(CountKey.For("LayoutA.Card.Files", account.FileCount));

    /// <summary>The conflict count label.</summary>
    public string ConflictsLabel => Localization.GetLocal(CountKey.For("Stats.Conflicts", account.ConflictCount));

    /// <summary>The last sync label.</summary>
    public string LastSyncLabel => Localization.GetLocal("LayoutA.Card.LastSync");

    /// <summary>The recent activity heading.</summary>
    public string RecentActivityLabel => Localization.GetLocal("LayoutA.Card.RecentActivity");
}
