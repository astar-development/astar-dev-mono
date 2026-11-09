namespace AStarDev.CloudSync.ViewModels.Accounts;

/// <summary>Creates <see cref="AccountDialogStep"/> cases.</summary>
public static class AccountDialogStepFactory
{
    /// <summary>Creates the closed step.</summary>
    public static AccountDialogStep CreateClosed() => new AccountDialogStep.Closed();

    /// <summary>Creates the confirm-removal step.</summary>
    public static AccountDialogStep CreateConfirmingRemoval(AccountViewModel account) => new AccountDialogStep.ConfirmingRemoval(account);

    /// <summary>Creates the sync-folder step.</summary>
    public static AccountDialogStep CreateSyncFolder(AccountViewModel account, string? currentFolderText, string? messageText) => new AccountDialogStep.SyncFolder(account, currentFolderText ?? string.Empty, messageText ?? string.Empty);

    /// <summary>Creates the confirm-sync-folder-change step.</summary>
    public static AccountDialogStep CreateConfirmingSyncFolderChange(AccountViewModel account, System.IO.Abstractions.IDirectoryInfo candidate, string? currentFolderText) => new AccountDialogStep.ConfirmingSyncFolderChange(account, candidate, currentFolderText ?? string.Empty);

    /// <summary>Creates the working step.</summary>
    public static AccountDialogStep CreateWorking(string? text) => new AccountDialogStep.Working(text ?? string.Empty);

    /// <summary>Creates the message step.</summary>
    public static AccountDialogStep CreateMessage(string? text) => new AccountDialogStep.Message(text ?? string.Empty);
}
