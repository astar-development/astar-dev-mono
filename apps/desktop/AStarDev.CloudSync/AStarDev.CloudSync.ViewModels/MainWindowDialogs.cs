using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Conflicts;
using AStarDev.CloudSync.ViewModels.FolderSelection;
using AStarDev.CloudSync.ViewModels.SyncRestart;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>The dialogs and screens the main window shows over its layout.</summary>
public sealed class MainWindowDialogs(AddAccountViewModel addAccount, AccountDialogViewModel accountDialog, FolderSelectionViewModel folderSelection, ConfirmSyncRestartViewModel syncRestart, ConflictListViewModel conflictList)
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
}
