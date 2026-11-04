using AStarDev.CloudSync.Core;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>What the layout B detail pane shows: the selected account, or a message when there are no accounts.</summary>
/// <param name="localization">Supplies the text and culture.</param>
public abstract class LayoutBDetailViewModel(ILocalizationService localization) : LocalizedObservableObject(localization);

/// <summary>The layout B detail pane for the selected account.</summary>
public sealed class AccountDetailViewModel : LayoutBDetailViewModel
{
    private const double FullCircleDegrees = 360;
    private const int RecentActivityLimit = 5;

    /// <summary>Creates the detail for <paramref name="account"/>, keeping the account's five newest activity entries.</summary>
    /// <param name="account">The selected account.</param>
    /// <param name="syncPairs">The account's sync pairs.</param>
    /// <param name="activity">Activity across every account, newest first; only this account's entries are kept.</param>
    /// <param name="localPathFormatter">Shortens the synced folders' local paths.</param>
    /// <param name="syncActions">Carries out the account actions the user picks.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    public AccountDetailViewModel(AccountViewModel account, IReadOnlyList<SyncPairViewModel> syncPairs, IEnumerable<ActivityItemViewModel> activity, LocalPathFormatter localPathFormatter, ISyncActions syncActions, ILocalizationService localization) : base(localization)
    {
        Account = account;
        PauseCommand = new AsyncRelayCommand(cancellationToken => syncActions.PauseAsync(account.Id, cancellationToken));
        SyncNowCommand = new AsyncRelayCommand(cancellationToken => syncActions.SyncNowAsync(account.Id, cancellationToken));
        AddFolderCommand = new AsyncRelayCommand(cancellationToken => syncActions.AddSyncPairAsync(Option.Some(account.Id), cancellationToken));
        SyncPairs = syncPairs;
        SyncedFolders = [.. syncPairs.Select(pair => new SyncedFolderViewModel(pair, localPathFormatter))];
        RecentActivity = [.. activity.Where(item => item.AccountId == account.Id).Take(RecentActivityLimit)];
        Stats =
        [
            new(() => account.FoldersText, CountKey.For("LayoutB.Stats.Folders", account.FolderCount), StatTileToneFactory.CreateNormal(), localization),
            new(() => account.FilesText, CountKey.For("LayoutB.Stats.Files", account.FileCount), StatTileToneFactory.CreateNormal(), localization),
            new(() => account.ConflictsText, CountKey.For("Stats.Conflicts", account.ConflictCount), StatTileToneFactory.CreateNormal(), localization),
            new(() => account.LastSyncText, "LayoutB.Stats.LastSync", StatTileToneFactory.CreateNormal(), localization)
        ];
    }

    /// <summary>The selected account.</summary>
    public AccountViewModel Account { get; }

    /// <summary>The account's sync pairs.</summary>
    public IReadOnlyList<SyncPairViewModel> SyncPairs { get; }

    /// <summary>The synced folders table rows, one per sync pair, with local paths shortened for display.</summary>
    public IReadOnlyList<SyncedFolderViewModel> SyncedFolders { get; }

    /// <summary>The account's five newest activity entries, newest first.</summary>
    public IReadOnlyList<ActivityItemViewModel> RecentActivity { get; }

    /// <summary>The account's figures: folders, files, conflicts and last sync.</summary>
    public IReadOnlyList<StatTileViewModel> Stats { get; }

    /// <summary>Provider and account, e.g. "OneDrive / Work".</summary>
    public string BreadcrumbText => Localization.GetLocal("LayoutB.Breadcrumb", Account.Provider.Name, Account.Name);

    /// <summary>Email and status, e.g. "work@example.com · Syncing 25%".</summary>
    public string SubtitleText => Localization.GetLocal("LayoutB.Subtitle", Account.Email, Account.Status.Text);

    /// <summary>The account's sync progress from 0 to 1.</summary>
    public double Progress => Account.Status.Progress;

    /// <summary>The storage ring's filled arc, in degrees.</summary>
    public double StorageSweepAngle => Account.Storage.Fraction * FullCircleDegrees;

    /// <summary>The used storage as a percentage, e.g. "71%".</summary>
    public string StoragePercentText => Account.Storage.Fraction.ToString("P0", Localization.CurrentCulture);

    /// <summary>Pauses syncing for the account.</summary>
    public IAsyncRelayCommand PauseCommand { get; }

    /// <summary>Syncs the account now.</summary>
    public IAsyncRelayCommand SyncNowCommand { get; }

    /// <summary>Starts adding a sync pair for the account.</summary>
    public IAsyncRelayCommand AddFolderCommand { get; }

    /// <summary>The pause action text.</summary>
    public string PauseText => Localization.GetLocal("LayoutB.Pause");

    /// <summary>The sync-now action text.</summary>
    public string SyncNowText => Localization.GetLocal("LayoutB.SyncNow");

    /// <summary>The Overview tab header.</summary>
    public string OverviewTabText => Localization.GetLocal("LayoutB.Tab.Overview");

    /// <summary>The Folders tab header.</summary>
    public string FoldersTabText => Localization.GetLocal("LayoutB.Tab.Folders");

    /// <summary>The Activity tab header.</summary>
    public string ActivityTabText => Localization.GetLocal("LayoutB.Tab.Activity");

    /// <summary>The Conflicts tab header.</summary>
    public string ConflictsTabText => Localization.GetLocal("LayoutB.Tab.Conflicts");

    /// <summary>The synced folders table title.</summary>
    public string SyncedFoldersText => Localization.GetLocal("LayoutB.SyncedFolders");

    /// <summary>The add-folder action text.</summary>
    public string AddFolderText => Localization.GetLocal("LayoutB.AddFolder");

    /// <summary>The local folder column header.</summary>
    public string LocalColumnText => Localization.GetLocal("LayoutB.Column.Local");

    /// <summary>The cloud folder column header.</summary>
    public string CloudColumnText => Localization.GetLocal("LayoutB.Column.Cloud");

    /// <summary>The status column header.</summary>
    public string StatusColumnText => Localization.GetLocal("LayoutB.Column.Status");

    /// <summary>The last sync column header.</summary>
    public string LastSyncColumnText => Localization.GetLocal("LayoutB.Column.LastSync");

    /// <summary>The storage card title.</summary>
    public string StorageText => Localization.GetLocal("LayoutB.Storage");

    /// <summary>The activity card title.</summary>
    public string ActivityText => Localization.GetLocal("LayoutB.Tab.Activity");
}

/// <summary>The layout B detail pane when no account is connected.</summary>
/// <param name="localization">Supplies the message text.</param>
public sealed class NoAccountDetailViewModel(ILocalizationService localization) : LayoutBDetailViewModel(localization)
{
    /// <summary>The message shown instead of account details.</summary>
    public string Text => Localization.GetLocal("LayoutB.NoAccounts");
}

/// <summary>The layout B detail pane when accounts exist but none match the sidebar search.</summary>
/// <param name="localization">Supplies the message text.</param>
public sealed class NoMatchingAccountsDetailViewModel(ILocalizationService localization) : LayoutBDetailViewModel(localization)
{
    /// <summary>The message shown instead of account details.</summary>
    public string Text => Localization.GetLocal("LayoutB.NoMatches");
}
