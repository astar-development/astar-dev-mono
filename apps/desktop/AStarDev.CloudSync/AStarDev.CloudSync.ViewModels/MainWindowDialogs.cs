using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Conflicts;
using AStarDev.CloudSync.ViewModels.FolderSelection;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.CloudSync.ViewModels.SyncRestart;
using AStarDev.CloudSync.ViewModels.Tray;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>The dialogs and screens the main window shows over its layout.</summary>
public sealed class MainWindowDialogs(AddAccountViewModel addAccount, AccountDialogViewModel accountDialog, FolderSelectionViewModel folderSelection, ConfirmSyncRestartViewModel syncRestart, ConflictListViewModel conflictList, DatabaseHealthBannerViewModel databaseHealth, ConfirmQuitViewModel confirmQuit)
{
    /// <summary>The AddAccount.</summary>
    public AddAccountViewModel AddAccount { get; } = addAccount;

    /// <summary>The AccountDialog.</summary>
    public AccountDialogViewModel AccountDialog { get; } = accountDialog;

    /// <summary>The FolderSelection.</summary>
    public FolderSelectionViewModel FolderSelection { get; } = folderSelection;

    /// <summary>The SyncRestart.</summary>
    public ConfirmSyncRestartViewModel SyncRestart { get; } = syncRestart;

    /// <summary>The ConflictList.</summary>
    public ConflictListViewModel ConflictList { get; } = conflictList;

    /// <summary>The banner shown while the database integrity check has failed.</summary>
    public DatabaseHealthBannerViewModel DatabaseHealth { get; } = databaseHealth;

    /// <summary>The question shown when Quit is chosen while a sync is running.</summary>
    public ConfirmQuitViewModel ConfirmQuit { get; } = confirmQuit;
}
