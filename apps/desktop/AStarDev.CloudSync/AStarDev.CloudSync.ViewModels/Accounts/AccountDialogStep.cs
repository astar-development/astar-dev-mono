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
