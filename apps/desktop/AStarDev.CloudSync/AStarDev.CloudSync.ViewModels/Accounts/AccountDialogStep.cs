namespace AStarDev.CloudSync.ViewModels.Accounts;

/// <summary>What the account dialog shows.</summary>
public abstract record AccountDialogStep
{
    private AccountDialogStep()
    {
    }

    /// <summary>The dialog is closed.</summary>
    public sealed record Closed : AccountDialogStep
    {
        internal Closed()
        {
        }
    }

    /// <summary>Asking the user to confirm removing <see cref="Account"/>.</summary>
    public sealed record ConfirmingRemoval : AccountDialogStep
    {
        internal ConfirmingRemoval(AccountViewModel account) => Account = account;

        /// <summary>The account to remove.</summary>
        public AccountViewModel Account { get; }
    }

    /// <summary>Showing the folder <see cref="Account"/> syncs into, with the action to change it.</summary>
    public sealed record SyncFolder : AccountDialogStep
    {
        internal SyncFolder(AccountViewModel account, string currentFolderText, string messageText)
        {
            Account = account;
            CurrentFolderText = currentFolderText;
            MessageText = messageText;
        }

        /// <summary>The account.</summary>
        public AccountViewModel Account { get; }

        /// <summary>The current sync folder, or text saying there is none.</summary>
        public string CurrentFolderText { get; }

        /// <summary>The last change failure, or empty.</summary>
        public string MessageText { get; }

        /// <summary>Returns a redacted form; the texts name a folder and an account.</summary>
        public override string ToString() => "SyncFolder(redacted)";
    }

    /// <summary>Asking the user to confirm moving <see cref="Account"/> to a new sync folder after it has synced.</summary>
    public sealed record ConfirmingSyncFolderChange : AccountDialogStep
    {
        internal ConfirmingSyncFolderChange(AccountViewModel account, System.IO.Abstractions.IDirectoryInfo candidate, string currentFolderText)
        {
            Account = account;
            Candidate = candidate;
            CurrentFolderText = currentFolderText;
        }

        /// <summary>The account.</summary>
        public AccountViewModel Account { get; }

        /// <summary>The folder the user chose.</summary>
        public System.IO.Abstractions.IDirectoryInfo Candidate { get; }

        /// <summary>The current sync folder, shown again if the user cancels.</summary>
        public string CurrentFolderText { get; }

        /// <summary>Returns a redacted form; it holds a folder path.</summary>
        public override string ToString() => "ConfirmingSyncFolderChange(redacted)";
    }

    /// <summary>A removal or sign-in is running.</summary>
    public sealed record Working : AccountDialogStep
    {
        internal Working(string text) => Text = text;

        /// <summary>The progress text.</summary>
        public string Text { get; }

        /// <summary>Returns a redacted form; the text names the account.</summary>
        public override string ToString() => "Working(redacted)";
    }

    /// <summary>A failure message the user closes.</summary>
    public sealed record Message : AccountDialogStep
    {
        internal Message(string text) => Text = text;

        /// <summary>The message.</summary>
        public string Text { get; }

        /// <summary>Returns a redacted form; the text may name the account.</summary>
        public override string ToString() => "Message(redacted)";
    }
}
