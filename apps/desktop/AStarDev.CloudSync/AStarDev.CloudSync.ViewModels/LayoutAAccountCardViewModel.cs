using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>An account as shown in layout A's account list and account cards.</summary>
/// <param name="account">The account.</param>
/// <param name="localization">Supplies the card text.</param>
public sealed class LayoutAAccountCardViewModel(AccountViewModel account, ILocalizationService localization) : LocalizedObservableObject(localization)
{
    /// <summary>The account.</summary>
    public AccountViewModel Account => account;

    /// <summary>The email and provider, e.g. "work@example.com · OneDrive".</summary>
    public string EmailAndProviderText => Localization.GetLocal("LayoutA.Card.EmailAndProvider", account.Email, account.Provider.Name);

    /// <summary>The card action: "Cancel" while syncing, otherwise "Sync now".</summary>
    public string ActionText => Localization.GetLocal(account.Status.IsSyncing ? "LayoutA.Card.Cancel" : "LayoutA.Card.SyncNow");

    /// <summary>The storage label.</summary>
    public string StorageLabel => Localization.GetLocal("LayoutA.Card.Storage");

    /// <summary>The folder count label.</summary>
    public string FoldersLabel => Localization.GetLocal("LayoutA.Card.Folders");

    /// <summary>The file count label.</summary>
    public string FilesLabel => Localization.GetLocal("LayoutA.Card.Files");

    /// <summary>The conflict count label.</summary>
    public string ConflictsLabel => Localization.GetLocal("Stats.Conflicts");

    /// <summary>The last sync label.</summary>
    public string LastSyncLabel => Localization.GetLocal("LayoutA.Card.LastSync");

    /// <summary>The recent activity heading.</summary>
    public string RecentActivityLabel => Localization.GetLocal("LayoutA.Card.RecentActivity");
}
